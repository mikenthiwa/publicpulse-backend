using Microsoft.Extensions.FileProviders;
using Web.Features.Reports;
using Web;
using Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddWebServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseCors(CorsOptions.PolicyName);
var reportImageOptions = builder.Configuration
    .GetSection(ReportImageStorageOptions.SectionName)
    .Get<ReportImageStorageOptions>() ?? new ReportImageStorageOptions();
var localStoragePath = string.IsNullOrWhiteSpace(reportImageOptions.LocalStoragePath)
    ? Path.Combine(app.Environment.ContentRootPath, "uploads")
    : reportImageOptions.LocalStoragePath;
localStoragePath = Path.GetFullPath(localStoragePath);
Directory.CreateDirectory(localStoragePath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(localStoragePath),
    RequestPath = "/uploads"
});
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.Run();

public partial class Program;
