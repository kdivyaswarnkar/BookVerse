using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BookStore.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log)
{
    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        // Our stored procedures raise business errors as 50001-50099
        // (50001 = email exists, 50002 = empty cart, 50003 = no stock, ...)
        catch (SqlException ex) when (ex.Number is >= 50001 and <= 50099)
        {
            await Write(ctx, ex.Number == 50001 ? 409 : 400, ex.Message);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Unhandled error on {Path}", ctx.Request.Path);
            await Write(ctx, 500, "Something went wrong. Please try again.");
        }
    }

    private static Task Write(HttpContext ctx, int status, string message)
    {
        ctx.Response.StatusCode = status;
        return ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Detail = message
        });
    }
}