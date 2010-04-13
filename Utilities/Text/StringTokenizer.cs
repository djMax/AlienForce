using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.Utilities.Text
{
	/// <summary>
	/// The StringTokenizer includes functions for parsing a delimeted string
	/// including escaping.
	/// </summary>
	public sealed class StringTokenizer
	{
		#region Escape processor
		/// <summary>
		/// Processes the escape codes in a string (slash followed by a character).
		/// Right now this only supports single character escapes
		/// </summary>
		/// <param name="escaped">The escaped.</param>
		/// <returns></returns>
		public static string ProcessEscapeCodes(string escaped)
		{
			if (escaped.IndexOf('\\') == -1)
			{
				// Nothing to do
				return escaped;
			}
			bool inEscape = false;
			StringBuilder currentTag = new StringBuilder();
			for (int i = 0; i < escaped.Length; i++)
			{
				if (inEscape)
				{
					inEscape = false;
					currentTag.Append(getEscapeChar(escaped, ref i));
				}
				else
				{
					char c = escaped[i];
					if (c == '\\')
					{
						inEscape = true;
						continue;
					}
					currentTag.Append(c);
				}
			}
			return currentTag.ToString();
		}

		private static char getEscapeChar(string source, ref int counter)
		{
			char c = source[counter];
			switch (c)
			{
				case 'n':
					return '\n';
				case 'r':
					return '\r';
				case 't':
					return '\t';
				case '"':
					return '"';
				default:
					return c;
			}
		}

		/// <summary>
		/// Insert necessary escape codes into the string.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <returns></returns>
		public static string EscapeString(string source)
		{
			StringBuilder sb = new StringBuilder((int)(source.Length + (source.Length * .2)));
			EscapeString(source, sb);
			return sb.ToString();
		}

		/// <summary>
		/// Insert necessary escape codes into the string.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="destination">The destination.</param>
		public static void EscapeString(string source, StringBuilder destination)
		{
			for (int i = 0; i < source.Length; i++)
			{
				switch (source[i])
				{
					case '\\':
						destination.Append("\\\\"); break;
					case '\n':
						destination.Append("\\n"); break;
					case '\r':
						destination.Append("\\r"); break;
					case '\t':
						destination.Append("\\t"); break;
					case '\"':
						destination.Append("\\\""); break;
					default:
						destination.Append(source[i]); break;
				}
			}
		}

		/// <summary>
		/// Read a string from the source (starting at index) and return the unescaped string
		/// as well as a count of how many characters were consumed.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="index">The index.</param>
		/// <param name="separator">The separator.</param>
		/// <param name="quoteChar">The quote char.</param>
		/// <param name="escapeChar">The escape char.</param>
		/// <param name="output">The output.</param>
		/// <returns></returns>
		public static int UnescapeString(string source, int index, char separator, char quoteChar, char escapeChar, StringBuilder output)
		{
			bool inEscape = false;
			bool inQuote = false, startedWithQuote = false, addIfNotDone = false;
			int i = index, sbStart = output.Length, lastNonWhitespace = -1, len = source.Length;

			if (source[i] == quoteChar)
			{
				i++;
				inQuote = true;
				startedWithQuote = true;
			}

			for (; i < len; i++)
			{
				if (inEscape)
				{
					output.Append(getEscapeChar(source, ref i));
					inEscape = false;
				}
				else
				{
					char c = source[i];
					if (c == escapeChar)
					{
						inEscape = true;
						if (addIfNotDone)
						{
							addIfNotDone = false;
							startedWithQuote = false;
							output.Insert(sbStart, quoteChar);
						}
						continue;
					}
					else if (c == quoteChar)
					{
						inQuote = !inQuote;
						if (!inQuote && startedWithQuote)
						{
							// if we see another char, make sure to add a quote to the beginning
							addIfNotDone = true;
							output.Append(c);
							lastNonWhitespace = output.Length;
							continue;
						}
					}
					else if (!inQuote && c == separator)
					{
						i++;
						break;
					}
					if (!Char.IsWhiteSpace(c))
					{
						if (addIfNotDone)
						{
							addIfNotDone = false;
							startedWithQuote = false;
							output.Insert(sbStart, quoteChar);
						}
						lastNonWhitespace = output.Length + 1;
					}
					output.Append(c);
				}
			}
			if (startedWithQuote)
			{
				if (output[output.Length - 1] == '"')
				{
					output.Length--;
				}
			}
			if (lastNonWhitespace >= 0 && lastNonWhitespace < output.Length)
			{
				output.Length = lastNonWhitespace;
			}
			return i;
		}
		#endregion

		#region Position discovery functions
		/// <summary>
		/// Returns the index into the "collection" of values corresponding to a position
		/// in the original string.
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="stringPosition">The string position.</param>
		/// <returns></returns>
		public static int StringPositionToCollectionPosition(string list, int stringPosition)
		{
			return StringPositionToCollectionPosition(list, ',', '\\', stringPosition);
		}

		/// <summary>
		/// Returns the index into the "collection" of values corresponding to a position
		/// in the original string.
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter used between values</param>
		/// <param name="stringPosition">The string position.</param>
		/// <returns></returns>
		public static int StringPositionToCollectionPosition(string list, char delimiter, int stringPosition)
		{
			return StringPositionToCollectionPosition(list, delimiter, '\\', stringPosition);
		}

		/// <summary>
		/// Returns the index into the "collection" of values corresponding to a position
		/// in the original string.
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter.</param>
		/// <param name="escape">The escape character.</param>
		/// <param name="stringPosition">The string position.</param>
		/// <returns></returns>
		public static int StringPositionToCollectionPosition(string list, char delimiter, char escape, int stringPosition)
		{
			if (stringPosition > list.Length || stringPosition < 0)
			{
				throw new ArgumentOutOfRangeException("stringPosition");
			}
			bool inQuote = false;
			bool inEscape = false;
			int productionCount = 0, tagIndex = 0;
			StringBuilder currentTag = new StringBuilder();
			for (int i = 0; i < stringPosition && i < list.Length; i++)
			{
				char c = list[i];
				if (inEscape)
				{
					inEscape = false;
					if (Char.IsWhiteSpace(c) && tagIndex == 0)
					{
						continue;
					}
					tagIndex++;
				}
				else
				{
					if (c == escape)
					{
						inEscape = true;
						continue;
					}
					else if (c == delimiter && !inQuote)
					{
						productionCount++;
						tagIndex = 0;
						continue;
					}
					else if (c == '"')
					{
						inQuote = !inQuote;
						if (tagIndex == 0)
						{
							continue;
						}
					}
					if (Char.IsWhiteSpace(c) && tagIndex == 0)
					{
						continue;
					}
					tagIndex++;
				}
			}
			return productionCount;
		}

		#endregion

		#region Generic type collection parsers
		/// <summary>
		/// A TypeConverter is a function that returns type T from a string.  It might use TryParse, or
		/// other custom logic.  <see cref="ParseCollection&lt;T&gt;(string,char,char,TypeConverter&lt;T&gt;)"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		public delegate T TypeConverter<T>(string value);

		/// <summary>
		/// Parse a comma delimeted collection from a string, including escape codes.
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="converter">The converter which returns your type from a string.</param>
		/// <returns></returns>
		public static List<T> ParseCollection<T>(string list, TypeConverter<T> converter)
		{
			return ParseCollection<T>(list, ',', '\\', converter);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter.</param>
		/// <param name="converter">The converter which returns your type from a string.</param>
		/// <returns></returns>
		public static List<T> ParseCollection<T>(string list, char delimiter, TypeConverter<T> converter)
		{
			return ParseCollection<T>(list, delimiter, '\\', converter);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes, into a list of a given type
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter character.</param>
		/// <param name="escape">The escape character.</param>
		/// <param name="converter">The converter which returns your type from a string.</param>
		/// <returns></returns>
		public static List<T> ParseCollection<T>(string list, char delimiter, char escape, TypeConverter<T> converter)
		{
			return ParseCollection<T>(list, delimiter, escape, -1, converter);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes, into a list of a given type
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter character.</param>
		/// <param name="escape">The escape character.</param>
		/// <param name="maxElements">The maximum number of elements to return, or &lt;= 0 for as many as we find.</param>
		/// <param name="converter">The converter which returns your type from a string.</param>
		/// <returns></returns>
		public static List<T> ParseCollection<T>(string list, char delimiter, char escape, int maxElements, TypeConverter<T> converter)
		{
			return ParseCollection<T>(list, delimiter, escape, maxElements, converter, null);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes, into a unique list of a given type
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter character.</param>
		/// <param name="escape">The escape character.</param>
		/// <param name="maxElements">The maximum number of elements to return, or &lt;= 0 for as many as we find.</param>
		/// <param name="converter">The converter which returns your type from a string.</param>
		/// <param name="equalityComparer">The equality comparer.</param>
		/// <returns></returns>
		public static List<T> ParseCollection<T>(string list, char delimiter, char escape, int maxElements, TypeConverter<T> converter, IEqualityComparer<T> equalityComparer)
		{
			int[] delimiterIndices;
			return ParseCollection<T>(list, delimiter, escape, maxElements, converter, equalityComparer, false, out delimiterIndices);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes, into a unique list of a given type
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter character.</param>
		/// <param name="escape">The escape character.</param>
		/// <param name="maxElements">The maximum number of elements to return, or &lt;= 0 for as many as we find.</param>
		/// <param name="converter">The converter which returns your type from a string.</param>
		/// <param name="equalityComparer">The equality comparer.</param>
		/// <param name="delimiterIndices">The indices at which the delimiters were located. As the beginning of the string is always the start of the first token, the first value in this array is always -1.</param>
		/// <returns></returns>
		public static List<T> ParseCollection<T>(string list, char delimiter, char escape, int maxElements, TypeConverter<T> converter, IEqualityComparer<T> equalityComparer, out int[] delimiterIndices)
		{
			return ParseCollection<T>(list, delimiter, escape, maxElements, converter, equalityComparer, true, out delimiterIndices);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes, into a unique list of a given type
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter character.</param>
		/// <param name="escape">The escape character.</param>
		/// <param name="maxElements">The maximum number of elements to return, or &lt;= 0 for as many as we find.</param>
		/// <param name="converter">The converter which returns your type from a string.</param>
		/// <param name="equalityComparer">The equality comparer.</param>
		/// <param name="trackDelimiterIndices">Indicates that delimiter indices should be tracked.</param>
		/// <param name="delimiterIndices">The indices at which the delimiters were located. As the beginning of the string is always the start of the first token, the first value in this array is always -1.</param>
		/// <returns></returns>
		private static List<T> ParseCollection<T>(string list, char delimiter, char escape, int maxElements, TypeConverter<T> converter, IEqualityComparer<T> equalityComparer, bool trackDelimiterIndices, out int[] delimiterIndices)
		{
			Dictionary<T, object> hash = null;
			bool checkUniqueness = false;
			if (equalityComparer != null)
			{
				hash = new Dictionary<T, object>(equalityComparer);
				checkUniqueness = true;
			}
			List<int> delimiterIndicesList = null;
			delimiterIndices = null;
			if (trackDelimiterIndices)
			{
				delimiterIndicesList = new List<int>();
			}
			List<T> tags = new List<T>();
			bool inQuote = false;
			bool addIfNotDone = false;
			bool inEscape = false;
			bool currentTagStartedWithQuote = false;
			int tagIndex = 0;
			int lastNonWhitespaceIndex = -1;
			int lastDelimiterIndex = -1; // before first character
			StringBuilder currentTag = new StringBuilder();
			int l = list.Length;
			for (int i = 0; i < l; i++)
			{
				char c = list[i];
				if (inEscape)
				{
					inEscape = false;
					if (Char.IsWhiteSpace(c))
					{
						if (currentTag.Length == 0)
						{
							continue;
						}
					}
					else
					{
						lastNonWhitespaceIndex = tagIndex;
					}
					currentTag.Append(getEscapeChar(list, ref i));
					tagIndex++;
				}
				else
				{
					if (c == escape)
					{
						inEscape = true;
						if (addIfNotDone)
						{
							currentTagStartedWithQuote = false;
							addIfNotDone = false;
							currentTag.Insert(0, '"');
							tagIndex++;
						}
						continue;
					}
					else if (c == delimiter && !inQuote && (maxElements <= 0 || tags.Count < maxElements - 1))
					{
						#region Found ending delimiter
						T tag;
						if (lastNonWhitespaceIndex >= 0 && currentTagStartedWithQuote && currentTag[lastNonWhitespaceIndex] == '"')
						{
							tag = converter(currentTag.ToString(0, lastNonWhitespaceIndex));
						}
						else
						{
							tag = converter(currentTag.ToString(0, lastNonWhitespaceIndex + 1));
						}
						if (checkUniqueness)
						{
							if (!hash.ContainsKey(tag))
							{
								hash.Add(tag, null);
								if (trackDelimiterIndices)
								{
									delimiterIndicesList.Add(lastDelimiterIndex);
									lastDelimiterIndex = i;
								}
							}
						}
						else
						{
							tags.Add(tag);
							if (trackDelimiterIndices)
							{
								delimiterIndicesList.Add(lastDelimiterIndex);
								lastDelimiterIndex = i;
							}
						}
						currentTag = new StringBuilder();
						addIfNotDone = false;
						currentTagStartedWithQuote = false;
						tagIndex = 0;
						lastNonWhitespaceIndex = -1;
						continue;
						#endregion
					}
					else if (c == '"')
					{
						if (inQuote && currentTagStartedWithQuote)
						{
							addIfNotDone = true;
							lastNonWhitespaceIndex = tagIndex;
							currentTag.Append('"');
							tagIndex++;
							inQuote = false;
							continue;
						}
						inQuote = !inQuote;
						if (currentTag.Length == 0)
						{
							currentTagStartedWithQuote = true;
							continue;
						}
					}
					if (Char.IsWhiteSpace(c))
					{
						if (currentTag.Length == 0)
						{
							continue;
						}
					}
					else
					{
						if (addIfNotDone)
						{
							currentTagStartedWithQuote = false;
							addIfNotDone = false;
							currentTag.Insert(0, '"');
							tagIndex++;
						}
						lastNonWhitespaceIndex = tagIndex;
					}
					currentTag.Append(c);
					tagIndex++;
				}
			}
			if (currentTag.Length > 0)
			{
				T tag;
				if (lastNonWhitespaceIndex > 0 && currentTagStartedWithQuote && currentTag[lastNonWhitespaceIndex] == '"')
				{
					tag = converter(currentTag.ToString(0, lastNonWhitespaceIndex));
				}
				else
				{
					tag = converter(currentTag.ToString(0, lastNonWhitespaceIndex + 1));
				}
				if (checkUniqueness)
				{
					if (!hash.ContainsKey(tag))
					{
						hash.Add(tag, null);
						if (trackDelimiterIndices)
						{
							delimiterIndicesList.Add(lastDelimiterIndex);
						}
					}
				}
				else
				{
					tags.Add(tag);
					if (trackDelimiterIndices)
					{
						delimiterIndicesList.Add(lastDelimiterIndex);
					}
				}
			}
			else if (currentTagStartedWithQuote) // empty tag
			{
				T tag = converter(String.Empty);
				if (checkUniqueness)
				{
					if (!hash.ContainsKey(tag))
					{
						hash.Add(tag, null);
						if (trackDelimiterIndices)
						{
							delimiterIndicesList.Add(lastDelimiterIndex);
						}
					}
				}
				else
				{
					tags.Add(tag);
					if (trackDelimiterIndices)
					{
						delimiterIndicesList.Add(lastDelimiterIndex);
					}
				}
			}
			if (checkUniqueness)
			{
				tags.AddRange(hash.Keys);
			}

			if (trackDelimiterIndices)
			{
				delimiterIndices = delimiterIndicesList.ToArray();
			}

			return tags;
		}
		#endregion

		#region String Collection Parsers
		/// <summary>
		/// This seemingly silly method is for the "standard" tokenizer
		/// which returns strings.
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		private static string Self(string x)
		{
			return x;
		}

		/// <summary>
		/// Parse a comma delimeted collection from a string, including escape codes.
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <returns></returns>
		public static List<string> ParseCollection(string list)
		{
			return ParseCollection<string>(list, ',', '\\', Self);
		}

		/// <summary>
		/// Parse a comma delimeted collection from a string, including escape codes.
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiterIndices">The indices at which the delimiters were located. As the beginning of the string is always the start of the first token, the first value in this array is always -1.</param>
		/// <returns></returns>
		public static List<string> ParseCollection(string list, out int[] delimiterIndices)
		{
			return ParseCollection<string>(list, ',', '\\', -1, Self, null, out delimiterIndices);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter.</param>
		/// <returns></returns>
		public static List<string> ParseCollection(string list, char delimiter)
		{
			return ParseCollection<string>(list, delimiter, '\\', Self);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter.</param>
		/// <param name="delimiterIndices">The indices at which the delimiters were located. As the beginning of the string is always the start of the first token, the first value in this array is always -1.</param>
		/// <returns></returns>
		public static List<string> ParseCollection(string list, char delimiter, out int[] delimiterIndices)
		{
			return ParseCollection<string>(list, delimiter, '\\', -1, Self, null, out delimiterIndices);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter.</param>
		/// <param name="escape">The escape character.</param>
		/// <returns></returns>
		public static List<string> ParseCollection(string list, char delimiter, char escape)
		{
			return ParseCollection<string>(list, delimiter, escape, Self);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter.</param>
		/// <param name="escape">The escape character.</param>
		/// <param name="maxElements">The maximum number of elements to return, or &lt;= 0 for as many as we find.</param>
		/// <returns></returns>
		public static List<string> ParseCollection(string list, char delimiter, char escape, int maxElements)
		{
			return ParseCollection<string>(list, delimiter, escape, maxElements, Self);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter.</param>
		/// <param name="escape">The escape character.</param>
		/// <param name="maxElements">The maximum number of elements to return, or &lt;= 0 for as many as we find.</param>
		/// <param name="delimiterIndices">The indices at which the delimiters were located. As the beginning of the string is always the start of the first token, the first value in this array is always -1.</param>
		/// <returns></returns>
		public static List<string> ParseCollection(string list, char delimiter, char escape, int maxElements, out int[] delimiterIndices)
		{
			return ParseCollection<string>(list, delimiter, escape, maxElements, Self, null, out delimiterIndices);
		}

		/// <summary>
		/// Parse a comma delimeted collection from a string, including escape codes, and removing case-insensitive duplicates.
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <returns></returns>
		public static List<string> ParseUniqueCollection(string list)
		{
			return ParseCollection<string>(list, ',', '\\', -1, Self, StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Parse a comma delimeted collection from a string, including escape codes, and removing case-insensitive duplicates.
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiterIndices">The indices at which the delimiters were located. As the beginning of the string is always the start of the first token, the first value in this array is always -1.</param>
		/// <returns></returns>
		public static List<string> ParseUniqueCollection(string list, out int[] delimiterIndices)
		{
			return ParseCollection<string>(list, ',', '\\', -1, Self, StringComparer.OrdinalIgnoreCase, out delimiterIndices);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter.</param>
		/// <returns></returns>
		public static List<string> ParseUniqueCollection(string list, char delimiter)
		{
			return ParseCollection<string>(list, delimiter, '\\', -1, Self, StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter.</param>
		/// <param name="delimiterIndices">The indices at which the delimiters were located. As the beginning of the string is always the start of the first token, the first value in this array is always -1.</param>
		/// <returns></returns>
		public static List<string> ParseUniqueCollection(string list, char delimiter, out int[] delimiterIndices)
		{
			return ParseCollection<string>(list, delimiter, '\\', -1, Self, StringComparer.OrdinalIgnoreCase, out delimiterIndices);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter.</param>
		/// <param name="escape">The escape character.</param>
		/// <returns></returns>
		public static List<string> ParseUniqueCollection(string list, char delimiter, char escape)
		{
			return ParseCollection<string>(list, delimiter, escape, -1, Self, StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter.</param>
		/// <param name="escape">The escape character.</param>
		/// <param name="maxElements">The maximum number of elements to return, or &lt;= 0 for as many as we find.</param>
		/// <returns></returns>
		public static List<string> ParseUniqueCollection(string list, char delimiter, char escape, int maxElements)
		{
			return ParseCollection<string>(list, delimiter, escape, maxElements, Self, StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Parse a delimeted list from a string, including escape codes
		/// </summary>
		/// <param name="list">The string representing a delimited, escaped collection of values.</param>
		/// <param name="delimiter">The delimiter.</param>
		/// <param name="escape">The escape character.</param>
		/// <param name="maxElements">The maximum number of elements to return, or &lt;= 0 for as many as we find.</param>
		/// <param name="delimiterIndices">The indices at which the delimiters were located. As the beginning of the string is always the start of the first token, the first value in this array is always -1.</param>
		/// <returns></returns>
		public static List<string> ParseUniqueCollection(string list, char delimiter, char escape, int maxElements, out int[] delimiterIndices)
		{
			return ParseCollection<string>(list, delimiter, escape, maxElements, Self, StringComparer.OrdinalIgnoreCase, out delimiterIndices);
		}
		#endregion

		/// <summary>
		/// Joins the specified elements.
		/// </summary>
		/// <param name="elements">The elements.</param>
		/// <returns></returns>
		public static string Join(IEnumerable<string> elements)
		{
			return Join(elements, ',');
		}

		/// <summary>
		/// Joins the specified elements.
		/// </summary>
		/// <param name="elements">The elements.</param>
		/// <param name="separator">The separator.</param>
		/// <returns></returns>
		public static string Join(IEnumerable<string> elements, char separator)
		{
			return Join(elements, separator, '\\');
		}

		/// <summary>
		/// Joins the specified elements.
		/// </summary>
		/// <param name="elements">The elements.</param>
		/// <param name="separator">The separator.</param>
		/// <param name="escape">The escape character.</param>
		/// <returns></returns>
		public static string Join(IEnumerable<string> elements, char separator, char escape)
		{
			StringBuilder output = new StringBuilder();
			string escapeSearch = escape.ToString();
			string escapeReplacement = escapeSearch + escapeSearch;
			foreach (string s in elements)
			{
				if (s.IndexOf(separator) >= 0)
				{
					output.Append('"');
					output.Append(s.Replace(escapeSearch, escapeReplacement).Replace("\"", escape + "\""));
					output.Append('"');
				}
				else
				{
					output.Append(s.Replace(escapeSearch, escapeReplacement));
				}
				output.Append(separator);
			}
			return output.Length > 0 ? output.ToString(0, output.Length - 1) : string.Empty;
		}

		/// <summary>
		/// Joins the specified elements.
		/// </summary>
		/// <param name="elements">The elements.</param>
		/// <param name="separator">The separator.</param>
		/// <param name="escape">The escape character.</param>
		/// <returns></returns>
		public static string Join(IEnumerable<string> elements, string separator, char escape)
		{
			StringBuilder output = new StringBuilder();
			string escapeSearch = escape.ToString();
			string escapeReplacement = escapeSearch + escapeSearch;
			foreach (string s in elements)
			{
				if (s.IndexOf(separator) >= 0)
				{
					output.Append('"');
					output.Append(s.Replace(escapeSearch, escapeReplacement).Replace("\"", escape + "\""));
					output.Append('"');
				}
				else
				{
					output.Append(s.Replace(escapeSearch, escapeReplacement));
				}
				output.Append(separator);
			}
			return output.Length > 0 ? output.ToString(0, output.Length - separator.Length) : string.Empty;
		}

		/// <summary>
		/// Joins the specified elements.
		/// </summary>
		/// <param name="elements">The elements.</param>
		/// <param name="separator">The separator.</param>
		/// <param name="escape">The escape character.</param>
		/// <returns></returns>
		public static string Join(IList<string> elements, char separator, char escape, int index, int length)
		{
			StringBuilder output = new StringBuilder();
			string escapeSearch = escape.ToString();
			string escapeReplacement = escapeSearch + escapeSearch;
			int limit = index + length;
			for (int i = index; i < limit; i++)
			{
				string s = elements[i];
				if (s.IndexOf(separator) >= 0)
				{
					output.Append('"');
					output.Append(s.Replace(escapeSearch, escapeReplacement).Replace("\"", escape + "\""));
					output.Append('"');
				}
				else
				{
					output.Append(s.Replace(escapeSearch, escapeReplacement));
				}
				output.Append(separator);
			}
			return output.Length > 0 ? output.ToString(0, output.Length - 1) : string.Empty;
		}

		/// <summary>
		/// Joins the specified elements.
		/// </summary>
		/// <param name="elements">The elements.</param>
		/// <param name="separator">The separator.</param>
		/// <param name="escape">The escape character.</param>
		/// <returns></returns>
		public static string Join<T>(IEnumerable<T> elements, char separator, char escape)
		{
			StringBuilder output = new StringBuilder();
			string escapeSearch = escape.ToString();
			string escapeReplacement = escapeSearch + escapeSearch;
			foreach (T v in elements)
			{
				string s = v != null ? v.ToString() : String.Empty;
				if (s.IndexOf(separator) >= 0)
				{
					output.Append('"');
					output.Append(s.Replace(escapeSearch, escapeReplacement).Replace("\"", escape + "\""));
					output.Append('"');
				}
				else
				{
					output.Append(s.Replace(escapeSearch, escapeReplacement));
				}
				output.Append(separator);
			}
			return output.Length > 0 ? output.ToString(0, output.Length - 1) : string.Empty;
		}

		/// <summary>
		/// Joins the specified elements.
		/// </summary>
		/// <param name="elements">The elements.</param>
		/// <param name="separator">The separator.</param>
		/// <param name="escape">The escape character.</param>
		/// <returns></returns>
		public static string Join(System.Collections.IEnumerable elements, char separator, char escape)
		{
			StringBuilder output = new StringBuilder();
			string escapeSearch = escape.ToString();
			string escapeReplacement = escapeSearch + escapeSearch;
			foreach (object v in elements)
			{
				string s = v != null ? v.ToString() : String.Empty;
				if (s.IndexOf(separator) >= 0)
				{
					output.Append('"');
					output.Append(s.Replace(escapeSearch, escapeReplacement).Replace("\"", escape + "\""));
					output.Append('"');
				}
				else
				{
					output.Append(s.Replace(escapeSearch, escapeReplacement));
				}
				output.Append(separator);
			}
			return output.Length > 0 ? output.ToString(0, output.Length - 1) : string.Empty;
		}
	}
}
