/*
 * Code taken from: https://github.com/bogdanbujdea/csharpbase36/blob/master/Base36Encoder/Base36.cs
 */

using System.Linq;
using System.Numerics;
using System;

namespace JiraExport.RevisionUtils
{
    public static class Base36
    {
        private const string Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static long Decode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Empty value.");
            value = value.ToUpper();
            var negative = false;
            if (value[0] == '-')
            {
                negative = true;
                value = value.Substring(1, value.Length - 1);
            }
            if (value.Any(c => !Digits.Contains(c)))
                throw new ArgumentException("Invalid value: \"" + value + "\".");

            var decoded = 0L;
            for (var i = 0; i < value.Length; ++i)
            {
                decoded += Digits.IndexOf(value[i]) * (long) BigInteger.Pow(Digits.Length, value.Length - i - 1);
            }

            return negative ? decoded * -1 : decoded;
        }

        public static string Encode(long value)
        {
            if (value == long.MinValue)
            {
                //hard coded value due to error when getting absolute value below: "Negating the minimum value of a twos complement number is invalid.".
                return "-1Y2P0IJ32E8E8";
            }
            var negative = value < 0;
            value = Math.Abs(value);
            var encoded = string.Empty;
            do
                encoded = Digits[(int)(value % Digits.Length)] + encoded;
            while ((value /= Digits.Length) != 0);
            return negative ? "-" + encoded : encoded;
        }
    }
}
