using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.IO;

namespace AlienForce.Utilities.Web
{
	public abstract class BaseController : Controller
	{
		protected string RenderPartialViewToString()
		{
			return RenderPartialViewToString(null, null);
		}

		protected string RenderPartialViewToString(string viewName)
		{
			return RenderPartialViewToString(viewName, null);
		}

		protected string RenderPartialViewToString(object model)
		{
			return RenderPartialViewToString(null, model);
		}

		protected string RenderPartialViewToString(string viewName, object model = null)
		{
			if (string.IsNullOrEmpty(viewName))
			{
				viewName = ControllerContext.RouteData.GetRequiredString("action");
			}

			ViewData.Model = model;

			using (StringWriter sw = new StringWriter())
			{
				ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
				ViewContext viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
				viewResult.View.Render(viewContext, sw);

				return sw.GetStringBuilder().ToString();
			}
		}

		protected string RenderViewToString(string viewName, object model = null)
		{
			if (string.IsNullOrEmpty(viewName))
			{
				viewName = ControllerContext.RouteData.GetRequiredString("action");
			}

			var viewData = new ViewDataDictionary(model);

			using (StringWriter sw = new StringWriter())
			{
				var viewResult = ViewEngines.Engines.FindView(ControllerContext, viewName, null);
				var viewContext = new ViewContext(ControllerContext, viewResult.View, viewData, TempData, sw);
				viewResult.View.Render(viewContext, sw);

				return sw.GetStringBuilder().ToString();
			}
		}

		protected string RenderViewToString<T>(string viewName, T model)
		{
			if (string.IsNullOrEmpty(viewName))
			{
				viewName = ControllerContext.RouteData.GetRequiredString("action");
			}

			var viewData = new ViewDataDictionary<T>(model);

			using (StringWriter sw = new StringWriter())
			{
				var viewResult = ViewEngines.Engines.FindView(ControllerContext, viewName, null);
				var viewContext = new ViewContext(ControllerContext, viewResult.View, viewData, TempData, sw);
				viewResult.View.Render(viewContext, sw);

				return sw.GetStringBuilder().ToString();
			}
		}

		protected ActionResult ServerTransferResult(object routeValues)
		{
			return new ServerTransferResult(routeValues);
		}
	}
}
