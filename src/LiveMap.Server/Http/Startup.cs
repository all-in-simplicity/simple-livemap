using Lib.AspNetCore.ServerSentEvents;
using LiveMap.Server.Extensions;
using LiveMap.Server.LiveMap;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LiveMap.Server.Http;

public sealed class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddServerSentEvents<IServerSentEventsService, ServerSentEventsService>(options =>
        {
            options.KeepaliveMode = ServerSentEventsKeepaliveMode.Always;
            options.KeepaliveInterval = 20;
        });

        services.AddMvc()
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

        services.AddSingleton(builder => new LiveMapScript(builder.GetService<ServerSentEventsService>()));
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        ServerMain.Self.LiveMapScript = app.ApplicationServices.GetService<LiveMapScript>();

        app.ServeMapTiles(new ServeMapTilesOptions(ServerMain.Self.WebRoot,
            ServerMain.Self.Configuration.AllowedOrigins));

        app.MapServerSentEvents<ServerSentEventsService>("/sse", new ServerSentEventsOptions()
        {
            OnPrepareAccept = (response) =>
            {
                response.Headers.Add("Access-Control-Allow-Origin",
                    ServerMain.Self.Configuration.ServerSentEvents.AllowedOrigins);
            }
        });

        app.UseMvc();
    }
}