var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ignore me
app.MapGet("/", () => Results.Content(
// language=html
"<html lang=\"en\">\n<body>\n<h1>Minimal API Filters</h1>\n<ul>\n    <li><a href=\"/before\">(Before) Skip Filter</a></li>\n    <li><a href=\"/before/khalid\">(Before) Change Result</a></li>\n    <li><a href=\"/short-circuit\">(Short Circuit) No Endpoint</a></li>\n    <li><a href=\"/after\">(After) Use Endpoint Result</a></li>\n</ul>\n</body>\n</html>"
, "text/html"));

// run filter before the endpoint executes
app.MapGet("/before/{name?}", (string? name) => 
        new MyResult(name ?? "Hi!"))
    .AddEndpointFilter<BeforeEndpointExecution>();

// run the filter before the endpoint ever executes
app.MapGet("/short-circuit", () => "It doesn't matter")
    .AddEndpointFilter<ShortCircuit>();

// run the filter after the endpoint executes
// and hydrate the result with some extra info
app.MapGet("/after", (string? name) => new MyResultWithEndpoint(name ?? "hi!"))
    .WithDisplayName("root endpoint (/)")
    .AddEndpointFilter<AfterEndpointExecution>();

app.Run();

public class AfterEndpointExecution : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var result = await next(context);

        if (result is MyResultWithEndpoint dp && 
            context.HttpContext.GetEndpoint() is { } e)
        {
            dp.EndpointDisplayName = e.DisplayName ?? "";
        }

        return result;
    }
}

public class ShortCircuit : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, 
        EndpointFilterDelegate next)
    {
        // because YOLO!
        return new ValueTask<object?>(Results.Json(new { Fizz = "Buzz" }));
    }
}

public class BeforeEndpointExecution : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        if (context.HttpContext.GetRouteValue("name") is string name)
        {
            return Results.Ok(new MyResult($"Hi {name}, this is from the filter!"));
        }

        return await next(context);
    }
}

public record MyResult(string Value);

public record MyResultWithEndpoint(string Value)
{
    public string EndpointDisplayName { get; set; }
}