using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;
using System.Reflection;
using AlienForce.Utilities.DataAnnotations;
using AlienForce.Utilities.DataAnnotations.Resources;
using AlienForce.Utilities.Billing;
using System.ComponentModel;

namespace AlienForce.Utilities.Web
{
	public class AlienForceMvcApplication : System.Web.HttpApplication
	{
		public virtual void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
			routes.MapRoutes(Assembly.GetCallingAssembly());
		}

		public virtual void RegisterRoutes(RouteCollection routes, Assembly assembly)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
			routes.MapRoutes(assembly);
		}

		protected virtual void Application_Start()
		{
			Logging.LogFramework.Framework.Initialize();
			AreaRegistration.RegisterAllAreas();
			RegisterRoutes(RouteTable.Routes);

			DataAnnotationsModelValidatorProvider.RegisterAdapter(typeof(RequiredAttribute), typeof(System.Web.Mvc.RequiredAttributeAdapter));
			DataAnnotationsModelValidatorProvider.RegisterAdapter(typeof(StringLengthAttribute), typeof(System.Web.Mvc.StringLengthAttributeAdapter));
			DataAnnotationsModelValidatorProvider.RegisterAdapter(typeof(EmailAttribute), typeof(System.Web.Mvc.RegularExpressionAttributeAdapter));
			DataAnnotationsModelValidatorProvider.RegisterAdapter(typeof(RangeAttribute), typeof(System.Web.Mvc.RangeAttributeAdapter));

			System.ComponentModel.TypeDescriptor.AddAttributes(typeof(CreditCardNumber), new TypeConverterAttribute(typeof(CreditCardNumberTypeConverter)));
			System.ComponentModel.TypeDescriptor.AddAttributes(typeof(ExpirationDate), new TypeConverterAttribute(typeof(ExpirationDateTypeConverter)));

		}
	}
}
