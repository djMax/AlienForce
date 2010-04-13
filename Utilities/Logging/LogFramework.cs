using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;

namespace AlienForce.Utilities.Logging
{
	/// <summary>
	/// The LogFramework class provides a centralized management point for AlienForce logging.
	/// By default, we provide log4net.  If for some reason this doesn't work, you can set
	/// the Default LogFramework to one of your choosing and implement Initialize and GetLogger.
	/// </summary>
	public class LogFramework
	{
		/// <summary>
		/// The framework that will be used by all components to get loggers.
		/// By default, the version of LogFramework created in the static constructor of this class
		/// will implement log4net logging.
		/// </summary>
		public static LogFramework Framework = new LogFramework();

		/// <summary>
		/// The name of the log4net config file.  If null, we will use the application configuration file.
		/// </summary>
		public static string LogConfigurationFile = "log4net.config";

		private bool _Initialized;

		/// <summary>
		/// Initializes this LogFramework.  This method should only be called once at the start of the application.
		/// </summary>
		public virtual void Initialize()
		{
			if (LogConfigurationFile != null)
			{
				string path = LogConfigurationFile;
				if (HttpContext.Current != null)
				{
					path = HttpContext.Current.Server.MapPath("~");
					path = Path.Combine(path, LogConfigurationFile);
				}
				log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(path));
			}
			else
			{
				log4net.Config.XmlConfigurator.Configure();
			}
			_Initialized = true;
		}

		/// <summary>
		/// True if this instance has already been initialized
		/// </summary>
		public virtual bool Initialized
		{
			get { return _Initialized; }
		}

		/// <summary>
		/// Gets an ILog interface for log messages from a given source (the name).
		/// </summary>
		/// <param name="name">The name of the component or area that will write messages to the returned log.</param>
		/// <returns></returns>
		public virtual ILog GetLogger(string name)
		{
			return new Log4NetLogger(log4net.LogManager.GetLogger(name));
		}

		/// <summary>
		/// Gets an ILog interface for log messages from a given source (the type).
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public virtual ILog GetLogger(Type type)
		{
			return new Log4NetLogger(log4net.LogManager.GetLogger(type));
		}
	}
}
