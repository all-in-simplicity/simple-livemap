using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace LiveMap.Server.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder ServeMapTiles(this IApplicationBuilder app, ServeMapTilesOptions options)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));
        if (options == null) throw new ArgumentNullException(nameof(options));

        app.UseDefaultFiles(
            new DefaultFilesOptions() { FileProvider = new PhysicalFileProvider(options.RootDirectory) });

        return app.UseStaticFiles(new StaticFileOptions()
        {
            FileProvider =
                new PhysicalFileProvider(options.RootDirectory),
            OnPrepareResponse = (context) =>
            {
                var headers = context.Context.Response.GetTypedHeaders();

                if (context.Context.Request.Headers.TryGetValue("Origin", out var origin) &&
                    options.AllowedOrigins.Contains(origin)) headers.Headers.Add("Access-Control-Allow-Origin", origin);

                headers.CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = TimeSpan.FromDays(30)
                };
            }
        });
    }
}

public sealed class ServeMapTilesOptions(string rootDirectory, string allowedOrigins)
{
    public string RootDirectory { get; set; } = rootDirectory;

    public string AllowedOrigins { get; set; } = allowedOrigins;
}