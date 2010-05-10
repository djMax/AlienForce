using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using ICSharpCode.SharpZipLib.GZip;

namespace AlienForce.Utilities.Web
{
	/// <summary>
	/// A content result which can accept binart data and will write to the output
	/// stream.  If GZip is set to true the content will be GZipped and the relevant
	/// header added to the response HTTP Headers
	/// 
	/// Courtesy: http://weblogs.asp.net/andrewrea/archive/2010/02/16/a-binarycontentresult-for-asp-net-mvc.aspx
	/// </summary>
	public class BinaryContentResult : ActionResult
	{
		/// <summary>
		/// Construct an empty binary content result
		/// </summary>
		public BinaryContentResult()
		{
		}

		/// <summary>
		/// Construct a binary content result from a mime blob.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="contentType"></param>
		public BinaryContentResult(byte[] data, string contentType)
		{
			Data = data;
			Headers["Content-Type"] = contentType;
		}

		private Dictionary<string, string> _Headers;

		public Dictionary<string, string> Headers 
		{
			get
			{
				if (_Headers == null)
				{
					_Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				}
				return _Headers;
			}
		}

		public byte[] Data { get; set; }
		public bool Gzip { get; set; }

		public override void ExecuteResult(ControllerContext context)
		{
			if (_Headers != null)
			{
				foreach (var kv in Headers)
				{
					context.HttpContext.Response.AddHeader(kv.Key, kv.Value);
				}
			}

			if (Gzip)
			{
				using (var os = new GZipOutputStream(context.HttpContext.Response.OutputStream))
				{
					os.Write(Data, 0, Data.Length);
				}
				context.HttpContext.Response.AddHeader("Content-Encoding", "gzip");
			}
			else
			{
				context.HttpContext.Response.BinaryWrite(Data);
			}

			context.HttpContext.Response.End();
		}
	}
}
