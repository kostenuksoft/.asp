using System.Globalization;
using System.Reflection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Registry.Api.ErrorHandling;
using Registry.Bll.Services;
using Registry.Bll.Services.Interfaces;
using Registry.Dal;
using Registry.Dal.Seeding;
using Registry.Dal.UnitOfWork;
using Serilog;
using Serilog.Events;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("RegistryDb")
    ?? throw new InvalidOperationException("Connection string 'RegistryDb' is missing.");

Assembly businessLayer = typeof(PackageService).Assembly;

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {RequestId} {RequestPath} {Message:lj}{NewLine}{Exception}",
        formatProvider: CultureInfo.InvariantCulture
    )
);

builder.Services.AddDbContext<RegistryDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<ITagService, TagService>();

builder.Services.AddAutoMapper(configuration =>
{
    configuration.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    configuration.AddMaps(businessLayer);
});

builder.Services.AddValidatorsFromAssembly(businessLayer);

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier
);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    RegistryDbContext context = scope.ServiceProvider.GetRequiredService<RegistryDbContext>();
    await context.Database.MigrateAsync();

    if (!app.Environment.IsProduction())
    {
        await DbInitializer.SeedAsync(context);
    }
}

app.UseSerilogRequestLogging(options => options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier)
);
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
