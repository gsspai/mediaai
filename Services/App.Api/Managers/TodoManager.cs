using System.Globalization;
using App.Api.Accessors;
using App.Api.Utilities;
using App.Api.Utilities.Roles;
using App.ServiceInvoker.Attributes;
using App.ServiceInvoker.Interfaces;
using Contracts.Domain.Database;
using Contracts.Dto;

namespace App.Api.Managers
{
    /// <summary>
    /// 跨角色待办。自动待办由 TodoBuilder 从商务单与内容计划推导，不落库；
    /// 这里只负责人工待办的增、勾选与列表装配。
    /// </summary>
    public class TodoManager : IManagerService
    {
        private const int DefaultTake = 20;
        private const int MaxTake = 200;

        private readonly IDatabaseAccessor _databaseAccessor;
        private readonly UserContextService _userContextService;

        public TodoManager(IDatabaseAccessor databaseAccessor, UserContextService userContextService)
        {
            _databaseAccessor = databaseAccessor;
            _userContextService = userContextService;
        }

        [RequireAuthenticated]
        public async Task<TodoListDto> ListTodos(TodoListRequestDto request)
        {
            var take = Math.Clamp(request?.Take ?? DefaultTake, 1, MaxTake);
            var stored = (await _databaseAccessor.GetAllDocuments<TodoItem>()).ToList();
            var deals = (await _databaseAccessor.GetAllDocuments<Deal>()).ToList();
            var plans = (await _databaseAccessor.GetAllDocuments<ContentPlan>()).ToList();

            var items = TodoBuilder.Build(stored, deals, plans, request?.IncludeDone == true, take);

            return new TodoListDto
            {
                Items = items,
                OpenCount = items.Count(item => !string.Equals(item.Status, TodoStatus.Done, StringComparison.OrdinalIgnoreCase))
            };
        }

        /// <summary>
        /// 新增人工待办。自动待办不通过这里创建（它由底层单据推导）。
        /// </summary>
        [RequireRole(Roles.Admin, Roles.Operations)]
        public async Task<OperationResultDto> SaveTodo(SaveTodoRequestDto request)
        {
            if (request == null)
            {
                return Fail("请求内容为空。");
            }

            var title = (request.Title ?? string.Empty).Trim();
            if (title.Length == 0)
            {
                return Fail("待办标题不能为空。");
            }

            var ownerRole = (request.OwnerRole ?? string.Empty).Trim();
            if (ownerRole.Length == 0 || !Roles.IsBusinessRole(ownerRole))
            {
                return Fail($"不支持的负责角色「{request.OwnerRole}」。");
            }

            var priority = string.IsNullOrWhiteSpace(request.Priority) ? TodoPriority.Normal : request.Priority.Trim();
            if (!TodoPriority.IsValid(priority))
            {
                return Fail($"不支持的优先级「{request.Priority}」。");
            }

            DateOnly? dueDate = null;
            if (!string.IsNullOrWhiteSpace(request.DueDate))
            {
                if (!DateOnly.TryParseExact(request.DueDate.Trim(), "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    return Fail("截止日期格式应为 yyyy-MM-dd。");
                }

                dueDate = parsed;
            }

            await _databaseAccessor.InsertDocument(new TodoItem
            {
                Title = title,
                OwnerRole = ownerRole,
                Priority = priority,
                DueDate = dueDate,
                Href = string.IsNullOrWhiteSpace(request.Href) ? "/" : request.Href.Trim(),
                Status = TodoStatus.Open,
                CreatedBy = _userContextService.UserEmail ?? string.Empty,
                CreatedAt = DateTimeOffset.UtcNow
            });

            return new OperationResultDto { Success = true, Message = $"待办「{title}」已创建。" };
        }

        /// <summary>
        /// 勾选完成。只能勾人工待办，自动待办要回去改商务单 / 内容计划。
        /// </summary>
        [RequireRole(Roles.Admin, Roles.Sales, Roles.Finance, Roles.Planner, Roles.Operations, Roles.Editor, Roles.Analyst)]
        public async Task<OperationResultDto> CompleteTodo(CompleteTodoRequestDto request)
        {
            if (request == null || request.TodoId == Guid.Empty)
            {
                // 自动待办的 Id 就是 Guid.Empty（不落库），所以空 Id 基本等同于「点了自动待办」。
                return Fail("自动待办由商务单阶段或内容计划推导，不能勾选；请直接处理对应的商务单或内容计划。");
            }

            var todo = await _databaseAccessor.GetDocumentById<TodoItem>(request.TodoId);
            if (todo == null)
            {
                return Fail("待办不存在。自动待办不能勾选，请直接处理对应的商务单或内容计划。");
            }

            if (string.Equals(todo.Status, TodoStatus.Done, StringComparison.OrdinalIgnoreCase))
            {
                return new OperationResultDto { Success = true, Message = "该待办已经完成。" };
            }

            todo.Status = TodoStatus.Done;
            todo.DoneAt = DateTimeOffset.UtcNow;
            todo.DoneBy = _userContextService.UserEmail ?? string.Empty;

            await _databaseAccessor.UpsertDocument(todo);

            return new OperationResultDto { Success = true, Message = $"待办「{todo.Title}」已完成。" };
        }

        private static OperationResultDto Fail(string message) => new() { Success = false, Message = message };
    }
}
