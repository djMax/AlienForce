using System;
using System.Text;
using System.Collections.Generic;

namespace AlienForce.Utilities.Text
{
	/// <summary>
	/// Standard composite formatting with one change... Instead of numbered indexes, you can use names. 
	/// </summary>
	public sealed class NamedStringFormatter
	{
		/// <summary>
		/// This delegate allows you to replace missing values with your own text, potentially
		/// retrieved from an external source, or marked with big exclamation points, or whatever.
		/// </summary>
		public delegate string MissingValueHandler(string key, string completeString, int startPoint, int endPoint);

		/// <summary>
		/// <seealso cref="T:System.String.Format"/>
		/// </summary>
		public static string Format(IFormatProvider provider, string format, IDictionary<string, object> context)
		{
			return Format(provider, format, context, null);
		}

		/// <summary>
		/// <seealso cref="T:System.String.Format"/>.  This version takes a string builder and outputs to that instead of returning a new string.
		/// </summary>
		public static void Format(IFormatProvider provider, string format, IDictionary<string, object> context, MissingValueHandler handler, StringBuilder result)
		{
			if (format == null)
				throw new ArgumentNullException("format");

			// Nothing to see here.  Move along.
			if (format.IndexOf('{') < 0)
			{
				result.Append(format);
				return;
			}

			if (context == null)
			{
				throw new ArgumentNullException("context");
			}

			int ptr = 0;
			int start = ptr;
			while (ptr < format.Length)
			{
				char c = format[ptr++];

				if (c == '{')
				{
					result.Append(format, start, ptr - start - 1);

					// check for escaped open bracket or whitespace

					if (format[ptr] == '{' || Char.IsWhiteSpace(format[ptr]))
					{
						start = ptr++;
						continue;
					}

					// parse specifier

					int width, formatStart = ptr;
					bool left_align;
					string arg_format, arg_name;

					ParseFormatSpecifier(format, ref ptr, out arg_name, out width, out left_align, out arg_format);
					object arg = null;
					if (!context.ContainsKey(arg_name))
					{
						if (handler == null)
						{
							throw new FormatException(String.Format("The named argument {{{0}}} must exist in the context.", arg_name));
						}
						else
						{
							arg = handler(arg_name, format, formatStart, ptr);
						}
					}
					else
						arg = context[arg_name];

					string str;
					if (arg == null)
						str = "";
					else if (arg is IFormattable)
						str = ((IFormattable)arg).ToString(arg_format, provider);
					else
						str = arg.ToString();

					// pad formatted string and append to result
					if (width > (str == null ? 0 : str.Length))
					{
						string pad = new String(' ', width - str.Length);

						if (left_align)
						{
							result.Append(str);
							result.Append(pad);
						}
						else
						{
							result.Append(pad);
							result.Append(str);
						}
					}
					else
						result.Append(str);

					start = ptr;
				}
				else if (c == '}' && ptr < format.Length && format[ptr] == '}')
				{
					result.Append(format, start, ptr - start - 1);
					start = ptr++;
				}
				else if (c == '}')
				{
					throw new FormatException("Input string was not in a correct format.");
				}
			}

			if (start < format.Length)
				result.Append(format.Substring(start));
		}

		/// <summary>
		/// <seealso cref="T:System.String.Format"/>
		/// </summary>
		public static string Format(IFormatProvider provider, string format, IDictionary<string, object> context, MissingValueHandler handler)
		{
			if (format == null)
				throw new ArgumentNullException("format");

			// Nothing to see here.  Move along.
			if (format.IndexOf('{') < 0)
				return format;

			StringBuilder result = new StringBuilder(format.Length);
			Format(provider, format, context, handler, result);
			return result.ToString();
		}

		/// <summary>
		/// <seealso cref="T:System.String.Format"/>
		/// </summary>
		public static string Format(string format, IDictionary<string, object> context)
		{
			return Format(null, format, context);
		}

		private static void ParseFormatSpecifier(string str, ref int ptr, out string n, out int width,
			out bool left_align, out string format)
		{
			// parses format specifier of form:
			//   N,[\ +[-]M][:F]}
			//
			// where:

			try
			{
				// N = argument name

				n = ParseName(str, ref ptr);
				if (n == null)
					throw new FormatException("Input string was not in a correct format.");

				// M = width (non-negative integer)

				if (str[ptr] == ',')
				{
					// White space between ',' and number or sign.
					int start = ++ptr;
					while (Char.IsWhiteSpace(str[ptr]))
						++ptr;

					format = str.Substring(start, ptr - start);

					left_align = (str[ptr] == '-');
					if (left_align)
						++ptr;

					width = ParseDecimal(str, ref ptr);
					if (width < 0)
						throw new FormatException("Input string was not in a correct format.");
				}
				else
				{
					width = 0;
					left_align = false;
					format = "";
				}

				// F = argument format (string)

				if (str[ptr] == ':')
				{
					int start = ++ptr;
					while (str[ptr] != '}')
						++ptr;

					format += str.Substring(start, ptr - start);
				}
				else
					format = null;

				if (str[ptr++] != '}')
					throw new FormatException("Input string was not in a correct format.");
			}
			catch (IndexOutOfRangeException)
			{
				throw new FormatException("Input string was not in a correct format.");
			}
		}

		private static char[] _sFormatTerm = new char[] { ':', '}' };

		private static string ParseName(string str, ref int ptr)
		{
			int nptr = str.IndexOfAny(_sFormatTerm, ptr);
			if (nptr < 0 || nptr == ptr)
				return null;
			string ret = str.Substring(ptr, nptr - ptr);
			ptr = nptr;
			return ret;
		}

		private static int ParseDecimal(string str, ref int ptr)
		{
			int p = ptr;
			int n = 0;
			while (true)
			{
				char c = str[p];
				if (c < '0' || '9' < c)
					break;

				n = n * 10 + c - '0';
				++p;
			}

			if (p == ptr)
				return -1;

			ptr = p;
			return n;
		}
	}
}
