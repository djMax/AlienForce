using System;

namespace AlienForce.Utilities.Logging
{
	/// <summary>
	/// A generic log mechanism for AlienForce.  Messages can be logged at various levels.
	/// </summary>
	public interface ILog
	{
		/// <summary>
		///     Checks if this logger is enabled for the Debug level.
		///
		/// Remarks:
		///      This function is intended to lessen the computational cost of disabled log
		///     debug statements.
		///      For some ILog interface log, when you write:
		///       log.Debug("This is entry number: " + i );
		///      You incur the cost constructing the message, string construction and concatenation
		///     in this case, regardless of whether the message is logged or not.
		///      If you are worried about speed (who isn't), then you should write:
		///       if (log.IsDebugEnabled) { log.Debug("This is entry number: " + i ); } 
		///      This way you will not incur the cost of parameter construction if debugging
		///     is disabled for log. On the other hand, if the log is debug enabled, you
		///     will incur the cost of evaluating whether the logger is debug enabled twice.
		///     Once in log4net.ILog.IsDebugEnabled and once in the log4net.ILog.Debug(System.Object).
		///      This is an insignificant overhead since evaluating a logger takes about
		///     1% of the time it takes to actually log. This is the preferred style of logging.
		///     Alternatively if your logger is available statically then the is debug enabled
		///     state can be stored in a static variable like this:
		///       private static readonly bool isDebugEnabled = log.IsDebugEnabled;
		///      Then when you come to log you can write:
		///       if (isDebugEnabled) { log.Debug("This is entry number: " + i ); }
		///      This way the debug enabled state is only queried once when the class is
		///     loaded. Using a private static readonly variable is the most efficient because
		///     it is a run time constant and can be heavily optimized by the JIT compiler.
		///      Of course if you use a static readonly variable to hold the enabled state
		///     of the logger then you cannot change the enabled state at runtime to vary
		///     the logging that is produced. You have to decide if you need absolute speed
		///     or runtime flexibility.
		/// </summary>
		bool IsDebugEnabled { get; }

		/// <summary>
		///     Checks if this logger is enabled for the Error level.
		///
		/// Remarks:
		///     For more information see log4net.ILog.IsDebugEnabled.
		/// </summary>
		bool IsErrorEnabled { get; }

		/// <summary>
		///     Checks if this logger is enabled for the Fatal level.
		///
		/// Remarks:
		///     For more information see log4net.ILog.IsDebugEnabled.
		/// </summary>
		bool IsFatalEnabled { get; }

		/// <summary>
		///     Checks if this logger is enabled for the Info level. 
		///
		/// Remarks:
		///     For more information see log4net.ILog.IsDebugEnabled.
		/// </summary>
		bool IsInfoEnabled { get; }

		/// <summary>
		///     Checks if this logger is enabled for the Warn level. 
		///
		/// Remarks:
		///     For more information see log4net.ILog.IsDebugEnabled.
		/// </summary>
		bool IsWarnEnabled { get; }

		/// <summary>
		///     Log a message object with the Debug level.
		///
		/// Parameters:
		///   message:
		///     The message object to log.
		///
		/// Remarks:
		///      This method first checks if this logger is DEBUG enabled by comparing the
		///     level of this logger with the Debug level. If this logger
		///     is DEBUG enabled, then it converts the message object (passed as parameter)
		///     to a string by invoking the appropriate log4net.ObjectRenderer.IObjectRenderer.
		///     It then proceeds to call all the registered appenders in this logger and
		///     also higher in the hierarchy depending on the value of the additivity flag.
		///     WARNING Note that passing an System.Exception to this method will print the
		///     name of the System.Exception but no stack trace. To print a stack trace use
		///     the log4net.ILog.Debug(System.Object,System.Exception) form instead.
		/// </summary>
		void Debug(object message);
		//
		/// <summary>
		///     Log a message object with the Debug level including the
		///     stack trace of the System.Exception passed as a parameter.
		///
		/// Parameters:
		///   message:
		///     The message object to log.
		///
		///   exception:
		///     The exception to log, including its stack trace.
		///
		/// Remarks:
		///      See the log4net.ILog.Debug(System.Object) form for more detailed information.
		/// </summary>
		void Debug(object message, Exception exception);
		//
		/// <summary>
		///     Logs a formatted message string with the Debug level.
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   arg0:
		///     An Object to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Debug(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void DebugFormat(string format, object arg0);

		/// <summary>
		///     Logs a formatted message string with the Debug level.
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   args:
		///     An Object array containing zero or more objects to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Debug(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void DebugFormat(string format, params object[] args);

		/// <summary>
		///     Logs a formatted message string with the Debug level.
		///
		/// Parameters:
		///   provider:
		///     An System.IFormatProvider that supplies culture-specific formatting information
		///
		///   format:
		///     A String containing zero or more format items
		///
		///   args:
		///     An Object array containing zero or more objects to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Debug(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void DebugFormat(IFormatProvider provider, string format, params object[] args);

		/// <summary>
		///     Logs a formatted message string with the Debug level.
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   arg0:
		///     An Object to format
		///
		///   arg1:
		///     An Object to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Debug(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void DebugFormat(string format, object arg0, object arg1);

		/// <summary>
		///     Logs a formatted message string with the Debug level.
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   arg0:
		///     An Object to format
		///
		///   arg1:
		///     An Object to format
		///
		///   arg2:
		///     An Object to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Debug(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void DebugFormat(string format, object arg0, object arg1, object arg2);

		/// <summary>
		///     Logs a message object with the Error level.
		///
		/// Parameters:
		///   message:
		///     The message object to log.
		///
		/// Remarks:
		///      This method first checks if this logger is ERROR enabled by comparing the
		///     level of this logger with the Error level. If this logger
		///     is ERROR enabled, then it converts the message object (passed as parameter)
		///     to a string by invoking the appropriate log4net.ObjectRenderer.IObjectRenderer.
		///     It then proceeds to call all the registered appenders in this logger and
		///     also higher in the hierarchy depending on the value of the additivity flag.
		///     WARNING Note that passing an System.Exception to this method will print the
		///     name of the System.Exception but no stack trace. To print a stack trace use
		///     the log4net.ILog.Error(System.Object,System.Exception) form instead.
		/// </summary>
		void Error(object message);
		//
		/// <summary>
		///     Log a message object with the Error level including the
		///     stack trace of the System.Exception passed as a parameter.
		///
		/// Parameters:
		///   message:
		///     The message object to log.
		///
		///   exception:
		///     The exception to log, including its stack trace.
		///
		/// Remarks:
		///      See the log4net.ILog.Error(System.Object) form for more detailed information.
		/// </summary>
		void Error(object message, Exception exception);
		//
		/// <summary>
		///     Logs a formatted message string with the Error level.
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   arg0:
		///     An Object to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Error(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void ErrorFormat(string format, object arg0);
		//
		/// <summary>
		///     Logs a formatted message string with the Error level.
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   args:
		///     An Object array containing zero or more objects to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Error(System.Object)
		///     methods instead.
		/// </summary>
		void ErrorFormat(string format, params object[] args);
		//
		/// <summary>
		///     Logs a formatted message string with the Error level.
		///
		/// Parameters:
		///   provider:
		///     An System.IFormatProvider that supplies culture-specific formatting information
		///
		///   format:
		///     A String containing zero or more format items
		///
		///   args:
		///     An Object array containing zero or more objects to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Error(System.Object)
		///     methods instead.
		/// </summary>
		void ErrorFormat(IFormatProvider provider, string format, params object[] args);

		/// <summary>
		///     Logs a formatted message string with the Error level.
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   arg0:
		///     An Object to format
		///
		///   arg1:
		///     An Object to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Error(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void ErrorFormat(string format, object arg0, object arg1);

		/// <summary>
		///     Logs a formatted message string with the Error level.
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   arg0:
		///     An Object to format
		///
		///   arg1:
		///     An Object to format
		///
		///   arg2:
		///     An Object to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Error(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void ErrorFormat(string format, object arg0, object arg1, object arg2);

		/// <summary>
		///     Log a message object with the Fatal level.
		///
		/// Parameters:
		///   message:
		///     The message object to log.
		///
		/// Remarks:
		///      This method first checks if this logger is FATAL enabled by comparing the
		///     level of this logger with the Fatal level. If this logger
		///     is FATAL enabled, then it converts the message object (passed as parameter)
		///     to a string by invoking the appropriate log4net.ObjectRenderer.IObjectRenderer.
		///     It then proceeds to call all the registered appenders in this logger and
		///     also higher in the hierarchy depending on the value of the additivity flag.
		///     WARNING Note that passing an System.Exception to this method will print the
		///     name of the System.Exception but no stack trace. To print a stack trace use
		///     the log4net.ILog.Fatal(System.Object,System.Exception) form instead.
		/// </summary>
		void Fatal(object message);

		/// <summary>
		///     Log a message object with the Fatal level including the
		///     stack trace of the System.Exception passed as a parameter.
		///
		/// Parameters:
		///   message:
		///     The message object to log.
		///
		///   exception:
		///     The exception to log, including its stack trace.
		///
		/// Remarks:
		///      See the log4net.ILog.Fatal(System.Object) form for more detailed information.
		/// </summary>
		void Fatal(object message, Exception exception);

		/// <summary>
		///     Logs a formatted message string with the Fatal level.
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   arg0:
		///     An Object to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Fatal(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void FatalFormat(string format, object arg0);

		/// <summary>
		///     Logs a formatted message string with the Fatal level.
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   args:
		///     An Object array containing zero or more objects to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Fatal(System.Object)
		///     methods instead.
		/// </summary>
		void FatalFormat(string format, params object[] args);

		/// <summary>
		///     Logs a formatted message string with the Fatal level.
		///
		/// Parameters:
		///   provider:
		///     An System.IFormatProvider that supplies culture-specific formatting information
		///
		///   format:
		///     A String containing zero or more format items
		///
		///   args:
		///     An Object array containing zero or more objects to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Fatal(System.Object)
		///     methods instead.
		/// </summary>
		void FatalFormat(IFormatProvider provider, string format, params object[] args);

		/// <summary>
		///     Logs a formatted message string with the Fatal level.
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   arg0:
		///     An Object to format
		///
		///   arg1:
		///     An Object to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Fatal(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void FatalFormat(string format, object arg0, object arg1);

		/// <summary>
		///     Logs a formatted message string with the Fatal level.
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   arg0:
		///     An Object to format
		///
		///   arg1:
		///     An Object to format
		///
		///   arg2:
		///     An Object to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Fatal(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void FatalFormat(string format, object arg0, object arg1, object arg2);
		//
		/// <summary>
		///     Logs a message object with the Info level.
		///
		/// Parameters:
		///   message:
		///     The message object to log.
		///
		/// Remarks:
		///      This method first checks if this logger is INFO enabled by comparing the
		///     level of this logger with the Info level. If this logger
		///     is INFO enabled, then it converts the message object (passed as parameter)
		///     to a string by invoking the appropriate log4net.ObjectRenderer.IObjectRenderer.
		///     It then proceeds to call all the registered appenders in this logger and
		///     also higher in the hierarchy depending on the value of the additivity flag.
		///     WARNING Note that passing an System.Exception to this method will print the
		///     name of the System.Exception but no stack trace. To print a stack trace use
		///     the log4net.ILog.Info(System.Object,System.Exception) form instead.
		/// </summary>
		void Info(object message);
		//
		/// <summary>
		///     Logs a message object with the INFO level including the stack trace of the
		///     System.Exception passed as a parameter.
		///
		/// Parameters:
		///   message:
		///     The message object to log.
		///
		///   exception:
		///     The exception to log, including its stack trace.
		///
		/// Remarks:
		///      See the log4net.ILog.Info(System.Object) form for more detailed information.
		/// </summary>
		void Info(object message, Exception exception);
		//
		/// <summary>
		///     Logs a formatted message string with the Info level. 
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   arg0:
		///     An Object to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Info(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void InfoFormat(string format, object arg0);
		//
		/// <summary>
		///     Logs a formatted message string with the Info level. 
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   args:
		///     An Object array containing zero or more objects to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Info(System.Object)
		///     methods instead.
		/// </summary>
		void InfoFormat(string format, params object[] args);
		//
		/// <summary>
		///     Logs a formatted message string with the Info level. 
		///
		/// Parameters:
		///   provider:
		///     An System.IFormatProvider that supplies culture-specific formatting information
		///
		///   format:
		///     A String containing zero or more format items
		///
		///   args:
		///     An Object array containing zero or more objects to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Info(System.Object)
		///     methods instead.
		/// </summary>
		void InfoFormat(IFormatProvider provider, string format, params object[] args);
		//
		/// <summary>
		///     Logs a formatted message string with the Info level. 
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   arg0:
		///     An Object to format
		///
		///   arg1:
		///     An Object to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Info(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void InfoFormat(string format, object arg0, object arg1);
		//
		/// <summary>
		///     Logs a formatted message string with the Info level. 
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   arg0:
		///     An Object to format
		///
		///   arg1:
		///     An Object to format
		///
		///   arg2:
		///     An Object to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Info(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void InfoFormat(string format, object arg0, object arg1, object arg2);
		//
		/// <summary>
		///     Log a message object with the Warn level.
		///
		/// Parameters:
		///   message:
		///     The message object to log.
		///
		/// Remarks:
		///      This method first checks if this logger is WARN enabled by comparing the
		///     level of this logger with the Warn level. If this logger
		///     is WARN enabled, then it converts the message object (passed as parameter)
		///     to a string by invoking the appropriate log4net.ObjectRenderer.IObjectRenderer.
		///     It then proceeds to call all the registered appenders in this logger and
		///     also higher in the hierarchy depending on the value of the additivity flag.
		///     WARNING Note that passing an System.Exception to this method will print the
		///     name of the System.Exception but no stack trace. To print a stack trace use
		///     the log4net.ILog.Warn(System.Object,System.Exception) form instead.
		/// </summary>
		void Warn(object message);
		//
		/// <summary>
		///     Log a message object with the Warn level including the
		///     stack trace of the System.Exception passed as a parameter.
		///
		/// Parameters:
		///   message:
		///     The message object to log.
		///
		///   exception:
		///     The exception to log, including its stack trace.
		///
		/// Remarks:
		///      See the log4net.ILog.Warn(System.Object) form for more detailed information.
		/// </summary>
		void Warn(object message, Exception exception);

		/// <summary>
		///     Logs a formatted message string with the Warn level. 
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   arg0:
		///     An Object to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Warn(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void WarnFormat(string format, object arg0);

		/// <summary>
		///     Logs a formatted message string with the Warn level. 
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   args:
		///     An Object array containing zero or more objects to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Warn(System.Object)
		///     methods instead.
		/// </summary>
		void WarnFormat(string format, params object[] args);

		/// <summary>
		///     Logs a formatted message string with the Warn level. 
		///
		/// Parameters:
		///   provider:
		///     An System.IFormatProvider that supplies culture-specific formatting information
		///
		///   format:
		///     A String containing zero or more format items
		///
		///   args:
		///     An Object array containing zero or more objects to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Warn(System.Object)
		///     methods instead.
		/// </summary>
		void WarnFormat(IFormatProvider provider, string format, params object[] args);

		/// <summary>
		///     Logs a formatted message string with the Warn level. 
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   arg0:
		///     An Object to format
		///
		///   arg1:
		///     An Object to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Warn(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void WarnFormat(string format, object arg0, object arg1);
		//
		/// <summary>
		///     Logs a formatted message string with the Warn level. 
		///
		/// Parameters:
		///   format:
		///     A String containing zero or more format items
		///
		///   arg0:
		///     An Object to format
		///
		///   arg1:
		///     An Object to format
		///
		///   arg2:
		///     An Object to format
		///
		/// Remarks:
		///      The message is formatted using the String.Format method. See System.String.Format(System.String,System.Object[])
		///     for details of the syntax of the format string and the behavior of the formatting.
		///      This method does not take an System.Exception object to include in the log
		///     event. To pass an System.Exception use one of the log4net.ILog.Warn(System.Object,System.Exception)
		///     methods instead.
		/// </summary>
		void WarnFormat(string format, object arg0, object arg1, object arg2);

	}
}
