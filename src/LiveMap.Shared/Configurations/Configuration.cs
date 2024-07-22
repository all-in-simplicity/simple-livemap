namespace LiveMap.Shared.Configurations;

public sealed class Configuration
{
    public static Configuration Default = new()
    {
        AllowedOrigins = "",
        ServerSentEvents = new ServerSentEventsConfiguration() { TickInterval = 1250, AllowedOrigins = "" }
    };

    public string AllowedOrigins { get; set; } = string.Empty;

    public ServerSentEventsConfiguration ServerSentEvents { get; set; } = new();

    public class ServerSentEventsConfiguration
    {
        public uint TickInterval { get; set; }

        public string AllowedOrigins { get; set; } = string.Empty;
    }
}