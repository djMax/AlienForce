using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace AlienForce.Utilities.Web
{
	/// <summary>
	/// There are cases where you want to call a controller action from INSIDE a controller and get the ActionResult back.  The particular
	/// case that yielded this was interactive rendering of email templates using EmailResult.  If you simply instantiate a controller
	/// and call your method, the context of views will be your caller, not the controller you called (for obvious reasons - namely that
	/// the runtime has no idea what contexts the called controller might exist in).  This helper wrapper (should be in a "using" clause)
	/// will substitute the appropriate values in the RouteData and then put them back as they were when disposed.
	/// </summary>
	public class ChildActionWrapper : IDisposable
	{
		public IController Controller { get; private set; }

		public T GetController<T>() where T : IController
		{
			return (T) Controller;	
		}

		ControllerContext mContext;

		bool mReplaced;

		bool mHadArea;
		object mOriginalArea;
		
		object mOriginalController;

		public ChildActionWrapper(ControllerContext c, string area, string controller)
		{
			mContext = c;
			mHadArea = c.RouteData.Values.TryGetValue("area", out mOriginalArea);
			mOriginalController = c.RouteData.GetRequiredString("controller");

			c.RouteData.Values["area"] = area;
			c.RouteData.Values["controller"] = controller;
			mReplaced = true;

			
			Controller = ControllerBuilder.Current.GetControllerFactory().CreateController(c.RequestContext, controller);
		}

		#region IDisposable Members

		void IDisposable.Dispose()
		{
			if (mReplaced)
			{
				if (mHadArea)
				{
					mContext.RouteData.Values["area"] = mOriginalArea;
				}
				else
				{
					mContext.RouteData.Values.Remove("area");
				}
				mContext.RouteData.Values["controller"] = mOriginalController;
				mOriginalController = mOriginalArea = null;
				mReplaced = false;
			}
		}

		#endregion
	}
}
