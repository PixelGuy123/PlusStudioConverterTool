using PlusStudioConverterTool.Models;

namespace PlusStudioConverterTool.Extensions;

internal static class TargetTypeExtensions
{
    public static string ToExtension(this TargetType type) => type switch
    {
        TargetType.BLDtoEBPL => ".bld",
        TargetType.CBLDtoBLD => ".cbld",
        TargetType.CBLDtoRBPL => ".cbld",
        TargetType.RBPLtoEBPL => ".rbpl",
        TargetType.PBPLtoEBPL => ".pbpl",
        TargetType.BPLtoEBPL => ".bpl",
        _ => string.Empty
    };
}