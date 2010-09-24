using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace AlienForce.Utilities.Web
{
	public class ServerTransferResult : RedirectResult
	{
		public ServerTransferResult(string url)
			: base(url)
		{
		}

		public ServerTransferResult(object routeValues)
			: base(GetRouteURL(routeValues))
		{
		}

		private static string GetRouteURL(object routeValues)
		{
			UrlHelper url = new UrlHelper(new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()), RouteTable.Routes);
			return url.RouteUrl(routeValues);
		}

		public override void ExecuteResult(ControllerContext context)
		{
			var httpContext = HttpContext.Current;

			httpContext.RewritePath(Url, false);

			IHttpHandler httpHandler = new MvcHttpHandler();
			httpHandler.ProcessRequest(HttpContext.Current);
		}
	}
}
