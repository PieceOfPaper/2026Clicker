using System;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// Stores a large signed number as a normalized double mantissa and a base-10 exponent.
/// BigNumber is intended for game values where roughly 15 significant digits are sufficient.
/// </summary>
[Serializable]
public struct BigNumber : IComparable<BigNumber>, IEquatable<BigNumber>, IFormattable
{
    private const int SignificantDigits = 17;

    [SerializeField] private double _mantissa;
    [SerializeField] private long _exponent;

    public double Mantissa => _mantissa;
    public long Exponent => _exponent;
    public bool IsZero => _mantissa == 0d;
    public int Sign => Math.Sign(_mantissa);

    public static BigNumber Zero => default;
    public static BigNumber One => new(1d, 0L);
    public static BigNumber MinValue => new(-9.999999999999999d, long.MaxValue);
    public static BigNumber MaxValue => new(9.999999999999999d, long.MaxValue);

    public BigNumber(double value)
        : this(value, 0L)
    {
    }

    public BigNumber(double mantissa, long exponent)
    {
        if (double.IsNaN(mantissa) || double.IsInfinity(mantissa))
            throw new ArgumentOutOfRangeException(nameof(mantissa), "Mantissa must be finite.");

        _mantissa = mantissa;
        _exponent = exponent;
        Normalize(ref _mantissa, ref _exponent);
    }

    public static BigNumber Abs(BigNumber value) => value.Sign < 0 ? -value : value;
    public static BigNumber Min(BigNumber left, BigNumber right) => left <= right ? left : right;
    public static BigNumber Max(BigNumber left, BigNumber right) => left >= right ? left : right;
    public static BigNumber Clamp(BigNumber value, BigNumber min, BigNumber max)
    {
        if (min > max)
            throw new ArgumentException("Minimum cannot be greater than maximum.");

        return Max(min, Min(max, value));
    }

    public static BigNumber Pow(BigNumber value, double power)
    {
        if (value.IsZero)
        {
            if (power < 0d)
                throw new DivideByZeroException();

            return power == 0d ? One : Zero;
        }

        if (value.Sign < 0 && power != Math.Truncate(power))
            throw new ArgumentOutOfRangeException(nameof(power), "A negative value requires an integer power.");

        var logarithm = Math.Log10(Math.Abs(value._mantissa)) + value._exponent;
        var resultLogarithm = logarithm * power;
        if (resultLogarithm > long.MaxValue || resultLogarithm < long.MinValue)
            throw new OverflowException("The result exponent is outside the BigNumber range.");

        var exponent = (long)Math.Floor(resultLogarithm);
        var mantissa = Math.Pow(10d, resultLogarithm - exponent);
        if (value.Sign < 0 && ((long)Math.Abs(power) & 1L) != 0L)
            mantissa = -mantissa;

        return new BigNumber(mantissa, exponent);
    }

    public static BigNumber Sqrt(BigNumber value)
    {
        if (value.Sign < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Cannot calculate the square root of a negative value.");

        return Pow(value, 0.5d);
    }

    public double Log10()
    {
        if (Sign <= 0)
            throw new InvalidOperationException("Log10 is only defined for positive values.");

        return Math.Log10(_mantissa) + _exponent;
    }

    public static BigNumber Parse(string text) => Parse(text, CultureInfo.InvariantCulture);

    public static BigNumber Parse(string text, IFormatProvider provider)
    {
        if (!TryParse(text, provider, out var value))
            throw new FormatException($"'{text}' is not a valid BigNumber.");

        return value;
    }

    public static bool TryParse(string text, out BigNumber value) =>
        TryParse(text, CultureInfo.InvariantCulture, out value);

    public static bool TryParse(string text, IFormatProvider provider, out BigNumber value)
    {
        value = Zero;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();
        var exponentIndex = text.IndexOfAny(new[] { 'e', 'E' });
        if (exponentIndex < 0)
            return TryParseFull(text, provider, out value);

        if (text.IndexOfAny(new[] { 'e', 'E' }, exponentIndex + 1) >= 0)
            return false;

        var coefficientText = text.Substring(0, exponentIndex);
        var exponentText = text.Substring(exponentIndex + 1);
        if (!long.TryParse(exponentText, NumberStyles.AllowLeadingSign, provider, out var explicitExponent) ||
            !TryParseFull(coefficientText, provider, out var coefficient))
        {
            return false;
        }

        try
        {
            value = coefficient.IsZero
                ? Zero
                : new BigNumber(coefficient._mantissa, checked(coefficient._exponent + explicitExponent));
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    /// <summary>
    /// Parses an ordinary decimal string containing every digit, without converting the whole
    /// string to double first. Digits beyond double precision are rounded to the nearest retained
    /// significant digit.
    /// </summary>
    public static BigNumber ParseFull(string text) => ParseFull(text, CultureInfo.InvariantCulture);

    public static BigNumber ParseFull(string text, IFormatProvider provider)
    {
        if (!TryParseFull(text, provider, out var value))
            throw new FormatException($"'{text}' is not a valid full decimal BigNumber.");

        return value;
    }

    public static bool TryParseFull(string text, out BigNumber value) =>
        TryParseFull(text, CultureInfo.InvariantCulture, out value);

    public static bool TryParseFull(string text, IFormatProvider provider, out BigNumber value)
    {
        value = Zero;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();
        var negative = false;
        var index = 0;
        if (text[0] == '+' || text[0] == '-')
        {
            negative = text[0] == '-';
            index++;
        }

        if (index >= text.Length)
            return false;

        var separator = NumberFormatInfo.GetInstance(provider).NumberDecimalSeparator;
        if (string.IsNullOrEmpty(separator))
            separator = ".";

        var digits = new StringBuilder(text.Length);
        var integerDigits = -1;
        for (; index < text.Length; index++)
        {
            var character = text[index];
            if (character >= '0' && character <= '9')
            {
                digits.Append(character);
                continue;
            }

            if (integerDigits < 0 && MatchesAt(text, index, separator))
            {
                integerDigits = digits.Length;
                index += separator.Length - 1;
                continue;
            }

            return false;
        }

        if (digits.Length == 0)
            return false;

        if (integerDigits < 0)
            integerDigits = digits.Length;

        var firstSignificant = 0;
        while (firstSignificant < digits.Length && digits[firstSignificant] == '0')
            firstSignificant++;

        if (firstSignificant == digits.Length)
        {
            value = Zero;
            return true;
        }

        long exponent;
        try
        {
            exponent = checked((long)integerDigits - firstSignificant - 1L);
        }
        catch (OverflowException)
        {
            return false;
        }

        var retainedDigits = Math.Min(SignificantDigits, digits.Length - firstSignificant);
        var mantissaText = new StringBuilder(retainedDigits + 1);
        mantissaText.Append(digits[firstSignificant]);
        if (retainedDigits > 1)
        {
            mantissaText.Append('.');
            for (var i = 1; i < retainedDigits; i++)
                mantissaText.Append(digits[firstSignificant + i]);
        }

        var mantissa = double.Parse(mantissaText.ToString(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        var roundingIndex = firstSignificant + retainedDigits;
        if (roundingIndex < digits.Length && digits[roundingIndex] >= '5')
            mantissa += Math.Pow(10d, 1 - retainedDigits);

        value = new BigNumber(negative ? -mantissa : mantissa, exponent);
        return true;
    }

    public override string ToString() => ToString("G", CultureInfo.InvariantCulture);
    public string ToString(string format) => ToString(format, CultureInfo.InvariantCulture);

    public string ToString(string format, IFormatProvider formatProvider)
    {
        formatProvider ??= CultureInfo.InvariantCulture;
        format = string.IsNullOrEmpty(format) ? "G" : format;

        if (IsZero)
            return 0d.ToString(format == "E" ? "E" : "G", formatProvider);

        switch (format.ToUpperInvariant())
        {
            case "G":
                if (_exponent >= -4 && _exponent <= 15)
                    return ToDouble().ToString("G17", formatProvider);
                return $"{_mantissa.ToString("G17", formatProvider)}e{_exponent.ToString(CultureInfo.InvariantCulture)}";
            case "E":
                return $"{_mantissa.ToString("G17", formatProvider)}e{(_exponent >= 0 ? "+" : string.Empty)}{_exponent.ToString(CultureInfo.InvariantCulture)}";
            default:
                return _mantissa.ToString(format, formatProvider) + "e" + _exponent.ToString(CultureInfo.InvariantCulture);
        }
    }

    public double ToDouble()
    {
        if (IsZero)
            return 0d;

        if (_exponent > 308)
            return Sign > 0 ? double.PositiveInfinity : double.NegativeInfinity;
        if (_exponent < -324)
            return Sign > 0 ? 0d : -0d;

        return _mantissa * Math.Pow(10d, _exponent);
    }

    public int CompareTo(BigNumber other)
    {
        if (Sign != other.Sign)
            return Sign.CompareTo(other.Sign);
        if (IsZero)
            return 0;

        var exponentComparison = _exponent.CompareTo(other._exponent);
        return Sign > 0 ? exponentComparison != 0 ? exponentComparison : _mantissa.CompareTo(other._mantissa)
            : exponentComparison != 0 ? -exponentComparison : _mantissa.CompareTo(other._mantissa);
    }

    public bool Equals(BigNumber other) => _mantissa.Equals(other._mantissa) && _exponent == other._exponent;
    public override bool Equals(object obj) => obj is BigNumber other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(_mantissa, _exponent);

    public static BigNumber operator +(BigNumber left, BigNumber right)
    {
        if (left.IsZero)
            return right;
        if (right.IsZero)
            return left;

        if (left._exponent < right._exponent)
            (left, right) = (right, left);

        var difference = (ulong)left._exponent - (ulong)right._exponent;
        if (difference > SignificantDigits)
            return left;

        return new BigNumber(left._mantissa + right._mantissa * Math.Pow(10d, -(double)difference), left._exponent);
    }

    public static BigNumber operator -(BigNumber left, BigNumber right) => left + -right;
    public static BigNumber operator -(BigNumber value) => new(-value._mantissa, value._exponent);
    public static BigNumber operator +(BigNumber value) => value;

    public static BigNumber operator *(BigNumber left, BigNumber right)
    {
        if (left.IsZero || right.IsZero)
            return Zero;

        return new BigNumber(left._mantissa * right._mantissa, checked(left._exponent + right._exponent));
    }

    public static BigNumber operator /(BigNumber left, BigNumber right)
    {
        if (right.IsZero)
            throw new DivideByZeroException();
        if (left.IsZero)
            return Zero;

        return new BigNumber(left._mantissa / right._mantissa, checked(left._exponent - right._exponent));
    }

    public static BigNumber operator ++(BigNumber value) => value + One;
    public static BigNumber operator --(BigNumber value) => value - One;

    public static bool operator ==(BigNumber left, BigNumber right) => left.Equals(right);
    public static bool operator !=(BigNumber left, BigNumber right) => !left.Equals(right);
    public static bool operator <(BigNumber left, BigNumber right) => left.CompareTo(right) < 0;
    public static bool operator >(BigNumber left, BigNumber right) => left.CompareTo(right) > 0;
    public static bool operator <=(BigNumber left, BigNumber right) => left.CompareTo(right) <= 0;
    public static bool operator >=(BigNumber left, BigNumber right) => left.CompareTo(right) >= 0;

    public static implicit operator BigNumber(sbyte value) => new(value);
    public static implicit operator BigNumber(byte value) => new(value);
    public static implicit operator BigNumber(short value) => new(value);
    public static implicit operator BigNumber(ushort value) => new(value);
    public static implicit operator BigNumber(int value) => new(value);
    public static implicit operator BigNumber(uint value) => new(value);
    public static implicit operator BigNumber(long value) => ParseFull(value.ToString(CultureInfo.InvariantCulture));
    public static implicit operator BigNumber(ulong value) => ParseFull(value.ToString(CultureInfo.InvariantCulture));
    public static implicit operator BigNumber(float value) => new(value);
    public static implicit operator BigNumber(double value) => new(value);
    public static implicit operator BigNumber(decimal value) => ParseFull(value.ToString(CultureInfo.InvariantCulture));

    public static explicit operator double(BigNumber value) => value.ToDouble();
    public static explicit operator float(BigNumber value) => (float)value.ToDouble();
    public static explicit operator decimal(BigNumber value) => decimal.Parse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);
    public static explicit operator long(BigNumber value) => checked((long)value.ToDouble());
    public static explicit operator int(BigNumber value) => checked((int)value.ToDouble());

    private static void Normalize(ref double mantissa, ref long exponent)
    {
        if (mantissa == 0d)
        {
            exponent = 0L;
            return;
        }

        var adjustment = (long)Math.Floor(Math.Log10(Math.Abs(mantissa)));
        adjustment = Math.Max(-323L, Math.Min(308L, adjustment));
        mantissa /= Math.Pow(10d, adjustment);
        exponent = checked(exponent + adjustment);

        if (Math.Abs(mantissa) >= 10d)
        {
            mantissa /= 10d;
            exponent = checked(exponent + 1L);
        }
        else if (Math.Abs(mantissa) < 1d)
        {
            mantissa *= 10d;
            exponent = checked(exponent - 1L);
        }
    }

    private static bool MatchesAt(string text, int index, string value)
    {
        if (index + value.Length > text.Length)
            return false;

        return string.CompareOrdinal(text, index, value, 0, value.Length) == 0;
    }
}
