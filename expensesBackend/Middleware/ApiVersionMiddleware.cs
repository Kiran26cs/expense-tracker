namespace ExpensesBackend.API.Middleware;

public class ApiVersionMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly string _version = configuration["App:ApiVersion"] ?? "1.0.0";

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Api-Version"] = _version;
            return Task.CompletedTask;
        });
        await next(context);
    }
}
