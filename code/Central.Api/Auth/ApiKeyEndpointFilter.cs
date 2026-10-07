namespace Central.Api.Auth;

/// <summary>
/// Bridge -> Central authentication (§12): a simple API key header, not a user login.
/// Only enforced when an expected key is configured.
/// </summary>
public class ApiKeyEndpointFilter(IConfiguration config) : IEndpointFilter
{
    private readonly string? _expected = config["Bridge:ApiKey"];

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (string.IsNullOrEmpty(_expected))
        {
            return await next(context); // not configured (e.g. bare local run) — stay open
        }

        var provided = context.HttpContext.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (!string.Equals(provided, _expected, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }
}
