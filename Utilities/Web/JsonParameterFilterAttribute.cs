using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.IO;

namespace AlienForce.Utilities.Web
{
	/// <summary>
	/// Treat an parameter as a JSON object and deserialize it.
	/// </summary>
	public class JsonParameterFilterAttribute : ActionFilterAttribute
	{
		public JsonParameterFilterAttribute()
		{
		}

		public JsonParameterFilterAttribute(string parameterName, Type rootType)
		{
			Parameter = parameterName;
			RootType = rootType;
		}

		public string Parameter { get; set; }
		public Type RootType { get; set; }

		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			if ((filterContext.HttpContext.Request.ContentType ?? string.Empty).Contains("application/json"))
			{
				try
				{
					var o = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize(new StreamReader(filterContext.HttpContext.Request.InputStream, Encoding.UTF8).ReadToEnd(), RootType);
					filterContext.ActionParameters[Parameter] = o;
				}
				catch (Exception xc)
				{
					throw;
				}
			}
		}
	}
}
