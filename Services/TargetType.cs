namespace CBLDtoBLD.Services;

internal enum TargetType
{
    Null = 0,
    CBLDtoBLD = 1,
    BLDtoEBPL = 2,
    CBLDtoRBPL = 3,
    RBPLtoEBPL = 4
}

internal static class TargetTypeExtensions
{
    public static string ToExtension(this TargetType type) => type switch
    {
        TargetType.BLDtoEBPL => ".bld",
        TargetType.CBLDtoBLD => ".cbld",
        TargetType.CBLDtoRBPL => ".cbld",
        TargetType.RBPLtoEBPL => ".rbpl",
        _ => string.Empty
    };
}