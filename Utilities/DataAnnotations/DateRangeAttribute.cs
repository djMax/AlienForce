using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace AlienForce.Utilities.DataAnnotations
{
	public class DateRangeAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
	{
		private DateTime? MinValue = DateTime.MinValue;
		private DateTime? MaxValue = DateTime.MaxValue;
		private string[] _Patterns;
		public string DatePatterns { get; set; }

		public DateRangeAttribute(DateTime minValue, DateTime maxValue)
		{
			MinValue = minValue;
			MaxValue = maxValue;
			InitializeErrorMessageResource();
		}

		/// <summary>
		/// Validate a date range
		/// </summary>
		/// <param name="nowOrLater"></param>
		public DateRangeAttribute(bool nowOrLater)
		{
			if (nowOrLater)
			{
				MinValue = null; // special value for "at the time of validation"
				MaxValue = DateTime.MaxValue;
			}
			else
			{
				MinValue = DateTime.MinValue;
				MaxValue = null;
			}
			InitializeErrorMessageResource();
		}

		private void InitializeErrorMessageResource()
		{
			ErrorMessageResourceName = "DateRangeError";
			ErrorMessageResourceType = AlienForce.Utilities.DataAnnotations.Resources.ResourceResolver.DefaultResourceType;
		}

		public override bool IsValid(object value)
		{
			if (value != null && value is string)
			{
				value = ConvertDate((string)value);
			}
			if (value == null) { return false; }
			TypeConverter conv;
			DateTime date;
			if (value is DateTime)
			{
				date = (DateTime)value;
			}
			else if ((conv = TypeDescriptor.GetConverter(value)) != null && conv.CanConvertTo(typeof(DateTime)))
			{
				date = (DateTime) conv.ConvertTo(value, typeof(DateTime));
			}
			else
			{
				date = Convert.ToDateTime(value);
			}
			if (MinValue == null)
			{
				return date >= DateTime.Now;
			}
			else if (MaxValue == null)
			{
				return date <= DateTime.Now;
			}
			return (date >= MinValue && date <= MaxValue);
		}

		private object ConvertDate(string value)
		{
			if (String.IsNullOrWhiteSpace(DatePatterns)) { return null; }
			if (_Patterns == null)
			{
				_Patterns = DatePatterns.Split('|');
			}
			var dt = ConvertDate(value, _Patterns);
			if (dt == null) { return null; }
			return dt.Value;
		}

		public static DateTime? ConvertDate(string value, string[] patterns)
		{
			foreach (string p in patterns)
			{
				DateTime cand;
				if (DateTime.TryParseExact(value, p, null, System.Globalization.DateTimeStyles.AssumeLocal, out cand))
				{
					return cand;
				}
			}
			return null;
		}
	}
}
