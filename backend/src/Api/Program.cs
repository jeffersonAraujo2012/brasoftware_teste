using Infra;

var builder = WebApplication.CreateBuilder(args);

const string FrontendPolicy = "Frontend";
builder.Services.AddControllers();
builder.Services.AddInfrastructure();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    options.AddPolicy(FrontendPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(FrontendPolicy);
app.MapControllers();
app.Run();

public partial class Program { }
