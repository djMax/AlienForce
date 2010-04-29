using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.Utilities.DataAnnotations.Resources
{
	public static class ResourceResolver
	{
		/// <summary>
		/// The type of YOUR web applications resource settings for the "special" validator attributes
		/// to reference.
		/// </summary>
		public static Type DefaultResourceType;
	}

	public class RequiredAttribute : System.ComponentModel.DataAnnotations.RequiredAttribute
	{
		public RequiredAttribute()
		{
			ErrorMessageResourceName = "RequiredFieldError";
			ErrorMessageResourceType = ResourceResolver.DefaultResourceType;
		}
	}

	public class StringLengthAttribute : System.ComponentModel.DataAnnotations.StringLengthAttribute
	{
		public StringLengthAttribute(int maximumLength)
			: base(maximumLength)
		{
			ErrorMessageResourceName = "InvalidLengthError";
			ErrorMessageResourceType = ResourceResolver.DefaultResourceType;
		}
	}

	public class RangeAttribute : System.ComponentModel.DataAnnotations.RangeAttribute
	{
		public RangeAttribute(int minimum, int maximum)
			: base(minimum, maximum)
		{
			InitializeErrorMessageResource();
		}

		public RangeAttribute(double minimum, double maximum)
			: base(minimum, maximum)
		{
			InitializeErrorMessageResource();
		}

		public RangeAttribute(Type type, string minimum, string maximum)
			: base(type, minimum, maximum)
		{
			InitializeErrorMessageResource();
		}

		private void InitializeErrorMessageResource()
		{
			ErrorMessageResourceName = "OutOfRangeError";
			ErrorMessageResourceType = ResourceResolver.DefaultResourceType;
		}
	}

	public class RegularExpressionAttribute : System.ComponentModel.DataAnnotations.RegularExpressionAttribute
	{
		public RegularExpressionAttribute(string pattern)
			: base(pattern)
		{
			ErrorMessageResourceName = "InvalidFormatError";
			ErrorMessageResourceType = ResourceResolver.DefaultResourceType;
		}
	}
}
