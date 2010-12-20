using System;
using System.ComponentModel;

namespace AlienForce.Utilities.Billing
{
	public class ExpirationDate
	{
		public DateTime Date { get; private set; }

		public ExpirationDate(string monthYear)
		{
			DateTime d;
			if (!DateTime.TryParseExact(monthYear, "M/yy", null, System.Globalization.DateTimeStyles.AssumeUniversal, out d) &&
				!DateTime.TryParseExact(monthYear, "M/yyyy", null, System.Globalization.DateTimeStyles.AssumeUniversal, out d) &&
				!DateTime.TryParseExact(monthYear, "M/d/yy", null, System.Globalization.DateTimeStyles.AssumeUniversal, out d) &&
				!DateTime.TryParseExact(monthYear, "M/d/yyyy", null, System.Globalization.DateTimeStyles.AssumeUniversal, out d))
			{
				Date = DateTime.MinValue;
			}
			else
			{
				Date = d;
			}
		}

		public ExpirationDate(DateTime d)
		{
			Date = new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
		}
	}

	public class ExpirationDateTypeConverter : TypeConverter
	{
		public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
		{
			if (value == null)
			{
				return null;
			}
			if (value is DateTime)
			{
				return new ExpirationDate((DateTime)value);
			}
			return new ExpirationDate(value.ToString());
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return true;
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(string) || destinationType == typeof(DateTime);
		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			var cc = (ExpirationDate)value;
			if (destinationType == typeof(DateTime))
			{
				return cc.Date;
			}
			return cc.Date.Month + "/" + cc.Date.Year;
		}
	}

}
