using System;
using System.ComponentModel.DataAnnotations;
using AlienForce.Utilities.Billing;

namespace AlienForce.Utilities.DataAnnotations
{
	public class CreditCardAttribute : ValidationAttribute
	{
		public CreditCardAttribute()
		{
			ErrorMessageResourceType = Resources.ResourceResolver.DefaultResourceType;
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
