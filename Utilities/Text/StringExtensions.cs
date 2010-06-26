using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.Utilities.Text
{
	public static class StringExtensions
	{
		#region Camel case helpers
		/// <summary>
		/// Change a non-flag enumeration value using Camel Case to a space-separated list.
		/// </summary>
		/// <example>MyEnum.TheColorGreen becomes The Color Green</example>
		public static string CamelCaseToSpaces(this System.Enum enumValue)
		{
			return enumValue.ToString().CamelCaseToSpaces();
		}

		/// <summary>
		/// Parses a camel cased or pascal cased string and returns an array
		/// of the words within the string.
		/// </summary>
		/// <example>
		/// The string "PascalCasing" will return an array with two
		/// elements, "Pascal" and "Casing".
		/// </example>
		/// <param name="source"></param>
		/// <returns></returns>
		public static string[] SplitByCamelCase(this string source)
		{
			if (source == null)
				return new string[] { }; //Return empty array.

			if (source.Length == 0)
				return new string[] { "" };

			List<string> words = new List<string>();
			int wordStartIndex = 0;

			char[] letters = source.ToCharArray();
			bool lastWasUpper = char.IsUpper(letters[0]);
			// Skip the first letter. we don't care what case it is.
			for (int i = 1; i < letters.Length; i++)
			{
				if ((!lastWasUpper && char.IsUpper(letters[i])) ||
					(lastWasUpper && char.IsUpper(letters[i]) && i + 1 < letters.Length && char.IsLower(letters[i + 1])))
				{
					lastWasUpper = true;
					//Grab everything before the current index.
					words.Add(new String(letters, wordStartIndex, i - wordStartIndex));
					wordStartIndex = i;
				}
				else if (!char.IsUpper(letters[i]))
					lastWasUpper = false;
			}
			//We need to have the last word.
			words.Add(new String(letters, wordStartIndex, letters.Length - wordStartIndex));

			//Copy to a string array.
			return words.ToArray();
		}

		/// <summary>
		/// <seealso cref="SplitByCamelCase"/>.  Joins the resulting array with spaces in between.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public static string CamelCaseToSpaces(this string source)
		{
			return String.Join(" ", SplitByCamelCase(source));
		}
		#endregion

		#region XML/SQL Server Escaping
		static string EncodeControlCharacters(string input)
		{
			int i = 0, len = input.Length;
			for (; i < len; i++)
			{
				char c = input[i];
				if (c < 0x20 && (c != 0x9 && c != 0xA && c != 0xD))
				{
					break;
				}
			}
			if (i == len)
			{
				return input;
			}
			StringBuilder sb = new StringBuilder();
			if (i != 0) { sb.Append(input, 0, i); }
			for (; i < len; i++)
			{
				char c = input[i];
				if (c < 0x20 && (c != 0x9 && c != 0xA && c != 0xD))
				{
					sb.Append((char) (0xE000+c));
				}
				else
				{
					sb.Append(c);
				}
			}
			return sb.ToString();
		}

		static string DecodeControlCharacters(string input)
		{
			int i = 0, len = input.Length;
			for (; i < len; i++)
			{
				char c = input[i];
				if (c >= 0xE000 && c < 0xE020)
				{
					break;
				}
			}
			if (i == len)
			{
				return input;
			}
			StringBuilder sb = new StringBuilder();
			if (i != 0) { sb.Append(input, 0, i); }
			for (; i < len; i++)
			{
				char c = input[i];
				if (c >= 0xE000 && c < 0xE020)
				{
					sb.Append((char) (c - 0xE000));
				}
				else
				{
					sb.Append(c);
				}
			}
			return sb.ToString();
		}

		#endregion

	}
}
