using Lithium.Gallery.Api;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>policy.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.AddGalleryApi(builder.Configuration.GetSection("Gallery"));
builder.Services.AddInstagramImages();

var app = builder.Build();
app.UseCors("AllowAll");
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();

app.UseAntiforgery();
app.UseGalleryApi();
app.UseInstagramImages();

app.Run();

