using System;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace AlienForce.Utilities.Billing
{
	public enum CardType
	{
		Unknown = 0,
		Visa = 1,
		MasterCard = 2,
		Discover = 3,
		AmericanExpress = 4,
		JCB = 5,
		DinersClub = 6
	}

	[TypeConverter(typeof(CreditCardNumberTypeConverter))]
	public class CreditCardNumber
	{
		static Regex VisaRE = new Regex("^4[0-9]{12}(?:[0-9]{3})?$", RegexOptions.Compiled);
		static Regex MCRE = new Regex("^5[1-5][0-9]{14}$", RegexOptions.Compiled);
		static Regex AmexRE = new Regex("^3[47][0-9]{13}$", RegexOptions.Compiled);
		static Regex DinersRE = new Regex("^3(?:0[0-5]|[68][0-9])[0-9]{11}$", RegexOptions.Compiled);
		static Regex DiscoverRE = new Regex("^6(?:011|5[0-9]{2})[0-9]{12}$", RegexOptions.Compiled);
		static Regex JCBRE = new Regex(@"^(?:2131|1800|35\d{3})\d{11}$", RegexOptions.Compiled);

		/// <summary>
		/// The number as it was entered by the user
		/// </summary>
		public string AsEntered { get; private set; }
		/// <summary>
		/// Card number with whitespace and dashes removed.  Other invalid characters are preserved, so that
		/// some crazy sentence with numbers that add up to the right checksum value will still fail.
		/// </summary>
		public string Cleaned { get; private set; }
		/// <summary>
		/// The type of card, based on the number, if a known type of card.
		/// </summary>
		public CardType CardType { get; private set; }
		/// <summary>
		/// True if this is a valid credit card number
		/// </summary>
		public bool IsValid { get; private set; }

		/// <summary>
		/// Construct a credit card number from a user-entered value.
		/// </summary>
		/// <param name="asEntered"></param>
		public CreditCardNumber(string asEntered)
		{
			bool needsValidate = true;
			AsEntered = asEntered;
			if (asEntered != null)
			{
				var sb = new StringBuilder(asEntered.Length);
				foreach (char c in asEntered)
				{
					// Whitespace and - are allowed punctuation, others should make this invalid.
					if (Char.IsDigit(c))
					{
						sb.Append(c);
					}
					else if (!Char.IsWhiteSpace(c) && c != '-')
					{
						needsValidate = false;
						sb.Append(c);
					}
				}
				Cleaned = sb.ToString();
				if (needsValidate)
				{
					Validate(Cleaned);
				}
			}
		}

		private void Validate(string cleanNumber)
		{
			var number = new byte[16]; // number to validate

			// Remove non-digits
			if (cleanNumber.Length > 16)
			{
				IsValid = false;
				return;
			}

			var len = cleanNumber.Length;
			for (int i = 0; i < len; i++)
			{
				number[i] = (byte)(cleanNumber[i] - '0');
			}

			// Use Luhn Algorithm to validate
			var sum = 0;
			for (int i = len - 1; i >= 0; i--)
			{
				if (i % 2 == len % 2)
				{
					int n = number[i] * 2;
					sum += (n / 10) + (n % 10);
				}
				else
					sum += number[i];
			}

			IsValid = sum % 10 == 0;
			if (IsValid)
			{
				switch (cleanNumber[0])
				{
					case '1':
					case '2':
						if (JCBRE.IsMatch(cleanNumber))
						{
							CardType = CardType.JCB;
						}
						return;
					case '3':
						if (AmexRE.IsMatch(cleanNumber))
						{
							CardType = CardType.AmericanExpress;
						}
						else if (DinersRE.IsMatch(cleanNumber))
						{
							CardType = CardType.DinersClub;
						}
						else if (JCBRE.IsMatch(cleanNumber))
						{
							CardType = CardType.JCB;
						}
						return;
					case '4':
						if (VisaRE.IsMatch(cleanNumber))
						{
							CardType = CardType.Visa;
						}
						return;
					case '5':
						if (MCRE.IsMatch(cleanNumber))
						{
							CardType = CardType.MasterCard;
						}
						return;
					case '6':
						if (DiscoverRE.IsMatch(cleanNumber))
						{
							CardType = CardType.Discover;
						}
						return;
				}
			}
		}
	}

	public class CreditCardNumberTypeConverter : TypeConverter
	{
		public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
		{
		    return value == null ? null : new CreditCardNumber(value.ToString());
		}

	    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return true;
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(string);
		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			var cc = (CreditCardNumber)value;
			return cc.AsEntered;
		}
	}

}
