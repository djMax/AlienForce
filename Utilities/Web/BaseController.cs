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

		protected string RenderPartialViewToString(string viewName, object model)
		{
			if (string.IsNullOrEmpty(viewName))
				viewName = ControllerContext.RouteData.GetRequiredString("action");

			ViewData.Model = model;

			using (StringWriter sw = new StringWriter())
			{
				ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
				ViewContext viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
				viewResult.View.Render(viewContext, sw);

				return sw.GetStringBuilder().ToString();
			}
		}

		protected string RenderViewToString(string viewName, object model)
		{
			if (string.IsNullOrEmpty(viewName))
				viewName = ControllerContext.RouteData.GetRequiredString("action");

			ViewData.Model = model;

			using (StringWriter sw = new StringWriter())
			{
				var viewResult = ViewEngines.Engines.FindView(ControllerContext, viewName, null);
				var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
				viewResult.View.Render(viewContext, sw);

				return sw.GetStringBuilder().ToString();
			}
		}

		protected EmailResult EmailResult(string from, string to, string subject,
			string textBody = null, string htmlBody = null, string textView = null, string htmlView = null,
			object viewData = null)
		{
			if (htmlView != null)
			{
				htmlBody = RenderViewToString(htmlView, viewData);
			}
			if (textView != null)
			{
				textBody = RenderViewToString(textView, viewData);
			}
			return new EmailResult(from, to, subject, textBody, htmlBody);
		}
	}
}
