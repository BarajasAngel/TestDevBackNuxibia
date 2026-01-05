using System.Net;
using System.Text.Json;

namespace CCenter.Api.Middlewares;

public sealed class ApiExceptionMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (InvalidOperationException ex)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            ctx.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new
            {
                error = ex.Message
            });

            await ctx.Response.WriteAsync(payload);
        }
    }
}
