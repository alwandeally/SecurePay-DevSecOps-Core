var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Ok(new
{
    service = "SecurePay Transactions API",
    status = "Running",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTimeOffset.UtcNow
}))
.WithName("GetTransactionsServiceStatus");

app.Run();
