using System.Text.Json.Serialization;

namespace PlusStudioConverterTool.Models;

internal sealed class FilterObject
{
    [JsonPropertyName("AreaType")]
    public string AreaType { get; set; } = "Object";

    [JsonPropertyName("replacements")]
    public Dictionary<string, string> replacements { get; set; } = [];

    [JsonPropertyName("exclusions")]
    public HashSet<string> exclusions { get; set; } = [];
}