using System.Reflection;
using Cicd.Api.ErrorHandling;
using Cicd.Bll.Services;
using Cicd.Bll.Services.Interfaces;
using Cicd.Dal.UnitOfWork;
using Platform.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

string connectionString = builder.Configuration.GetConnectionString("CicdDb")
    ?? throw new InvalidOperationException("Connection string 'CicdDb' is missing.");

Assembly businessLayer = typeof(BuildRunService).Assembly;

builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
builder.Services.AddScoped<IPipelineService, PipelineService>();
builder.Services.AddScoped<IBuildRunService, BuildRunService>();
builder.Services.AddScoped<IAgentService, AgentService>();

builder.Services.AddAutoMapper(configuration =>
{
    configuration.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    configuration.AddMaps(businessLayer);
});

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier
);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
