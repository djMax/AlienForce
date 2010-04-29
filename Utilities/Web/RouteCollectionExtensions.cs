using System;
using System.Linq;
using System.Reflection;
using System.Web.Routing;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace AlienForce.Utilities.Web
{
	public static class RouteCollectionExtensions
	{
		public static RouteCollection MapRoutes(this RouteCollection routes)
		{
			return MapRoutes(routes, Assembly.GetCallingAssembly());
		}

		public static RouteCollection MapRoutes(this RouteCollection routes, Assembly assembly)
		{
			if (routes == null)
			{
				throw new ArgumentNullException("routes");
			}

			if (assembly == null)
			{
				throw new ArgumentNullException("assembly");
			}

			assembly.GetTypes()
				// Find all non abstract classes of type Controller and whose names end with "Controller"
				.Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof(Controller)) && x.Name.EndsWith("Controller"))

				// Find all public methods from those controller classes
				.SelectMany(x => x.GetMethods(), (x, y) => new { Controller = x.Name, Method = y, Namespace = x.Namespace })

				// Find all route attributes from those methods
				.SelectMany(x => x.Method.GetCustomAttributes(typeof(UrlRouteAttribute), false),
										(x, y) => new { Controller = x.Controller.Substring(0, x.Controller.Length - 10), Action = x.Method.Name, Method = x.Method, Namespace = x.Namespace, Route = (UrlRouteAttribute)y })

				// Order selected entires by rank number and iterate through each of them
				.OrderBy(x => x.Route.Order == -1).ThenBy(x => x.Route.Order).ToList().ForEach(x =>
				{
					// Set Defautls
					var defaults = GetDefaults(x.Method);
					defaults.Add("controller", x.Controller);
					defaults.Add("action", x.Action);

					// Set Optional Parameters and remove '?' mark from the url
					Match m;

					while ((m = Regex.Match(x.Route.Path, @"\{([^\}]+?)\?\}")) != null && m.Success)
					{
						var p = m.Groups[1].Value;
						defaults.Add(p, UrlParameter.Optional);
						x.Route.Path = x.Route.Path.Replace("{" + p + "?}", "{" + p + "}");
					}

					// Set Defautls
					var constraints = GetConstraints(x.Method);

					// Set Data Tokens
					var dataTokens = new RouteValueDictionary();
					dataTokens.Add("Namespaces", new string[] { x.Namespace });

					var route = new Route(x.Route.Path, new MvcRouteHandler())
					{
						Defaults = new RouteValueDictionary(defaults),
						Constraints = new RouteValueDictionary(constraints),
						DataTokens = dataTokens
					};

					if (x.Route.Name == null)
					{
						routes.Add(route);
					}
					else
					{
						routes.Add(x.Route.Name, route);
					}
				});

			return routes;
		}

		private static Dictionary<string, object> GetConstraints(MethodInfo mi)
		{
			Dictionary<string, object> constraints = new Dictionary<string, object>();

			foreach (UrlRouteParameterConstraintAttribute attrib in mi.GetCustomAttributes(typeof(UrlRouteParameterConstraintAttribute), true))
			{
				if (String.IsNullOrEmpty(attrib.Name))
				{
					throw new ApplicationException(String.Format("UrlRouteParameterContraint attribute on {0}.{1} is missing the Name property.",
							mi.DeclaringType.Name, mi.Name));
				}

				if (String.IsNullOrEmpty(attrib.Regex))
				{
					throw new ApplicationException(String.Format("UrlRouteParameterContraint attribute on {0}.{1} is missing the RegEx property.",
							mi.DeclaringType.Name, mi.Name));
				}

				constraints.Add(attrib.Name, attrib.Regex);
			}

			return constraints;
		}

		private static Dictionary<string, object> GetDefaults(MethodInfo mi)
		{
			Dictionary<string, object> defaults = new Dictionary<string, object>();

			foreach (UrlRouteParameterDefaultAttribute attrib in mi.GetCustomAttributes(typeof(UrlRouteParameterDefaultAttribute), true))
			{
				if (String.IsNullOrEmpty(attrib.Name))
				{
					throw new ApplicationException(String.Format("UrlRouteParameterDefault attribute on {0}.{1} is missing the Name property.",
							mi.DeclaringType.Name, mi.Name));
				}

				if (attrib.Value == null)
				{
					throw new ApplicationException(String.Format("UrlRouteParameterDefault attribute on {0}.{1} is missing the Value property.",
							mi.DeclaringType.Name, mi.Name));
				}

				defaults.Add(attrib.Name, attrib.Value);
			}

			return defaults;
		}

	}
}
