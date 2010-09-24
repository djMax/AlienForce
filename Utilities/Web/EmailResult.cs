using System;using System.Collections.Generic;
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
		public enum RenderMode
		{
			/// <summary>
			/// By default, send an email
			/// </summary>
			Send,
			/// <summary>
			/// Render the output as text/html
			/// </summary>
			Html,
			/// <summary>
			/// Render the output as text/plain
			/// </summary>
			Text,
			/// <summary>
			/// Render the output as JSON details
			/// </summary>
			Json,
			/// <summary>
			/// Send an email, but return JSON indicating success/failure to the browser.
			/// Useful for AJAX scenarios.
			/// </summary>
			SendWithJson
		}

		MailMessage mMessage = new MailMessage();
		string mHTML;
		string mText;

		public RenderMode Mode { get; set; }

		public EmailResult(string from, string to, string subject, string textBody = null, string htmlBody = null) : this(EmailResult.RenderMode.Send, from, to, subject, textBody, htmlBody)
		{
		}

		public EmailResult(RenderMode mode, string from, string to, string subject, string textBody = null, string htmlBody = null)
		{
			Mode = mode;
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
			var cr = new ContentResult();
			if (Mode == RenderMode.Html)
			{
				cr.ContentType = "text/html";
				cr.Content = mHTML;
			}
			else if (Mode == RenderMode.Text)
			{
				cr.ContentType = "text/plain";
				cr.Content = mText;
			}
			else if (Mode == RenderMode.Json)
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
				if (Mode == RenderMode.SendWithJson)
				{
					var jr = new JsonResult();
					jr.Data = new { success = true };
					jr.ExecuteResult(context);
					return;
				}
				cr.Content = "OK";
			}
			cr.ExecuteResult(context);
		}

		public void Send()
		{
			new SmtpClient().Send(mMessage);
		}

	}
}
