using System;
using System.Collections.Generic;

namespace AlienForce.Utilities.Text
{
	/// <summary>
	/// TryParse for enumerations in a non-exception-generating way.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public static class EnumerationParser<T>
		where T : struct
	{
		private static Dictionary<string, T> Values = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);

		static EnumerationParser()
		{
			foreach (T t in System.Enum.GetValues(typeof(T)))
			{
				Values[t.ToString()] = t;
			}
		}

		/// <summary>
		/// Try to parse an enumeration value safely and quickly (i.e. not using exceptions)
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="result">The result.</param>
		/// <returns></returns>
		public static bool TryParse(string value, out T result)
		{
			if (String.IsNullOrEmpty(value))
			{
				result = default(T);
				return false; // sketchy.
			}
			if (value.IndexOf(',') >= 0)
			{
				string[] spl = value.Split(',');
				foreach (string s in spl)
				{
					if (!Values.ContainsKey(s))
					{
						result = default(T);
						return false;
					}
				}
				result = (T)System.Enum.Parse(typeof(T), value);
				return true;
			}
			else
			{
				return (Values.TryGetValue(value, out result));
			}
		}

		/// <summary>
		/// A type-safe version of System.Enum.Parse
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="ignoreCase">if set to <c>true</c> ignore letter casing.</param>
		/// <returns></returns>
		public static T Parse(string value, bool ignoreCase)
		{
			return (T)System.Enum.Parse(typeof(T), value, ignoreCase);
		}
	}
}
