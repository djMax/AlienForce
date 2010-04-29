using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;

namespace Launcher
{
	class Launcher
	{
		private static ILog mLog = LogManager.GetLogger(typeof(Launcher));

		static int Main(string[] args)
		{
			log4net.Config.XmlConfigurator.Configure();

			string useWindow = System.Configuration.ConfigurationManager.AppSettings["LauncherWindow"];

			if (useWindow == "true")
			{
				try
				{
					// This gives this process a title in the debugger.
					System.Windows.Forms.Form mf = new System.Windows.Forms.Form();
					mf.Height = 0;
					mf.Width = 0;
					mf.ShowInTaskbar = false;
					mf.Text = String.Join(" ", args);
					mf.WindowState = System.Windows.Forms.FormWindowState.Minimized;
					mf.Show();
				}
				catch (Exception)
				{
				}
			}

			if (args.Length < 2)
			{
				if (mLog.IsErrorEnabled)
					mLog.Error("Usage: Launcher.exe <AssemblyLib> <Class> <other args>");
				else
					Console.WriteLine("Usage: Launcher.exe <AssemblyLib> <Class> <other args>");
				return 0;
			}
			Assembly a = null;
			bool quietFail = false;
			int argBase = 0;
			if (args[0] == "-q")
			{
				quietFail = true;
				argBase++;
			}
			try
			{
				a = Assembly.LoadFrom(args[0]);
			}
			catch (Exception e)
			{
				if (quietFail)
				{
					return 0;
				}
				if (mLog.IsErrorEnabled)
					mLog.Error("Can't find assembly.", e);
				else
					Console.WriteLine("Can't find assembly: " + e.Message);
				return -1;
			}
			Type t = null;
			try
			{
				t = a.GetType(args[1], true, true);
			}
			catch (Exception)
			{
				try
				{
					Type[] ts = a.GetTypes();
					foreach (Type tt in ts)
					{
						if (tt.Name == args[1])
						{
							t = tt;
							break;
						}
					}
				}
				catch (ReflectionTypeLoadException rtle)
				{
					if (quietFail)
					{
						return 0;
					}
					foreach (Exception e in rtle.LoaderExceptions)
					{
						ShowException(e);
					}
					throw;
				}
			}

			if (t == null)
			{
				if (quietFail)
				{
					return 0;
				}
				if (mLog.IsErrorEnabled)
					mLog.Error("Can't find requested type.");
				else
					Console.WriteLine("Can't find requested type.");
				return -1;
			}

			try
			{
				int retCode = 0;
				MethodInfo mi =
					t.GetMethod("Main", BindingFlags.Static | BindingFlags.Public);
				if (mi == null)
				{
					if (mLog.IsErrorEnabled)
						mLog.Error(String.Format("{0} has no Main() method defined.", args[1]));
					else
						Console.WriteLine("{0} has no Main() method defined.", args[1]);
				}
				else
				{
					string[] argsNew = new string[args.Length - 2];
					Array.Copy(args, 2, argsNew, 0, argsNew.Length);
					object res = mi.Invoke(null, new object[] { argsNew });
					if (res is int)
					{
						retCode = (int)res;
					}
				}
				return retCode;
			}
			catch (Exception ex)
			{
				if (quietFail)
				{
					return 0;
				}
				ShowException(ex);
				return -1;
			}
		}

		private static void ShowException(Exception e)
		{
			if (e != null)
			{
				ShowException(e.InnerException);
				if (mLog.IsErrorEnabled)
				{
					mLog.Error(e.GetType().Name + " Exception!", e);
				}
				else
				{
					Console.WriteLine("{0} {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace);
				}
			}
		}
	}
}