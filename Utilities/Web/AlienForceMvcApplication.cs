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
using AlienForce.Utilities.Logging;
using System.Configuration;

namespace AlienForce.Utilities.Web
{
	public class AlienForceMvcApplication : System.Web.HttpApplication
	{
		static ILog Log = LogFramework.Framework.GetLogger(typeof(AlienForceMvcApplication));

		private static Dictionary<string, bool> _EnvironmentTags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
		private static bool _DebugEnv;

		/// <summary>
		/// Environment tags can be used for things like 'DEBUG' or 'NOSSL' that control major decisions
		/// about the way the site should operate. This allows attributes to control whether SSL is required,
		/// roles are enforced, and etc.  The tags are pulled from the app setting "AlienForce.EnvironmentTags"
		/// and should be separated by commas.  Tags are case-insensitive.
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		public virtual bool HasEnvironmentTag(string tag)
		{
			if (String.Equals(tag, "DEBUG", StringComparison.OrdinalIgnoreCase))
			{
				return _DebugEnv;
			}
			return _EnvironmentTags.ContainsKey(tag);
		}

		public virtual void RegisterRoutes(RouteCollection routes)
		{
			RegisterRoutes(routes, Assembly.GetCallingAssembly());
		}

		public virtual void RegisterRoutes(RouteCollection routes, Assembly assembly)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
			routes.IgnoreRoute("favicon.ico");
			routes.MapRoutes(assembly);
		}

		protected virtual void Application_Start()
		{
			Logging.LogFramework.Framework.Initialize();

			string tags = ConfigurationManager.AppSettings["AlienForce.EnvironmentTags"];
			if (!String.IsNullOrWhiteSpace(tags))
			{
				string[] tagSplit = tags.Split(',');
				foreach (string s in tagSplit)
				{
					string tr = s.Trim();
					if (String.Equals(tr, "DEBUG", StringComparison.OrdinalIgnoreCase))
					{
						_DebugEnv = true;
					}
					_EnvironmentTags[tr] = true;
				}
				Log.InfoFormat("Application_Start beginning with environment tags: '{0}'.", tags);
			}
			else
			{
				Log.Info("Application_Start beginning (no environment tags set).");
			}
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
