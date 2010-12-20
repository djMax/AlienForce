using System;

namespace AlienForce.Utilities.Logging
{
	/// <summary>
	/// A mapping of BenTen logging to log4net
	/// </summary>
	public class Log4NetLogger : ILog
	{
		private readonly log4net.ILog _Log;

		/// <summary>
		/// Construct a BenTen logger for a log4net logger
		/// </summary>
		/// <param name="log"></param>
		public Log4NetLogger(log4net.ILog log)
		{
			_Log = log;
		}

		#region ILog Members

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public bool IsDebugEnabled
		{
			get { return _Log.IsDebugEnabled; }
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public bool IsErrorEnabled
		{
			get { return _Log.IsErrorEnabled; }
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public bool IsFatalEnabled
		{
			get { return _Log.IsFatalEnabled; }
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public bool IsInfoEnabled
		{
			get { return _Log.IsInfoEnabled; }
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public bool IsWarnEnabled
		{
			get { return _Log.IsWarnEnabled; }
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void Debug(object message)
		{
			_Log.Debug(message);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void Debug(object message, Exception exception)
		{
			_Log.Debug(message, exception);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void DebugFormat(string format, object arg0)
		{
			_Log.DebugFormat(format, arg0);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void DebugFormat(string format, params object[] args)
		{
			_Log.DebugFormat(format, args);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void DebugFormat(IFormatProvider provider, string format, params object[] args)
		{
			_Log.DebugFormat(provider, format, args);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void DebugFormat(string format, object arg0, object arg1)
		{
			_Log.DebugFormat(format, arg0, arg1);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void DebugFormat(string format, object arg0, object arg1, object arg2)
		{
			_Log.DebugFormat(format, arg0, arg1, arg2);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void Error(object message)
		{
			_Log.Error(message);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void Error(object message, Exception exception)
		{
			_Log.Error(message, exception);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void ErrorFormat(string format, object arg0)
		{
			_Log.ErrorFormat(format, arg0);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void ErrorFormat(string format, params object[] args)
		{
			_Log.ErrorFormat(format, args);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
		{
			_Log.ErrorFormat(provider, format, args);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void ErrorFormat(string format, object arg0, object arg1)
		{
			_Log.ErrorFormat(format, arg0, arg1);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void ErrorFormat(string format, object arg0, object arg1, object arg2)
		{
			_Log.ErrorFormat(format, arg0, arg1, arg2);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void Fatal(object message)
		{
			_Log.Fatal(message);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void Fatal(object message, Exception exception)
		{
			_Log.Fatal(message, exception);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void FatalFormat(string format, object arg0)
		{
			_Log.FatalFormat(format, arg0);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void FatalFormat(string format, params object[] args)
		{
			_Log.FatalFormat(format, args);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void FatalFormat(IFormatProvider provider, string format, params object[] args)
		{
			_Log.FatalFormat(provider, format, args);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void FatalFormat(string format, object arg0, object arg1)
		{
			_Log.FatalFormat(format, arg0, arg1);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void FatalFormat(string format, object arg0, object arg1, object arg2)
		{
			_Log.FatalFormat(format, arg0, arg1, arg2);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void Info(object message)
		{
			_Log.Info(message);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void Info(object message, Exception exception)
		{
			_Log.Info(message, exception);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void InfoFormat(string format, object arg0)
		{
			_Log.InfoFormat(format, arg0);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void InfoFormat(string format, params object[] args)
		{
			_Log.InfoFormat(format, args);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void InfoFormat(IFormatProvider provider, string format, params object[] args)
		{
			_Log.InfoFormat(provider, format, args);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void InfoFormat(string format, object arg0, object arg1)
		{
			_Log.InfoFormat(format, arg0, arg1);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void InfoFormat(string format, object arg0, object arg1, object arg2)
		{
			_Log.InfoFormat(format, arg0, arg1, arg2);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void Warn(object message)
		{
			_Log.Warn(message);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void Warn(object message, Exception exception)
		{
			_Log.Warn(message, exception);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void WarnFormat(string format, object arg0)
		{
			_Log.WarnFormat(format, arg0);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void WarnFormat(string format, params object[] args)
		{
			_Log.WarnFormat(format, args);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void WarnFormat(IFormatProvider provider, string format, params object[] args)
		{
			_Log.WarnFormat(provider, format, args);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void WarnFormat(string format, object arg0, object arg1)
		{
			_Log.WarnFormat(format, arg0, arg1);
		}

		/// <summary>
		/// <see cref="ILog"/>
		/// </summary>
		public void WarnFormat(string format, object arg0, object arg1, object arg2)
		{
			_Log.WarnFormat(format, arg0, arg1, arg2);
		}

		#endregion
	}
}
