using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Web;
using AlienForce.Utilities.Logging;
using ICSharpCode.SharpZipLib.Zip;
using System.DirectoryServices;

namespace AlienForce.Utilities.Web
{
	/// <summary>
	/// An HTTP handler to provide direct URL access to the contents of zip files.  Can be useful when using packages with large numbers
	/// of files (such as Javascript UI libraries) in a source-controlled environment where you don't want hundreds of files separately
	/// tracked in source control.  This handler attempts to deal with caching "sanely" by respoding with ETags and respecting if-modified-since
	/// requests.
	/// </summary>
	public class ZipFileHttpHandler : IHttpHandler
	{
		static Dictionary<string, string> _MimeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		static ILog _Log = LogFramework.Framework.GetLogger(typeof(ZipFileHttpHandler));

		[ComImport, TypeLibType((short)0x1040), Guid("9036B027-A780-11D0-9B3D-0080C710EF95")]
		private interface IISMimeType
		{
			[DispId(4)]
			string MimeType { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)] get; [param: In, MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)] set; }
			[DispId(5)]
			string Extension { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(5)] get; [param: In, MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(5)] set; }
		}

		/// <summary>
		/// Get the mime type for a request
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public static string GetMimeType(HttpRequest request)
		{
			string ext = new System.IO.FileInfo(request.FilePath).Extension;
			string mbPath = HttpRuntime.AppDomainAppId;
			mbPath = mbPath.Substring(mbPath.IndexOf('/', 1));
			mbPath = "IIS://localhost" + mbPath + "/MimeMap";

			lock (_MimeMap)
			{
				string mt;
				if (_MimeMap.TryGetValue(ext, out mt))
				{
					return mt;
				}
				// Define the path to the metabase
				try
				{
					// Talk to the IIS Metabase to read the MimeMap Metabase key
					DirectoryEntry MimeMap = null;
					PropertyValueCollection pvc = null;
					try
					{
						MimeMap = new DirectoryEntry(mbPath);
						// Get the Mime Types as a collection
						pvc = MimeMap.Properties["MimeMap"];
					}
					catch
					{
						MimeMap = new DirectoryEntry("IIS://localhost/MimeMap");
						// Get the Mime Types as a collection
						pvc = MimeMap.Properties["MimeMap"];
					}
					foreach (object po in pvc)
					{
						IISMimeType imt = (IISMimeType)po;
						if (imt.Extension.Equals(ext))
						{
							return (_MimeMap[ext] = imt.MimeType);
						}
					}
				}
				catch (Exception ex)
				{
					if (ex.Message.IndexOf("0x80005006") >= 0)
					{
						_Log.WarnFormat("Property MimeMap does not exist at {0}", mbPath);
					}
					else
					{
						_Log.WarnFormat("An exception has occurred: \n{0}", ex.Message);
					}
				}
				return null;
			}
		}

		#region IHttpHandler Members

		/// <summary>
		/// Yes, we can be reused.
		/// </summary>
		public bool IsReusable
		{
			get { return true; }
		}

		/// <summary>
		/// Process an incoming HTTP request
		/// </summary>
		/// <param name="context"></param>
		public void ProcessRequest(HttpContext context)
		{
			string mtype = GetMimeType(context.Request);
			context.Response.ContentType = mtype;
			FileInfo req = new FileInfo(context.Server.MapPath(context.Request.FilePath));
			if (req.Exists)
			{
				if (ManageModified(context, req)) { return; }
				context.Response.TransmitFile(context.Request.FilePath);
			}
			else
			{
				string rootDir = context.Server.MapPath("~");
				DirectoryInfo di = req.Directory;
				string name = req.Name;
				FileInfo[] zips;
				while (!di.Exists || (zips = di.GetFiles(name + ".zip")) == null || zips.Length == 0)
				{
					name = di.Name;
					di = di.Parent;
					if (di == null || !di.FullName.StartsWith(rootDir, StringComparison.OrdinalIgnoreCase))
					{
						context.Response.StatusCode = 404;
						context.Response.End();
						return;
					}
				}
				if (ManageModified(context, zips[0])) { return; }
				using (ICSharpCode.SharpZipLib.Zip.ZipFile zf = new ICSharpCode.SharpZipLib.Zip.ZipFile(zips[0].FullName))
				{
					string fname = req.FullName.Substring(di.FullName.Length + name.Length + 2);
					int entryId = zf.FindEntry(fname, true);
					if (entryId < 0)
					{
						fname = fname.Replace('\\', '/');
						entryId = zf.FindEntry(fname, true);
					}
					if (entryId < 0)
					{
						context.Response.StatusCode = 404;
						context.Response.End();
						return;
					}
					ZipEntry ze = zf[entryId];
					using (Stream s = zf.GetInputStream(entryId))
					{
						using (BinaryReader br = new BinaryReader(s))
						{
							context.Response.BinaryWrite(br.ReadBytes((int)ze.Size));
						}
					}
				}
			}
		}

		private bool ManageModified(HttpContext context, FileInfo req)
		{
			string lms = context.Request.Headers["If-Modified-Since"];
			DateTime lMod;
			if (lms != null && DateTime.TryParse(lms, out lMod) && ((int)req.LastWriteTimeUtc.Subtract(lMod.ToUniversalTime()).TotalSeconds) <= 0)
			{
				context.Response.StatusCode = 304;
				context.Response.End();
				// This should be unnecessary, but safety first.
				return true;
			}
			// SetLastModified requires a local DateTime
			context.Response.Cache.SetLastModified(req.LastWriteTime);
			context.Response.Cache.SetETag(req.LastWriteTimeUtc.Ticks.ToString());
			return false;
		}

		#endregion
	}
}
