using System;
using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace Phillips
{
    public class BigNum
    {
        private BigInteger _number;
        private int _exponent;

        private const ulong DoubleFractionMask = 0xFFFFFFFFFFFFF;
        private const int DoubleExponentShiftValue = 1023;
        private const uint DoubleExponentMask = 0x7FF;
        private const uint DefaultDivisionPrecision = 30;
        private const string UndefinedString = "undefined";

        /// <summary>
        /// Gets whether or not this <see cref="BigNum"/> represents an undefinable value.
        /// </summary>
        public bool IsUndefined { get; set; }

        /// <summary>
        /// Gets whether or not this <see cref="BigNum"/> contains a negative value.
        /// </summary>
        public bool IsNegative
        {
            get
            {
                this.ThrowIfUndefined();
                return _number.Sign < 0;
            }
        }

        /// <summary>
        /// Gets whether or not this <see cref="BigNum"/> represents zero.
        /// </summary>
        public bool IsZero
        {
            get
            {
                this.ThrowIfUndefined();
                return _number.IsZero;
            }
        }

        #region Constructors
        /// <summary>
        /// Construct a lossless <see cref="BigNum"/> object with the passed string.
        /// </summary>
        /// <param name="value">The string representing the number to represent.</param>
        /// <exception cref="ArgumentException"/>
        public BigNum(string value)
        {
            value.ThrowIfNullArg();

            if (!IsValidInputString(value))
            {
                throw new ArgumentException(string.Format("Input \"{0}\": invalid format.", value));
            }

            ParseString(value);
        }

        /// <summary>
        /// Construct a <see cref="BigNum"/> object with the given double. <seealso cref="double"/>.ToString()
        /// will be used to construct the <see cref="BigNum"/> object if useDoubleToString is set to true.
        /// </summary>
        /// <param name="value">The <seealso cref="double"/> this <see cref="BigNum"/> will represent.</param>
        /// <param name="useDoubleToString">Whether or not <see cref="BigNum"/> should convert the <seealso cref="double"/>
        ///  to a string first.</param>
        public BigNum(double value, bool useDoubleToString)
        {
            if (useDoubleToString)
            {
                if (double.IsNaN(value) || double.IsNegativeInfinity(value) || double.IsPositiveInfinity(value))
                {
                    ParseDouble(double.NaN);
                    return;
                }

                ParseString(value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                ParseDouble(value);
            }
        }

        /// <summary>
        /// Construct a <see cref="BigNum"/> object manually by assigning a number and its exponent.
        /// </summary>
        /// <param name="number">The number portion of the <see cref="BigNum"/>.</param>
        /// <param name="exponent">The power to offset this <see cref="BigNum"/>'s number portion by.</param>
        public BigNum(BigInteger number, int exponent)
        {
            _number = number;
            _exponent = _number.IsZero ? 0 : exponent;
        }

        #endregion

        /// <summary>
        /// Getm an exact string representation of this <see cref="BigNum"/> object.
        /// </summary>
        public override string ToString()
        {
            if (IsUndefined)
            {
                return UndefinedString;
            }

            if (IsZero)
            {
                return "0";
            }

            StringBuilder builder = new StringBuilder();
            var stringRep = _number.ToString();
            if (_number.Sign < 0)
            {
                stringRep = stringRep.Substring(1);
                builder.Append('-');
            }

            if (_exponent == 0)
            {
                return _number.ToString();
            }

            if (_exponent < 0)
            {
                if (-_exponent > stringRep.Length)
                {
                    builder.Append('.');
                    for (int i = 0; i < -_exponent - stringRep.Length; i++)
                    {
                        builder.Append('0');
                    }
                    builder.Append(stringRep);
                }
                else
                {
                    int digitIndex = 0, toPrintBeforeDec = stringRep.Length + _exponent;
                    while (toPrintBeforeDec-- > 0)
                    {
                        builder.Append(stringRep[digitIndex++]);
                    }
                    builder.Append('.');
                    while (digitIndex < stringRep.Length)
                    {
                        builder.Append(stringRep[digitIndex++]);
                    }

                    return builder.ToString().TrimEnd('0');
                }
            }
            else
            {
                builder.Append(stringRep);
                int i = 0;
                while (i++ < _exponent)
                {
                    builder.Append('0');
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Alternatively to <see cref="BigNum"/>.ToString(), this method will return the stored number in its
        /// scientific format.
        /// </summary>
        public string GetStringScientific()
        {
            if (IsUndefined)
            {
                return UndefinedString;
            }

            string s = _number.ToString();
            string s2 = s.Substring(1);
            if (s2 == string.Empty)
            {
                s2 = "0";
            }

            return string.Format("{0}.{1}e{2}", s[0], s2, _exponent);
        }

        #region Operators
        public static BigNum operator + (BigNum lhs, BigNum rhs)
        {
            lhs.ThrowIfNullArg();
            rhs.ThrowIfNullArg();

            if (lhs.IsUndefined || rhs.IsUndefined)
            {
                // Cannot sum an undefined number
                return GetUndefinedNumber();
            }

            BigInteger num1, num2;
            int oldPower, shift;

            if (lhs._exponent > rhs._exponent)
            {
                num1 = rhs._number;
                num2 = lhs._number;
                oldPower = lhs._exponent;
                shift = lhs._exponent - rhs._exponent;
            }
            else
            {
                num1 = lhs._number;
                num2 = rhs._number;
                oldPower = rhs._exponent;
                shift = rhs._exponent - lhs._exponent;
            }

            num2 = num2 * BigInteger.Pow(10, shift);

            return new BigNum(num1 + num2, oldPower - shift);
        }

        public static BigNum operator - (BigNum lhs, BigNum rhs)
        {
            lhs.ThrowIfNullArg();
            rhs.ThrowIfNullArg();

            if (lhs.IsUndefined || rhs.IsUndefined)
            {
                return GetUndefinedNumber();
            }

            BigInteger num1, num2;
            int oldPower, shift;

            if (lhs._exponent > rhs._exponent)
            {
                num1 = rhs._number;
                num2 = lhs._number;
                oldPower = lhs._exponent;
                shift = lhs._exponent - rhs._exponent;
            }
            else
            {
                num1 = lhs._number;
                num2 = rhs._number;
                oldPower = rhs._exponent;
                shift = rhs._exponent - lhs._exponent;
            }

            num2 = num2 * BigInteger.Pow(10, shift);

            return lhs._exponent > rhs._exponent
                ? new BigNum(num2 - num1, oldPower - shift)
                : new BigNum(num1 - num2, oldPower - shift);
        }

        public static BigNum operator * (BigNum lhs, BigNum rhs)
        {
            lhs.ThrowIfNullArg();
            rhs.ThrowIfNullArg();

            if (lhs.IsUndefined || rhs.IsUndefined)
            {
                return GetUndefinedNumber();
            }

            return new BigNum(lhs._number * rhs._number, lhs._exponent + rhs._exponent);
        }

        public static BigNum operator / (BigNum lhs, BigNum rhs)
        {
            lhs.ThrowIfNullArg();
            rhs.ThrowIfNullArg();

            if (lhs.IsUndefined || rhs.IsUndefined || rhs.IsZero)
            {
                return GetUndefinedNumber();
            }

            BigInteger result = 0;
            BigInteger numerator = lhs._number;
            BigInteger denominator = rhs._number;
            bool precise = false;
            int power;

            /* Divide with a precision of up to DivisionPrecision (default of 20) */
            for (power = 0; power < DefaultDivisionPrecision; power++)
            {
                var remainder = numerator % denominator;
                result = numerator / denominator;

                if (remainder.IsZero)
                {
                    // This number could be precisely calculated, so it is safe to break
                    precise = true;
                    break;
                }

                numerator = lhs._number * BigInteger.Pow(10, power + 1);
            }

            int newPower = precise ? (lhs._exponent - rhs._exponent) - 1: power - 1;
            return new BigNum(result, newPower * -1);
        }

        public static bool operator < (BigNum lhs, BigNum rhs)
        {
            lhs.ThrowIfNullArg();
            rhs.ThrowIfNullArg();

            if (lhs.IsUndefined || rhs.IsUndefined)
            {
                // Neither operator can be undefined (because they would then be equal).
                // Furthermore, it is invalid to compare an undefined number against a
                // definable number. In this case, return false.
                return false;
            }

            var subbed = lhs - rhs;
            if (subbed.IsZero)
            {
                // If equal, subtracting left and right will yield zero -- this is not desired here.
                return false;
            }

            // If left is greater than right, the subtraction of left and right will yield a negative value.
            return subbed.IsNegative;
        }

        public static bool operator > (BigNum lhs, BigNum rhs)
        {
            lhs.ThrowIfNullArg();
            rhs.ThrowIfNullArg();

            if (lhs.IsUndefined || rhs.IsUndefined)
            {
                // Neither operator can be undefined (because they would then be equal).
                // Furthermore, it is invalid to compare an undefined number against a
                // definable number. In this case, return false.
                return false;
            }

            var subbed = lhs - rhs;
            if (subbed.IsZero)
            {
                // If equal, subtracting left and right will yield zero -- this is not desired here.
                return false;
            }

            // If left is greater than right, the subtraction of left and right will yield a positive value.
            return !subbed.IsNegative;
        }

        public static bool operator <= (BigNum lhs, BigNum rhs)
        {
            lhs.ThrowIfNullArg();
            rhs.ThrowIfNullArg();

            if (lhs.IsUndefined && rhs.IsUndefined)
            {
                //  Two undefined numbers are equal to each other.
                return true;
            }

            if ((lhs.IsUndefined && !rhs.IsUndefined) || (!lhs.IsUndefined && rhs.IsUndefined))
            {
                // An undefined number may never compare against a definable number.
                return false;
            }

            var subbed = lhs - rhs;
            if (subbed.IsZero)
            {
                // If equal, subtracting left and right will yield zero.
                return true;
            }

            // If left is greater than right, the subtraction of left and right will yield a negative value.
            return subbed.IsNegative;
        }

        public static bool operator >= (BigNum lhs, BigNum rhs)
        {
            lhs.ThrowIfNullArg();
            rhs.ThrowIfNullArg();

            if (lhs.IsUndefined && rhs.IsUndefined)
            {
                //  Two undefined numbers are equal to each other.
                return true;
            }

            if ((lhs.IsUndefined && !rhs.IsUndefined) || (!lhs.IsUndefined && rhs.IsUndefined))
            {
                // An undefined number may never compare against a definable number.
                return false;
            }

            var subbed = lhs - rhs;
            if (subbed.IsZero)
            {
                // If equal, subtracting left and right will yield zero.
                return true;
            }

            // If left is greater than right, the subtraction of left and right will yield a positive value.
            return !subbed.IsNegative;
        }
        #endregion

        #region Input Handling
        private static bool IsValidInputString(string value)
        {
            // The string cannot be null or empty
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            // The string cannot contain whitespace
            if (Regex.IsMatch(value, @"\s+"))
            {
                return false;
            }

            // The string may only start with '.', '-', or a digit [0-9]
            if (Regex.IsMatch(value, @"^[^\d-\.]"))
            {
                return false;
            }

            // There may only be up to one occurance of the decimal
            if (Regex.IsMatch(value, @"[\.]{2,}"))
            {
                return false;
            }

            // The negative symbol may only be in the first position
            if (Regex.IsMatch(value, @".+-+.*"))
            {
                if (!value.Contains("E-"))
                {
                    return false;
                }
            }

            if (Regex.IsMatch(value, @"[a-df-zA-DF-Z]"))
            {
                return false;
            }

            return true;
        }

        private unsafe void ParseDouble(double value)
        {
            if (double.IsNegativeInfinity(value) || double.IsPositiveInfinity(value) || double.IsNaN(value))
            {
                IsUndefined = true;

                _exponent = 0;
                _number = 0;
            }
            else
            {
                ulong bits = ((ulong*)&value)[0];

                /* Determine whether or not the number is negative. */
                bool isNegative = (int)((bits >> 63) & 0x1) != 0;

                /* Grab the exponent portion of the double (63 - 52 = 11 bits max) */
                int shift = (int)((int)(bits >> 52) & DoubleExponentMask);
                shift -= DoubleExponentShiftValue;

                /* Grab the exponent portion */
                ulong fraction = bits & DoubleFractionMask;

                if (shift != 0)
                {
                    fraction |= (ulong) BigInteger.Pow(2, 52);
                }

                BigNum fractionBigNum = new BigNum(fraction.ToString());
                fractionBigNum *= Pow2(shift - 52);

                _number = fractionBigNum._number;
                _exponent = fractionBigNum._exponent;

                if (fraction == 0 && shift == 0)
                {
                    _number = 1;
                }
                if (isNegative)
                {
                    _number *= -1;
                }
            }
        }

        private void ParseString(string value)
        {
            if (value.Contains("E"))
            {
                value = HandleScientificInput(value);
            }

            bool isNegative;
            if (value[0] == '-')
            {
                isNegative = true;
                value = value.Substring(1);
            }
            else
            {
                isNegative = false;
            }

            IsUndefined = false;

            int decimalIndex = value.IndexOf('.');

            if (decimalIndex < 0)
            {
                // The string represents an integer with no decimal
                if (value == "0")
                {
                    _exponent = 0;
                    _number = 0;
                }
                else
                {
                    var trimmed = value.Trim('0');
                    _exponent = value.Length - trimmed.Length;
                    _number = BigInteger.Parse(trimmed);
                    if (isNegative)
                    {
                        _number *= -1;
                    }
                }
                return;
            }

            value = value.Trim('0');
            decimalIndex = value.IndexOf('.');

            if (value == ".")
            {
                _number = 0;
                _exponent = 0;
                return;
            }

            if (decimalIndex == value.Length - 1)
            {
                // After removing redundant characters, the string was
                // found to still represent an integer with no decimal
                value = value.Replace(".", string.Empty);
                var trimmed = value.Trim('0');
                _exponent = value.Length - trimmed.Length;
                _number = BigInteger.Parse(trimmed);
            }
            else if (decimalIndex == 0)
            {
                // The string represents a value with no integer and
                // just a decimal portion.
                _exponent = (value.Length - 1) * -1;
                /*
                for (int i = decimalIndex + 1; i < value.Length - 1 && value[i] == '0'; i++)
                {
                    _exponent--;
                }*/

                BigInteger.TryParse(value.Replace(".", string.Empty), out _number);
            }
            else
            {
                // The string represents a value containing both an integer
                // portion and a decimal portion
                _number = BigInteger.Parse(value.Replace(".", string.Empty));
                _exponent = -(_number.ToString().Length - decimalIndex);
            }

            if (isNegative)
            {
                _number *= -1;
            }
        }

        private string HandleScientificInput(string value)
        {
            StringBuilder builder = new StringBuilder();

            if (value[0] == '-')
            {
                builder.Append('-');
                value = value.Substring(1);
            }

            var eIndex = value.IndexOf("E", StringComparison.OrdinalIgnoreCase);
            var baseNum = value.Substring(0, eIndex);
            var exp = int.Parse(value.Substring(eIndex + 1));
            var decIndex = baseNum.IndexOf('.');

            if (decIndex == 0)
            {
                baseNum = baseNum.Substring(1);
                if (exp <= 0)
                {
                    builder.Append('.');
                    for (int i = 0; i < -exp; i++)
                    {
                        builder.Append('0');
                    }
                    builder.Append(baseNum);
                }
                else
                {
                    if (baseNum.Length < exp)
                    {
                        builder.Append(baseNum);
                        for (int i = 0; i < exp - baseNum.Length; i++)
                        {
                            builder.Append('0');
                        }
                    }
                    else
                    {
                        for (int i = 0; i < exp; i++)
                        {
                            builder.Append(baseNum[i]);
                        }
                        builder.Append('.');
                        for (int i = decIndex + exp; i < baseNum.Length; i++)
                        {
                            builder.Append(baseNum[i]);
                        }
                    }
                }
                return builder.ToString();
            }

            if (decIndex < 0)
            {
                if (exp < 0)
                {
                    if (baseNum.Length <= -exp)
                    {
                        builder.Append('.');
                        int i = baseNum.Length;
                        while (i < -exp)
                        {
                            builder.Append('0');
                            i++;
                        }
                        builder.Append(baseNum);
                    }
                    else
                    {
                        for (int i = 0; i < -exp; i++)
                        {
                            builder.Append(baseNum[i]);
                        }
                        builder.Append('.');
                        for (int i = exp; i < baseNum.Length; i++)
                        {
                            builder.Append(baseNum[i]);
                        }
                    }
                }
                else
                {
                    builder.Append(baseNum);
                    for (int i = 0; i < exp; i++)
                    {
                        builder.Append('0');
                    }
                }
            }
            else
            {
                if (exp == 0)
                {
                    builder.Append(baseNum);
                }
                else if (exp < 0)
                {
                    baseNum = baseNum.Replace(".", string.Empty);

                    if (decIndex + exp < 0)
                    {
                        builder.Append('.');
                        int i = 0;
                        while (i < decIndex + exp)
                        {
                            builder.Append('0');
                            i++;
                        }

                        builder.Append(baseNum);
                    }
                    else
                    {
                        for (int i = 0; i < decIndex + exp; i++)
                        {
                            builder.Append(baseNum[i]);
                        }
                        builder.Append('.');
                        for (int i = decIndex + exp; i < baseNum.Length; i++)
                        {
                            builder.Append(baseNum[i]);
                        }
                    }
                }
                else
                {
                    baseNum = baseNum.Replace(".", string.Empty);

                    if (decIndex + exp < baseNum.Length)
                    {
                        for (int i = 0; i < decIndex + exp; i++)
                        {
                            builder.Append(baseNum[i]);
                        }
                        builder.Append('.');
                        for (int i = decIndex + exp; i < baseNum.Length; i++)
                        {
                            builder.Append(baseNum[i]);
                        }
                    }
                    else
                    {
                        builder.Append(baseNum);
                        for (int i = 0; i < decIndex + exp - baseNum.Length; i++)
                        {
                            builder.Append('0');
                        }
                    }
                }
            }

            return builder.ToString();
        }
        #endregion

        #region Utility
        private static BigInteger BitsToInteger(BitArray bits)
        {
            byte[] bytes = new byte[(ulong)Math.Ceiling((double)bits.Length / 8)];
            bits.Reverse();
            bits.CopyTo(bytes, 0);

            return new BigInteger(bytes);
        }

        /// <summary>
        /// Determines whether or not <seealso cref="double"/>.ToString(), for the specified value, generates an
        /// exact representation of the stored value.
        /// </summary>
        /// <param name="value">The <seealso cref="double"/> to check.</param>
        /// <returns>True if the <seealso cref="double"/>'s .ToString() is exact; false otherwise.</returns>
        public static bool IsToStringCorrect(double value)
        {
            BigNum doubleVersion = new BigNum(value, false);
            BigNum stringVersion = new BigNum(value, true);

            var doubleVersionString = doubleVersion.ToString();
            var stringVersrionString = stringVersion.ToString();

            if (doubleVersionString.Length < stringVersrionString.Length)
            {
                return false;
            }

            for (int i = 0; i < doubleVersionString.Length; i++)
            {
                if (doubleVersionString[i] != stringVersrionString[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns an undefined <see cref="BigNum"/> representation.
        /// </summary>
        public static BigNum GetUndefinedNumber()
        {
            return new BigNum(double.NaN, false);
        }

        public static BigNum Pow2(int exponent)
        {
            if (exponent < 0)
            {
                var one = new BigNum("1");
                return one/Pow2(-exponent);
            }

            return new BigNum(BigInteger.Pow(2, exponent).ToString());
        }
        #endregion

        public static implicit operator BigNum (string value)
        {
            return new BigNum(value);
        }

        public static implicit operator BigNum(double value)
        {
            return new BigNum(value, false);
        }
    }
}
