using Newtonsoft.Json;
namespace PlusStudioConverterTool.Models;

[JsonObject]
internal sealed class ConfigFile
{
    public List<string> jsonFilterPaths = [];
}