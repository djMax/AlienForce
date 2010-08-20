using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Net.Mail;
using System.IO;

namespace AlienForce.Utilities.Web
{
	/// <summary>
	/// When your controller returns this ActionResult derivative, we will in fact send an email rather
	/// than render content to the browser.  This is a part of the "ASP.Net based email templating system"
	/// in AlienForce.  In concert with BaseController's EmailResult helper allows you to use the MVC
	/// framework to generate data driven emails.
	/// </summary>
	public class EmailResult : ActionResult
	{
		MailMessage mMessage = new MailMessage();
		string mHTML;
		string mText;

		public EmailResult(string from, string to, string subject, string textBody = null, string htmlBody = null)
		{
			mText = textBody;
			mHTML = htmlBody;
			mMessage.From = new MailAddress(from);
			mMessage.To.Add(new MailAddress(to));
			mMessage.Subject = subject;
			if (textBody != null && htmlBody != null)
			{
				mMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody));
				mMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, new System.Net.Mime.ContentType("text/html")));
			}
			else
			{
				mMessage.Body = textBody ?? htmlBody;
				mMessage.IsBodyHtml = textBody == null;
			}
		}

		public override void ExecuteResult(ControllerContext context)
		{
			var renderMode = context.RequestContext.HttpContext.Request.QueryString["renderMode"];
			var cr = new ContentResult();
			if (renderMode == "html")
			{
				cr.ContentType = "text/html";
				cr.Content = mHTML;
			}
			else if (renderMode == "text")
			{
				cr.ContentType = "text/plain";
				cr.Content = mText;
			}
			else if (renderMode == "json")
			{
				JsonResult jr = new JsonResult();
				jr.Data = new
				{
					mMessage.From,
					mMessage.To,
					mMessage.CC,
					mMessage.Bcc,
					mMessage.Headers,
					mMessage.Priority,
					mMessage.ReplyToList,
					mMessage.Sender,
					mMessage.Subject,
					mMessage.SubjectEncoding
				};
				jr.ExecuteResult(context);
				return;
			}				
			else
			{
				new SmtpClient().Send(mMessage);
				cr.Content = "OK";
			}
			cr.ExecuteResult(context);
		}
	}
}
