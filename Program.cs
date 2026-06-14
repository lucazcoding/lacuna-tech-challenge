using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");


app.MapPost("/sum", (JsonElement body) =>
{
    int a = body.GetProperty("a").GetInt32();
    int b = body.GetProperty("b").GetInt32();

    return a + b;
});


app.Run();