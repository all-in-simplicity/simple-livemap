using System;
using System.IO;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using LiveMap.Server.Configurations;
using LiveMap.Server.Http;
using LiveMap.Server.LiveMap;
using LiveMap.Server.Utilities;
using LiveMap.Shared.Configurations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;

namespace LiveMap.Server;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

public class ServerMain : ServerScript
{
    public static ServerMain Self;

    public Configuration Configuration = new();

    public LiveMapScript LiveMapScript;

    public string ResourceName;

    public string ResourcePath;

    public string WebRoot;

    public ServerMain()
    {
        Self = this;

        ResourceName = API.GetCurrentResourceName();

        ResourcePath = API.GetResourcePath(ResourceName);

        WebRoot = Path.Combine(ResourcePath, "wwwroot");

        Tick += OnFirstTick;
    }

    public new PlayerList Players => base.Players;

    private async Task OnFirstTick()
    {
        Tick -= OnFirstTick;

        Configuration = LoadConfiguration(ResourcePath, "config.yml");

        Task.Factory.StartNew(UpdateChecker.CheckForUpdate);

        StartWebServer();
    }

    private Configuration LoadConfiguration(string path, string fileName)
    {
        Configuration configuration;

        try
        {
            configuration =
                LoadConfiguration<Configuration>(path, fileName);
        }
        catch (Exception ex)
        {
            var configFilePath = Path.Combine(path, fileName);

            PrintError($"Error occured on attempt to read configuration file \"{Path.GetFullPath(configFilePath)}\"");

            PrintError($"Exception: {ex.Message}");

            PrintError("Proceeding with default values");

            configuration = Configuration.Default;
        }

        return configuration;
    }

    private static T LoadConfiguration<T>(string path, string fileName)
    {
        return Yaml.Deserialize<T>(File.ReadAllText(Path.Combine(path, fileName)));
    }

    private void StartWebServer()
    {
        try
        {
            var host = new WebHostBuilder()
                .ConfigureServices(services => { services.AddSingleton<IServer, HttpServer>(); })
                .UseContentRoot(WebRoot)
                .UseStartup<Startup>()
                .Build();

            host.Start();

            RegisterScript(LiveMapScript);
        }
        catch (Exception ex)
        {
            PrintError("Failed to start webserver");

            PrintError($"Exception: {ex.Message}");
        }
    }

    private static void PrintError(string error)
    {
        Debug.WriteLine($"^1{error}^7");
    }
}