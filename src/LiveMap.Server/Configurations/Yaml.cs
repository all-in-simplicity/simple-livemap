using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LiveMap.Server.Configurations;

public static class Yaml
{
    public static T Deserialize<T>(string yaml)
    {
        return Deserializer().Deserialize<T>(yaml);
    }

    private static IDeserializer Deserializer()
    {
        return new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
    }
}