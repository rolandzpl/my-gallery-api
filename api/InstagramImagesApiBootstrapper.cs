using Microsoft.AspNetCore.Mvc;
using static Lithium.Imaging.Processing;

namespace Lithium.Gallery.Api;

public static class InstagramImagesApiBootstrapper
{
    public static IServiceCollection AddInstagramImages(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication UseInstagramImages(this WebApplication app)
    {
        app.MapPost("/ig/images/converted", (
            IFormFile file,
            [FromQuery] int? imgQual,
            [FromQuery] int padding = 50,
            [FromQuery] int targetWidth = 2048,
            [FromQuery] int targetHeight = 2048,
            [FromQuery] int borderWidth = 20) =>
        {
            return Results.Stream(async (outStream) =>
            {
                await ConvertImage(
                    file.OpenReadStream(),
                    outStream,
                    padding,
                    targetWidth,
                    targetHeight,
                    imgQual,
                    borderWidth
                );
            }, fileDownloadName: file.FileName);
        })
        .DisableAntiforgery();

        return app;
    }
}