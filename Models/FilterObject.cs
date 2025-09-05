using Newtonsoft.Json;

namespace PlusStudioConverterTool.Models;

[JsonObject]
internal sealed class FilterObject()
{
    [JsonRequired]
    public string AreaType = "Object";
    [JsonRequired]
    public Dictionary<string, string> replacements = [];

    [JsonRequired]
    public HashSet<string> exclusions = [];
}