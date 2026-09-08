using Microsoft.EntityFrameworkCore;
using Os.Api.Domain;

namespace Os.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var (status, title) = exception switch
            {
                BusinessException => (400, exception.Message),
                KeyNotFoundException => (404, "Registro não encontrado."),
                UnauthorizedAccessException => (403, "Acesso não permitido."),
                DbUpdateConcurrencyException => (409, "Registro alterado por outra operação. Recarregue e tente novamente."),
                DbUpdateException => (409, "Conflito de dados ou registro em uso."),
                _ => (500, "Erro interno ao processar a solicitação.")
            };
            if (status == 500)
            {
                logger.LogError("Falha inesperada do tipo {ExceptionType}; trace {TraceId}",
                    exception.GetType().Name, context.Response.Headers["X-Trace-Id"].ToString());
            }
            await Results.Problem(statusCode: status, title: title,
                extensions: new Dictionary<string, object?> { ["traceId"] = context.Response.Headers["X-Trace-Id"].ToString() }).ExecuteAsync(context);
        }
    }
}
