WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

WebApplication app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
