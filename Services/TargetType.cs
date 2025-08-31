namespace CBLDtoBLD.Services;

internal enum TargetType
{
    Null = 0,
    CBLD = 1,
    BLD = 2
}

internal static class TargetTypeExtensions
{
    public static string ToExtension(this TargetType type) => type switch
    {
        TargetType.BLD => ".bld",
        TargetType.CBLD => ".cbld",
        _ => string.Empty
    };

    public static TargetType ToTargetType(this string extension) => extension.ToLowerInvariant().Trim('.') switch
    {
        "bld" => TargetType.BLD,
        "cbld" => TargetType.CBLD,
        _ => TargetType.Null
    };
}