﻿
namespace AlienForce.Utilities.DataAnnotations
{
	public class EmailAttribute : System.ComponentModel.DataAnnotations.RegularExpressionAttribute
	{
		public EmailAttribute()
			: base(@"^([A-Za-z0-9-\.\+_]+)\@((?:[\da-zA-Z-]+\.)+(?:[\da-zA-Z-]{2,4}|[Mm][Uu][Ss][Ee][Uu][Mm]|[Tt][Rr][Aa][Vv][Ee][Ll]))$")
		{
			ErrorMessageResourceName = "EmailAddressError";
			ErrorMessageResourceType = Resources.ResourceResolver.DefaultResourceType;
		}
	}
}
