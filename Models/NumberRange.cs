using System.Diagnostics.CodeAnalysis;

namespace PlusStudioConverterTool.Models;

public readonly struct NumberRange
{
    readonly public int Min;
    readonly public int Max;
    readonly public int Difference => Max - Min;
    public NumberRange(int min, int max)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);
        Min = min;
        Max = max;
    }
    public bool InInclusiveRange(int num) =>
        Min <= num && num <= Max;
    public bool InExclusiveRange(int num) =>
        Min < num && num < Max;
    public bool InMinInclusiveRange(int num) =>
        Min <= num && num < Max;
    public bool InMaxInclusiveRange(int num) =>
        Min < num && num <= Max;
    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is NumberRange NumberRange && NumberRange.Min == Min && NumberRange.Max == Max;
    public override int GetHashCode() =>
        HashCode.Combine(Min, Max);
    public override string ToString() => $"({Min}, {Max})";

    public static bool operator ==(NumberRange left, NumberRange right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NumberRange left, NumberRange right)
    {
        return !(left == right);
    }

    public static bool TryParse(string? str, out NumberRange? range)
    {
        range = null;
        if (str == null)
            return false;

        str = str.Trim(); // let's make sure it is exactly as this follows
        string[] strNums = str.Split("..", StringSplitOptions.RemoveEmptyEntries);
        if (strNums.Length != 2) // If it is not (1..x), just remove it
            return false;
        if (!int.TryParse(strNums[0], out int num1)) return false; // Check for min
        if (!int.TryParse(strNums[1], out int num2)) return false; // Check for max

        // Set the actual NumberRange
        range = new(num1, num2);
        return true;
    }
}