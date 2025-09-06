using System.Text.Json.Serialization;

namespace PlusStudioConverterTool.Models;

internal sealed class ConfigFile
{
    [JsonPropertyName("jsonFilterPaths")]
    public List<string> jsonFilterPaths { get; set; } = [];
}