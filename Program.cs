using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();

app.UseAntiforgery();
app.UseGalleryApi();

app.Run();

public static class GalleryBootstrapper
{
    public static IServiceCollection AddGalleryApi(this IServiceCollection services, IConfiguration settings)
    {
        services.Configure<GalleryConfiguration>(settings);
        return services;
    }

    public static WebApplication UseGalleryApi(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/gallery/all", ([FromServices] IOptions<GalleryConfiguration> cfg) =>
            Directory.GetDirectories(cfg.Value.RootDirectory ?? "uploads")
                .Select(dir => new DirectoryInfo(dir))
                .Select(dirInfo => dirInfo.Name));

        api.MapPost("/gallery/{galleryId}", (string galleryId, Gallery gallery, [FromServices] IOptions<GalleryConfiguration> cfg) =>
        {
            var directoryPath = Path.Combine(cfg.Value.RootDirectory ?? "uploads", galleryId);
            if (Path.Exists(directoryPath))
            {
                throw new ArgumentException("Gallery already exists.");
            }
            Directory.CreateDirectory(directoryPath);
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            using var writer = new StreamWriter(Path.Combine(directoryPath, "gallery.yaml"));
            serializer.Serialize(writer, gallery);
            return Results.Created($"/api/gallery/{galleryId}", gallery);
        });

        api.MapPut("/gallery/{galleryId}", (string galleryId, Gallery gallery, [FromServices] IOptions<GalleryConfiguration> cfg) =>
        {
            var directoryPath = Path.Combine(cfg.Value.RootDirectory ?? "uploads", galleryId);
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            using var writer = new StreamWriter(Path.Combine(directoryPath, "gallery.yaml"));
            serializer.Serialize(writer, gallery);
            return Results.Ok(gallery);
        });

        api.MapGet("/gallery/{galleryId}", (string galleryId, [FromServices] IOptions<GalleryConfiguration> cfg) =>
        {
            var directoryPath = Path.Combine(cfg.Value.RootDirectory ?? "uploads", galleryId);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var metadataPath = Path.Combine(directoryPath, "gallery.yaml");
            if (!File.Exists(metadataPath))
            {
                return Results.NotFound();
            }
            using var reader = new StreamReader(metadataPath);
            var gallery = deserializer.Deserialize<Gallery>(reader);
            return Results.Ok(gallery);
        });

        api.MapGet("/gallery/{galleryId}/images", (string galleryId, [FromServices] IOptions<GalleryConfiguration> cfg) =>
            Directory.GetFiles(Path.Combine(cfg.Value.RootDirectory ?? "uploads", galleryId))
                .Select(file => new Image
                {
                    ImageId = Path.GetFileNameWithoutExtension(file),
                    GalleryId = galleryId,
                    Url = "/uploads/" + galleryId + "/" + Path.GetFileName(file),
                    Description = "Image description for " + Path.GetFileName(file)
                }));

        api.MapPost("/gallery/{galleryId}/images/upload",
            async (string galleryId, IFormFileCollection files, [FromServices] IOptions<GalleryConfiguration> cfg) =>
            {
                foreach (var file in files)
                {
                    var directoryPath = Path.Combine(cfg.Value.RootDirectory ?? "uploads", galleryId);
                    Directory.CreateDirectory(directoryPath);
                    var path = Path.Combine(directoryPath, file.FileName);
                    using var stream = File.Create(path);
                    await file.CopyToAsync(stream);
                }
                return Results.Ok();
            })
            .DisableAntiforgery();

        return app;
    }

    class GalleryConfiguration
    {
        public string? RootDirectory { get; set; }
    }
}

public class Gallery
{
    public string? Name;
    public string? Description;
};

public class Image
{
    public string? ImageId;
    public string? GalleryId;
    public string? Url;
    public string? Description;
};
