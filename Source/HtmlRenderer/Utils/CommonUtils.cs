// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they bagin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using HtmlRenderer.Entities;

namespace HtmlRenderer.Utils
{
    public delegate void Action<T>(T obj);

    public delegate void Action<T1, T2>(T1 arg1, T2 arg2);

    public delegate void Action<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);

    /// <summary>
    /// Utility methods for general stuff.
    /// </summary>
#if CF_1_0
    internal class CommonUtils
#else
    internal static class CommonUtils
#endif
    {
        #region Fields and Consts

        /// <summary>
        /// Table to convert numbers into roman digits
        /// </summary>
        private static readonly string[,] _romanDigitsTable = new[,]
            {
                {"", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX"},
                {"", "X", "XX", "XXX", "XL", "L", "LX", "LXX", "LXXX", "XC"},
                {"", "C", "CC", "CCC", "CD", "D", "DC", "DCC", "DCCC", "CM"},
                {
                    "", "M", "MM", "MMM", "M(V)", "(V)", "(V)M",
                    "(V)MM", "(V)MMM", "M(X)"
                }
            };

        private static readonly string[,] _hebrewDigitsTable = new[,]
            {
                {"א", "ב", "ג", "ד", "ה", "ו", "ז", "ח", "ט"},
                {"י", "כ", "ל", "מ", "נ", "ס", "ע", "פ", "צ"},
                {"ק", "ר", "ש", "ת", "תק", "תר", "תש", "תת", "תתק",}
            };

        private static readonly string[,] _georgianDigitsTable = new[,]
            {
                {"ა", "ბ", "გ", "დ", "ე", "ვ", "ზ", "ჱ", "თ"},
                {"ი", "პ", "ლ", "მ", "ნ", "ჲ", "ო", "პ", "ჟ"},
                {"რ", "ს", "ტ", "ჳ", "ფ", "ქ", "ღ", "ყ", "შ"}
            };

        private static readonly string[,] _armenianDigitsTable = new[,]
            {
                {"Ա", "Բ", "Գ", "Դ", "Ե", "Զ", "Է", "Ը", "Թ"},
                {"Ժ", "Ի", "Լ", "Խ", "Ծ", "Կ", "Հ", "Ձ", "Ղ"},
                {"Ճ", "Մ", "Յ", "Ն", "Շ", "Ո", "Չ", "Պ", "Ջ"}
            };

        private static readonly string[] _hiraganaDigitsTable = new[]
            {
                "あ", "ぃ", "ぅ", "ぇ", "ぉ", "か", "き", "く", "け", "こ", "さ", "し", "す", "せ", "そ", "た", "ち", "つ", "て", "と", "な", "に", "ぬ", "ね", "の", "は", "ひ", "ふ", "へ", "ほ", "ま", "み", "む", "め", "も", "ゃ", "ゅ", "ょ", "ら", "り", "る", "れ", "ろ", "ゎ", "ゐ", "ゑ", "を", "ん"
            };

        private static readonly string[] _satakanaDigitsTable = new[]
            {
                "ア", "イ", "ウ", "エ", "オ", "カ", "キ", "ク", "ケ", "コ", "サ", "シ", "ス", "セ", "ソ", "タ", "チ", "ツ", "テ", "ト", "ナ", "ニ", "ヌ", "ネ", "ノ", "ハ", "ヒ", "フ", "ヘ", "ホ", "マ", "ミ", "ム", "メ", "モ", "ヤ", "ユ", "ヨ", "ラ", "リ", "ル", "レ", "ロ", "ワ", "ヰ", "ヱ", "ヲ", "ン"
            };

        #endregion

        public static bool StrContains(String str, String mask)
        {
#if !CF_1_0 && !CF_2_0
            return str.Contains(mask);
#else
            return str.IndexOf(mask) >= 0;
#endif
        }

        public static char CharToLowerInvariant(char c)
        {
#if !CF_1_0 && !CF_2_0
            return Char.ToLowerInvariant(c);
#else
            return Char.ToLower(c, CultureInfo.InvariantCulture);
#endif
        }

        public static void StrAppendLine(StringBuilder sb, String str)
        {
            if (str == null)
            {
#if !CF_1_0 && !CF_2_0
                sb.AppendLine();
#else
                sb.Append("\n");
#endif
            }
            else
            {
#if !CF_1_0 && !CF_2_0
                sb.AppendLine(str);
#else
                sb.Append(str + "\n");
#endif
            }
        }

        public static void StrAppendLine(StringBuilder sb)
        {
            StrAppendLine(sb, null);
        }

        public static void StrAppendFormat(StringBuilder sb, String format, params object[] args)
        {
#if !CF_1_0 && !CF_2_0
            sb.AppendFormat(format, args);
#else
            sb.Append(String.Format(format, args));
#endif
        }

        /// <summary>
        /// Check if the given char is of Asian range.
        /// </summary>
        /// <param name="ch">the character to check</param>
        /// <returns>true - Asian char, false - otherwise</returns>
        public static bool IsAsianCharecter(char ch)
        {
            return ch >= 0x4e00 && ch <= 0xFA2D;
        }

        public static bool IsDigit(char ch)
        {
            return IsDigit(ch, false);
        }
        /// <summary>
        /// Check if the given char is a digit character (0-9) and (0-9, a-f for HEX)
        /// </summary>
        /// <param name="ch">the character to check</param>
        /// <param name="hex">optional: is hex digit check</param>
        /// <returns>true - is digit, false - not a digit</returns>
        public static bool IsDigit(char ch, bool hex)
        {
            return (ch >= '0' && ch <= '9') || (hex && ((ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F')));
        }

        /// <summary>
        /// Convert the given char to digit.
        /// </summary>
        /// <param name="ch">the character to check</param>
        /// <param name="hex">optional: is hex digit check</param>
        /// <returns>true - is digit, false - not a digit</returns>
        public static int ToDigit(char ch, bool hex)
        {
            if (ch >= '0' && ch <= '9')
                return ch - '0';
            else if (hex)
            {
                if (ch >= 'a' && ch <= 'f')
                    return ch - 'a' + 10;
                else if (ch >= 'A' && ch <= 'F')
                    return ch - 'A' + 10;
            }

            return 0;
        }

        public static int ToDigit(char ch)
        {
            return ToDigit(ch, false);
        }

        /// <summary>
        /// Get size that is max of <paramref name="size"/> and <paramref name="other"/> for width and height separately.
        /// </summary>
        public static SizeF Max(SizeF size, SizeF other)
        {
            return new SizeF(Math.Max(size.Width, other.Width), Math.Max(size.Height, other.Height));
        }

        /// <summary>
        /// Get Uri object for the given path if it is valid uri path.
        /// </summary>
        /// <param name="path">the path to get uri for</param>
        /// <returns>uri or null if not valid</returns>
        public static Uri TryGetUri(string path)
        {
            try
            {
                if (Uri.IsWellFormedUriString(path, UriKind.RelativeOrAbsolute))
                {
                    return new Uri(path);
                }
            }
            catch
            { }

            return null;
        }

        public static TValue GetFirstValueOrDefault<TKey, TValue>(IDictionary<TKey, TValue> dic)
        {
            return GetFirstValueOrDefault<TKey, TValue>(dic, default(TValue));
        }

        /// <summary>
        /// Get the first value in the given dictionary.
        /// </summary>
        /// <typeparam name="TKey">the type of dictionary key</typeparam>
        /// <typeparam name="TValue">the type of dictionary value</typeparam>
        /// <param name="dic">the dictionary</param>
        /// <param name="defaultValue">optional: the default value to return of no elements found in dictionary </param>
        /// <returns>first element or default value</returns>
        public static TValue GetFirstValueOrDefault<TKey, TValue>(IDictionary<TKey, TValue> dic, TValue defaultValue)
        {
            if (dic != null)
            {
                foreach (var value in dic)
                    return value.Value;
            }
            return defaultValue;
        }

        /// <summary>
        /// Get file info object for the given path if it is valid file path.
        /// </summary>
        /// <param name="path">the path to get file info for</param>
        /// <returns>file info or null if not valid</returns>
        public static FileInfo TryGetFileInfo(string path)
        {
            try
            {
                return new FileInfo(path);
            }
            catch
            { }

            return null;
        }

        public static bool TryParse(string att, out int val)
        {
            val = 0;
            try
            {
                val = int.Parse(att);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryParse(string att, out float val)
        {
            val = .0f;
            try
            {
                if (!Char.IsDigit(att, 0))
                {
                    return false;
                }
                val = float.Parse(att);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryParse(string att, NumberStyles style, IFormatProvider info, out float val)
        {
            val = .0f;
            try
            {
                if (!Char.IsDigit(att, 0))
                {
                    return false;
                }
                val = float.Parse(att, style, info);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

#if PC
        /// <summary>
        /// Get web client response content type.
        /// </summary>
        /// <param name="client">the web client to get the response content type from</param>
        /// <returns>response content type or null</returns>
        public static string GetResponseContentType(WebClient client)
        {
            foreach (string header in client.ResponseHeaders)
            {
                if (header.Equals("Content-Type", StringComparison.InvariantCultureIgnoreCase))
                    return client.ResponseHeaders[header];

            }
            return null;
        }
#endif

        /// <summary>
        /// Gets the representation of the online uri on the local disk.
        /// </summary>
        /// <param name="imageUri">The online image uri.</param>
        /// <returns>The path of the file on the disk.</returns>
        public static FileInfo GetLocalfileName(Uri imageUri)
        {
            StringBuilder fileNameBuilder = new StringBuilder();
            string absoluteUri = imageUri.AbsoluteUri;
            int lastSlash = absoluteUri.LastIndexOf('/');
            if (lastSlash == -1)
            {
                return null;
            }

            string uriUntilSlash = absoluteUri.Substring(0, lastSlash);
            fileNameBuilder.Append(uriUntilSlash.GetHashCode().ToString());
            fileNameBuilder.Append('_');

            string restOfUri = absoluteUri.Substring(lastSlash + 1);
            int indexOfParams = restOfUri.IndexOf('?');
            if (indexOfParams == -1)
            {
                string ext = ".cache";
                int indexOfDot = restOfUri.IndexOf('.');
                if (indexOfDot > -1)
                {
                    ext = restOfUri.Substring(indexOfDot);
                    restOfUri = restOfUri.Substring(0, indexOfDot);
                }

                fileNameBuilder.Append(restOfUri);
                fileNameBuilder.Append(ext);
            }
            else
            {
                int indexOfDot = restOfUri.IndexOf('.');
                if (indexOfDot == -1 || indexOfDot > indexOfParams)
                {
                    //The uri is not for a filename
                    fileNameBuilder.Append(restOfUri);
                    fileNameBuilder.Append(".cache");
                }
                else if (indexOfParams > indexOfDot)
                {
                    //Adds the filename without extension.
                    fileNameBuilder.Append(restOfUri, 0, indexOfDot);
                    //Adds the parameters
                    fileNameBuilder.Append(restOfUri, indexOfParams, restOfUri.Length - indexOfParams);
                    //Adds the filename extension.
                    fileNameBuilder.Append(restOfUri, indexOfDot, indexOfParams - indexOfDot);
                }
            }

            var validFileName = GetValidFileName(fileNameBuilder.ToString());
            if (validFileName.Length > 25)
            {
                validFileName = validFileName.Substring(0, 24) + validFileName.Substring(24).GetHashCode() + Path.GetExtension(validFileName);
            }

            return new FileInfo(Path.Combine(Path.GetTempPath(), validFileName));
        }

        /// <summary>
        /// Get substring seperated by whitespace starting from the given idex.
        /// </summary>
        /// <param name="str">the string to get substring in</param>
        /// <param name="idx">the index to start substring search from</param>
        /// <param name="length">return the length of the found string</param>
        /// <returns>the index of the substring, -1 if no valid sub-string found</returns>
        public static int GetNextSubString(string str, int idx, out int length)
        {
            while (idx < str.Length && Char.IsWhiteSpace(str[idx]))
                idx++;
            if (idx < str.Length)
            {
                var endIdx = idx + 1;
                while (endIdx < str.Length && !Char.IsWhiteSpace(str[endIdx]))
                    endIdx++;
                length = endIdx - idx;
                return idx;
            }
            length = 0;
            return -1;
        }

        /// <summary>
        /// Compare that the substring of <paramref name="str"/> is equal to <paramref name="str"/>
        /// Assume given substring is not empty and all indexes are valid!<br/>
        /// </summary>
        /// <returns>true - equals, false - not equals</returns>
        public static bool SubStringEquals(string str, int idx, int length, string str2)
        {
            if (length == str2.Length && idx + length <= str.Length)
            {
                for (int i = 0; i < length; i++)
                {
                    if (CharToLowerInvariant(str[idx + i]) != CharToLowerInvariant(str2[i]))
                        return false;
                }
                return true;
            }
            return false;
        }

        private static char[] GetInvalidFileNameChars()
        {
            return new char[41] { '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07',
					'\x08', '\x09', '\x0A', '\x0B', '\x0C', '\x0D', '\x0E', '\x0F', '\x10', '\x11', '\x12', 
					'\x13', '\x14', '\x15', '\x16', '\x17', '\x18', '\x19', '\x1A', '\x1B', '\x1C', '\x1D', 
					'\x1E', '\x1F', '\x22', '\x3C', '\x3E', '\x7C', ':', '*', '?', '\\', '/' };
        }

        /// <summary>
        /// Replaces invalid filename chars to '_'
        /// </summary>
        /// <param name="source">The possibly-not-valid filename</param>
        /// <returns>A valid filename.</returns>
        private static string GetValidFileName(string source)
        {
            string retVal = source;
            char[] invalidFileNameChars = GetInvalidFileNameChars();
            foreach (var invalidFileNameChar in invalidFileNameChars)
            {
                retVal = retVal.Replace(invalidFileNameChar, '_');
            }
            return retVal;
        }

        public static string ConvertToAlphaNumber(int number)
        {
            return ConvertToAlphaNumber(number, CssConstants.UpperAlpha);
        }

        /// <summary>
        /// Convert number to alpha numeric system by the requested style (UpperAlpha, LowerRoman, Hebrew, etc.).
        /// </summary>
        /// <param name="number">the number to convert</param>
        /// <param name="style">the css style to convert by</param>
        /// <returns>converted string</returns>
        public static string ConvertToAlphaNumber(int number, string style)
        {
            if (number == 0)
                return string.Empty;

            if (style.Equals(CssConstants.LowerGreek, StringComparison.InvariantCultureIgnoreCase))
            {
                return ConvertToGreekNumber(number);
            }
            else if (style.Equals(CssConstants.LowerRoman, StringComparison.InvariantCultureIgnoreCase))
            {
                return ConvertToRomanNumbers(number, true);
            }
            else if (style.Equals(CssConstants.UpperRoman, StringComparison.InvariantCultureIgnoreCase))
            {
                return ConvertToRomanNumbers(number, false);
            }
            else if (style.Equals(CssConstants.Armenian, StringComparison.InvariantCultureIgnoreCase))
            {
                return ConvertToSpecificNumbers(number, _armenianDigitsTable);
            }
            else if (style.Equals(CssConstants.Georgian, StringComparison.InvariantCultureIgnoreCase))
            {
                return ConvertToSpecificNumbers(number, _georgianDigitsTable);
            }
            else if (style.Equals(CssConstants.Hebrew, StringComparison.InvariantCultureIgnoreCase))
            {
                return ConvertToSpecificNumbers(number, _hebrewDigitsTable);
            }
            else if (style.Equals(CssConstants.Hiragana, StringComparison.InvariantCultureIgnoreCase) || style.Equals(CssConstants.HiraganaIroha, StringComparison.InvariantCultureIgnoreCase))
            {
                return ConvertToSpecificNumbers2(number, _hiraganaDigitsTable);
            }
            else if (style.Equals(CssConstants.Katakana, StringComparison.InvariantCultureIgnoreCase) || style.Equals(CssConstants.KatakanaIroha, StringComparison.InvariantCultureIgnoreCase))
            {
                return ConvertToSpecificNumbers2(number, _satakanaDigitsTable);
            }
            else
            {
                var lowercase = style.Equals(CssConstants.LowerAlpha, StringComparison.InvariantCultureIgnoreCase) || style.Equals(CssConstants.LowerLatin, StringComparison.InvariantCultureIgnoreCase);
                return ConvertToEnglishNumber(number, lowercase);
            }
        }

        /// <summary>
        /// Convert the given integer into alphabetic numeric format (D, AU, etc.)
        /// </summary>
        /// <param name="number">the number to convert</param>
        /// <param name="lowercase">is to use lowercase</param>
        /// <returns>the roman number string</returns>
        private static string ConvertToEnglishNumber(int number, bool lowercase)
        {
            var sb = string.Empty;
            int alphStart = lowercase ? 97 : 65;
            while (number > 0)
            {
                var n = number % 26 - 1;
                if (n >= 0)
                {
                    sb = (Char)(alphStart + n) + sb;
                    number = number / 26;
                }
                else
                {
                    sb = (Char)(alphStart + 25) + sb;
                    number = (number - 1) / 26;
                }
            }

            return sb;
        }

        /// <summary>
        /// Convert the given integer into alphabetic numeric format (alpha, AU, etc.)
        /// </summary>
        /// <param name="number">the number to convert</param>
        /// <returns>the roman number string</returns>
        private static string ConvertToGreekNumber(int number)
        {
            var sb = string.Empty;
            while (number > 0)
            {
                var n = number % 24 - 1;
                if (n > 16)
                    n++;
                if (n >= 0)
                {
                    sb = (Char)(945 + n) + sb;
                    number = number / 24;
                }
                else
                {
                    sb = (Char)(945 + 24) + sb;
                    number = (number - 1) / 25;
                }
            }

            return sb;
        }

        /// <summary>
        /// Convert the given integer into roman numeric format (II, VI, IX, etc.)
        /// </summary>
        /// <param name="number">the number to convert</param>
        /// <param name="lowercase">if to use lowercase letters for roman digits</param>
        /// <returns>the roman number string</returns>
        private static string ConvertToRomanNumbers(int number, bool lowercase)
        {
            var sb = string.Empty;
            for (int i = 1000, j = 3; i > 0; i /= 10, j--)
            {
                int digit = number / i;
                sb += string.Format(_romanDigitsTable[j, digit]);
                number -= digit * i;
            }
            return lowercase ? sb.ToLower() : sb;
        }

        /// <summary>
        /// Convert the given integer into given alphabet numeric system.
        /// </summary>
        /// <param name="number">the number to convert</param>
        /// <param name="alphabet">the alphabet system to use</param>
        /// <returns>the number string</returns>
        private static string ConvertToSpecificNumbers(int number, string[,] alphabet)
        {
            int level = 0;
            var sb = string.Empty;
            while (number > 0 && level < alphabet.GetLength(0))
            {
                var n = number % 10;
                if (n > 0)
                    sb = alphabet[level, number % 10 - 1].ToString(CultureInfo.InvariantCulture) + sb;
                number /= 10;
                level++;
            }
            return sb;
        }

        /// <summary>
        /// Convert the given integer into given alphabet numeric system.
        /// </summary>
        /// <param name="number">the number to convert</param>
        /// <param name="alphabet">the alphabet system to use</param>
        /// <returns>the number string</returns>
        private static string ConvertToSpecificNumbers2(int number, string[] alphabet)
        {
            for (int i = 20; i > 0; i--)
            {
                if (number > 49 * i - i + 1)
                    number++;
            }

            var sb = string.Empty;
            while (number > 0)
            {


                sb = alphabet[Math.Max(0, number % 49 - 1)].ToString(CultureInfo.InvariantCulture) + sb;
                number /= 49;
            }
            return sb;
        }
    }
}
