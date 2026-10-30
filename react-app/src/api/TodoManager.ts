import {
  CompleteTodoRequestDto,
  OperationResultDto,
  SaveTodoRequestDto,
  TodoListDto,
  TodoListRequestDto,
} from './AppDtos';
import ApiClient from './ApiClient';

const ListTodos = (request: TodoListRequestDto): Promise<TodoListDto> =>
  ApiClient.invokeMethod<TodoListDto>('Api', 'TodoManager', 'ListTodos', request);

const SaveTodo = (request: SaveTodoRequestDto): Promise<OperationResultDto> =>
  ApiClient.invokeMethod<OperationResultDto>('Api', 'TodoManager', 'SaveTodo', request);

const CompleteTodo = (request: CompleteTodoRequestDto): Promise<OperationResultDto> =>
  ApiClient.invokeMethod<OperationResultDto>('Api', 'TodoManager', 'CompleteTodo', request);

export default { ListTodos, SaveTodo, CompleteTodo };
