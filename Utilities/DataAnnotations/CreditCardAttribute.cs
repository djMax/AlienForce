using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AlienForce.Utilities.Billing;

namespace AlienForce.Utilities.DataAnnotations
{
	public class CreditCardAttribute : ValidationAttribute
	{
		public CreditCardAttribute()
			: base()
		{
			ErrorMessageResourceType = AlienForce.Utilities.DataAnnotations.Resources.ResourceResolver.DefaultResourceType;
		}

		public override bool IsValid(object value)
		{
			if (value is CreditCardNumber)
			{
				return ((CreditCardNumber)value).IsValid;
			}

			string pan = value != null ? value.ToString() : null;

			if (String.IsNullOrEmpty(pan))
			{
				return false;
			}

			return new CreditCardNumber(pan).IsValid;
		}
	}

}
