using System;
using System.Globalization;

public static class BigNumberFormatter
{
    private const int AlphabetLength = 26;
    private static readonly string[] s_fixedUnits = { string.Empty, "K", "M", "B", "T" };

    public static string Format(BigNumber value)
    {
        if (value.IsZero)
            return "0.0";

        var negative = value.Sign < 0;
        var absolute = BigNumber.Abs(value);
        var unitIndex = GetUnitIndex(absolute.Exponent);
        var coefficient = GetCoefficient(absolute, unitIndex);
        var rounded = Math.Round(coefficient, 1, MidpointRounding.ToEven);

        if (rounded >= 10000d)
        {
            unitIndex++;
            coefficient /= 1000d;
            rounded = Math.Round(coefficient, 1, MidpointRounding.ToEven);
        }

        var sign = negative ? "-" : string.Empty;
        return sign + rounded.ToString("F1", CultureInfo.InvariantCulture) + GetUnit(unitIndex);
    }

    public static string GetUnit(long unitIndex)
    {
        if (unitIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(unitIndex));

        if (unitIndex < s_fixedUnits.Length)
            return s_fixedUnits[(int)unitIndex];

        var remaining = (ulong)(unitIndex - s_fixedUnits.Length);
        var length = 2;

        while (true)
        {
            var blockSize = Pow26(length);
            var allCaseBlocksSize = checked(blockSize * 3UL);
            if (remaining < allCaseBlocksSize)
            {
                var caseBlock = remaining / blockSize;
                var indexInBlock = remaining % blockSize;
                return EncodeUnit(indexInBlock, length, caseBlock);
            }

            remaining -= allCaseBlocksSize;
            length++;
        }
    }

    private static long GetUnitIndex(long exponent)
    {
        return exponent < 4L ? 0L : (exponent - 1L) / 3L;
    }

    private static double GetCoefficient(BigNumber value, long unitIndex)
    {
        var unitExponent = unitIndex * 3L;
        var scaleExponent = value.Exponent - unitExponent;
        return value.Mantissa * Math.Pow(10d, scaleExponent);
    }

    private static ulong Pow26(int exponent)
    {
        var result = 1UL;
        for (var i = 0; i < exponent; i++)
            result = checked(result * AlphabetLength);

        return result;
    }

    private static string EncodeUnit(ulong index, int length, ulong caseBlock)
    {
        var characters = new char[length];
        for (var position = length - 1; position >= 0; position--)
        {
            characters[position] = (char)('a' + index % AlphabetLength);
            index /= AlphabetLength;
        }

        if (caseBlock == 1UL)
            characters[0] = char.ToUpperInvariant(characters[0]);
        else if (caseBlock == 2UL)
        {
            for (var i = 0; i < characters.Length; i++)
                characters[i] = char.ToUpperInvariant(characters[i]);
        }

        return new string(characters);
    }
}
