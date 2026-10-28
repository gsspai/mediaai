using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using System.Text;
using App.ServiceInvoker.Reflection;
using App.ServiceInvoker.Interfaces;
using App.ServiceInvoker.Auth;

namespace App.ServiceInvoker.Endpoints
{
    internal sealed class StreamingErrorEventDto
    {
        public string Type { get; init; } = "error";
        public string Error { get; init; } = string.Empty;
        public bool IsFinal { get; init; } = true;
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
        public long Sequence { get; init; }
    }

    public static class ServiceInvokerEndpoints
    {
        public static IEndpointRouteBuilder MapServiceInvokerEndpoint(
            this IEndpointRouteBuilder routes,
            string route = "/invoke")
        {
            routes.MapPost(route, async (
                HttpContext context,
                IServiceProvider serviceProvider,
                IMethodAuthorizer methodAuthorizer,
                IAuthTokenManager authTokenManager,
                JsonSerializerOptions jsonOptions) =>
            {
                try
                {
                    var invoker = new ServiceMethodInvoker(serviceProvider, methodAuthorizer, authTokenManager, jsonOptions);
                    var result = await invoker.InvokeAsync(context.Request);
                    return Results.Json(result, jsonOptions);
                }
                catch (ForbiddenAccessException ex)
                {
                    // 403 也带 JSON 正文，前端才能把「需要什么角色」原样显示出来；
                    // 只回状态码时浏览器侧只能看到一句 Forbidden。
                    return Results.Json(
                        new { Success = false, Message = ex.Message },
                        jsonOptions,
                        statusCode: StatusCodes.Status403Forbidden);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.ToString());
                }
            });

            return routes;
        }

        public static IEndpointRouteBuilder MapStreamingInvokerEndpoint(
            this IEndpointRouteBuilder routes,
            string route = "/stream")
        {
            routes.MapPost(route, async (
                HttpContext context,
                IServiceProvider serviceProvider,
                IMethodAuthorizer methodAuthorizer,
                IAuthTokenManager authTokenManager,
                JsonSerializerOptions jsonOptions) =>
            {
                try
                {
                    var invoker = new ServiceMethodInvoker(serviceProvider, methodAuthorizer, authTokenManager, jsonOptions);

                    await invoker.InvokeStreamingAsync(context.Request, context.Response.Body, context.RequestAborted);

                    return Results.Empty;
                }
                catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
                {
                    return Results.Empty;
                }
                catch (ForbiddenAccessException ex)
                {
                    return Results.Json(
                        new { Success = false, Message = ex.Message },
                        jsonOptions,
                        statusCode: StatusCodes.Status403Forbidden);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
                catch (Exception ex)
                {
                    if (context.Response.HasStarted)
                    {
                        try
                        {
                            var errorEvent = new StreamingErrorEventDto
                            {
                                Error = ex.ToString(),
                                TimestampUtc = DateTime.UtcNow,
                                Sequence = 0
                            };

                            var json = JsonSerializer.Serialize(errorEvent, jsonOptions);
                            var bytes = Encoding.UTF8.GetBytes(json + "\n");
                            await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
                            await context.Response.Body.FlushAsync(context.RequestAborted);
                        }
                        catch
                        {
                        }

                        return Results.Empty;
                    }

                    return Results.Problem(ex.ToString());
                }
            });

            return routes;
        }
    }
}
