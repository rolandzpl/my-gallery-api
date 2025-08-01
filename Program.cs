var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var api = app.MapGroup("/api");

api.MapGet("/gallery/all", () => new Gallery[]
{
    new Gallery("1", "Nature Gallery", "A collection of beautiful nature images."),
    new Gallery("2", "Urban Gallery", "A showcase of urban photography.")
});

api.MapPost("/gallery", () => { });

api.MapPut("/gallery/{galleryId}", (string galleryId) => { });

api.MapGet("/gallery/{galleryId}", (string galleryId) => new Gallery(galleryId, "Sample Gallery", "This is a sample gallery description."));

api.MapGet("/gallery/{galleryId}/images", (string galleryId) => new Image[]
{
    new Image("1", galleryId, "https://example.com/image1.jpg", "A beautiful sunrise."),
    new Image("2", galleryId, "https://example.com/image2.jpg", "A serene mountain landscape.")
});

api.MapPost("/gallery/{galleryId}/images", (string galleryId) => { });

app.Run();

record Gallery(string GalleryId, string Name, string Description);

record Image(string ImageId, string GalleryId, string Url, string Description);
