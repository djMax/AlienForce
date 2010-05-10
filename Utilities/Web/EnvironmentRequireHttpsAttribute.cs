using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace AlienForce.Utilities.Web
{
	public class EnvironmentRequireHttpsAttribute : System.Web.Mvc.RequireHttpsAttribute
	{
		public string In  { get; set; }
		public string NotIn { get; set; }

		public override void OnAuthorization(System.Web.Mvc.AuthorizationContext filterContext)
		{
			if (String.IsNullOrWhiteSpace(In) && String.IsNullOrWhiteSpace(NotIn))
			{
				throw new ArgumentException("The EnvironmentRequireHttps attribute must have In or NotIn set in order to function.");
			}
			var app = filterContext.HttpContext.ApplicationInstance as AlienForceMvcApplication;
			if (app == null)
			{
				throw new InvalidCastException("Your application must derive from AlienForceMvcApplication to use the EnvironmentRequireHttps attribute.");
			}
			// If In is set, and Not In is not set or does not apply, go for it.
			if (In != null && app.HasEnvironmentTag(In) && (NotIn == null || !app.HasEnvironmentTag(NotIn)))
			{
				base.OnAuthorization(filterContext);
			}
			// Otherwise, if NotIn is set, just check that.
			else if (NotIn != null && !app.HasEnvironmentTag(NotIn))
			{
				base.OnAuthorization(filterContext);
			}
		}

	}
}
