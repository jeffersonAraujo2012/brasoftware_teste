using Api.Errors;
using Application;
using Infra;

var builder = WebApplication.CreateBuilder(args);

const string FrontendPolicy = "Frontend";
builder.Services.AddControllers().ConfigureApiBehaviorOptions(
    options => options.InvalidModelStateResponseFactory = ApiProblemDetails.FromModelState);
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = ApiProblemDetails.TraceId(context.HttpContext));
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();
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

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(FrontendPolicy);
app.MapControllers();
app.Run();

public partial class Program { }
