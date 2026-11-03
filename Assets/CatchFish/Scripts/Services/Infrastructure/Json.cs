using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

/// <summary>JSON settings shared by everything that talks to the backend: camelCase like the server docs.</summary>
public static class Json
{
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        // Dictionary keys are ids ("f1", "lineLength"), keep them as they are.
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = false },
        },
        Formatting = Formatting.Indented,
    };

    public static string Serialize(object value) => JsonConvert.SerializeObject(value, Settings);

    public static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);
}
