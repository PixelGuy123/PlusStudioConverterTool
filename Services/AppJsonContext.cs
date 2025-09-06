using System.Text.Json.Serialization;
using PlusStudioConverterTool.Models;

namespace PlusStudioConverterTool.Services;
// For trimmed option not affect these classes
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = true, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ConfigFile))]
[JsonSerializable(typeof(FilterObject))]
internal partial class AppJsonContext : JsonSerializerContext
{
}
