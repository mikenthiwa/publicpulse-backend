using Web;
using Web.Infrastructure;
using Web.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddWebServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

    if (app.Configuration.GetValue<bool>("SeedData:Enabled"))
    {
        await app.SeedDemoDataAsync();
    }
}

app.UseExceptionHandler();
app.UseCors(CorsOptions.PolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.Run();

public partial class Program;
