using System.Text.Json;
using System.Text.Json.Serialization;
using Platform.ServiceDefaults;
using Telemetry.Application;
using Telemetry.Application.Common.Mappings;
using Telemetry.Infrastructure;
using Telemetry.Infrastructure.Persistence;
using Telemetry.WebApi.ErrorHandling;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

string? telemetryDb = builder.Configuration.GetConnectionString("TelemetryDb");

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

if (!string.IsNullOrEmpty(telemetryDb))
{
    builder.Services.PostConfigure<MongoDbOptions>(options => options.ConnectionString = telemetryDb);
}

builder.Services.AddAutoMapper(options =>
{
    options.LicenseKey = builder.Configuration["LuckyPenny:LicenseKey"];
    options.AddMaps(typeof(MappingProfile).Assembly);
});

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier
);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase))
);
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

await app.RunAsync();
