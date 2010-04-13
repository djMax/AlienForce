using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using AlienForce.Utilities.Logging;
using System.Configuration;
using System.Globalization;
using System.Threading;
using System.Web;
using System.Diagnostics;

namespace AlienForce.Utilities.Db
{
	/// <summary>
	/// An abstract connection to a database.  This "intermediate" class handles much of the annoying parts of implementing a
	/// custom db connection in the BenTen framework.  We can track opens and closes more carefully, control the *real* ADO
	/// open/close behavior separately from the application open/close, log, etc.
	/// </summary>
	public abstract class DbConnectionBase : IDisposable, IDbConnection
	{
		private class CommandLog { }

		#region Fields

		/// <summary>
		/// Set this value to greater than or equal to 0 to enable "long running command logging" which will
		/// write any commands taking longer than this time to the log.
		/// </summary>
		public static int LongRunningCommandLogThresholdSeconds = -1; // by default don't time statements.
		private static ILog mLog = LogFramework.Framework.GetLogger(typeof(DbConnectionBase));
		internal static ILog mCommandLog = LogFramework.Framework.GetLogger(typeof(CommandLog));
		private static int mDefaultCommandTimeout = -1; // by default, don't manipulate it.
		internal static bool mTrackCallsPerPage;

		private const string cCallInfoKey = "AlienForce.Database.PageCallInfo";

		private bool _IsReadOnly = true;
		private bool _CloseOnClose = true;
		private int _CommandTimeout;
		private bool _CheckedOut;
		private int _NumCalls;
		private bool _Open;
		private static int _InstanceCount;
		private static int _OpenInstanceCount;
		private List<string> _LogEntries;

		/// <summary>
		/// The underlying database connection
		/// </summary>
		protected IDbConnection mConnection;
		/// <summary>
		/// The active transaction, if any
		/// </summary>
		protected IDbTransaction mTransaction;
		/// <summary>
		/// Metadata about the connection
		/// </summary>
		protected DbConnectionInfo mConnectionInfo;
		#endregion

		#region Db-specific methods
		/// <summary>
		/// Creates and returns a Command object associated with the connection.
		/// </summary>
		/// <returns>
		/// A Command object associated with the connection.
		/// </returns>
		public abstract IDbCommand CreateCommand();
		/// <summary>
		/// Duplicates this instance.
		/// </summary>
		/// <returns></returns>
		public abstract DbConnectionBase Duplicate();
		/// <summary>
		/// Gets the writeable version of a database (usually to convert a read-only to a read/write).
		/// </summary>
		/// <returns></returns>
		public abstract DbConnectionBase GetWriteableDatabase();
		/// <summary>
		/// Creates the adapter.
		/// </summary>
		/// <param name="command">The command.</param>
		/// <returns></returns>
		public abstract IDbDataAdapter CreateAdapter(IDbCommand command);
		#endregion

		static DbConnectionBase()
		{
			string cfg = ConfigurationManager.AppSettings["AlienForce.DbConnection.CommandTimeout"];
			if (cfg != null)
			{
				try
				{
					mDefaultCommandTimeout = Convert.ToInt32(cfg, CultureInfo.InvariantCulture);
					mLog.Info(String.Format(CultureInfo.CurrentCulture, Messages.DbConn_SettingDefaultTimeout, mDefaultCommandTimeout));
				}
				catch (FormatException ex)
				{
					mLog.Error(String.Format(CultureInfo.CurrentCulture, Messages.InvalidDbConnTimeout, cfg), ex);
				}
			}
			cfg = ConfigurationManager.AppSettings["DbConnection.TrackCallsPerPage"];
			if (cfg != null)
			{
				bool newSet;
				if (bool.TryParse(cfg, out newSet))
				{
					mTrackCallsPerPage = newSet;
				}
				else
				{
					mLog.Warn(String.Format("Invalid configuration for application setting 'DbConnection.TrackCallsPerPage' ({0})", cfg));
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:DbConnectionBase"/> class.
		/// </summary>
		/// <param name="db">The db.</param>
		protected DbConnectionBase(IDbConnection db)
			: this(db, false)
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:DbConnectionBase"/> class.
		/// </summary>
		/// <param name="db">The db.</param>
		/// <param name="info">The info.</param>
		protected DbConnectionBase(IDbConnection db, DbConnectionInfo info)
			: this(db, info.IsReadOnly)
		{
			mConnectionInfo = info;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:DbConnectionBase"/> class.
		/// </summary>
		/// <param name="db">The db.</param>
		/// <param name="isReadOnly">if set to <c>true</c> [is read only].</param>
		protected DbConnectionBase(IDbConnection db, bool isReadOnly)
		{
			if (db == null)
				throw new ArgumentNullException("db");

			Log(Messages.DbConn_New);

			_IsReadOnly = isReadOnly;
			_CommandTimeout = mDefaultCommandTimeout;
			Interlocked.Increment(ref _InstanceCount);
			mConnection = db;
			_Open = (mConnection.State == ConnectionState.Open);
			if (_Open)
			{
				Interlocked.Increment(ref _OpenInstanceCount);
			}
			_CheckedOut = false;
		}

		#region Properties
		/// <summary>
		/// The number of outstanding DbConnectionBase instances.
		/// </summary>
		public static int InstanceCount { get { return _InstanceCount; } }

		/// <summary>
		/// The number of outstanding DbConnectionBase instances with open connections.
		/// </summary>
		public static int OpenInstanceCount { get { return _OpenInstanceCount; } }

		/// <summary>
		/// The command timeout to set on commands executed via ExecuteReader, ExecuteNonQuery,
		/// and ExecuteScalar on THIS DbConnection.  If you call them on the underlying SQL
		/// conn, you would have to set it manually since we don't get the chance.  Setting this
		/// value to -1 means we'll leave the timeout alone.
		/// </summary>
		public virtual int CommandTimeout
		{
			get { return _CommandTimeout; }
			set { _CommandTimeout = value; }
		}

		/// <summary>
		/// If CloseOnClose is true, the underlying IDbConnection is really closed
		/// when you call close.  Otherwise it only happens on IDispose
		/// </summary>
		public virtual bool CloseOnClose
		{
			get { return _CloseOnClose; }
			set { _CloseOnClose = value; }
		}

		/// <summary>
		/// Return true if the database connection is currently "checked out"
		/// by someone.  This means a function or thread has called Open but
		/// not yet called Close.
		/// </summary>
		public bool IsCheckedOut
		{
			get { return _CheckedOut; }
		}

		/// <summary>
		/// Returns true if the database is read-only.
		/// </summary>
		/// <remarks>Using a command that requires a writeable database with a read-only database connection will result in a runtime error.</remarks>
		public bool IsReadOnly
		{
			get { return _IsReadOnly; }
		}

		/// <summary>
		/// Return the number of times Open was called in the lifetime
		/// of this object.
		/// </summary>
		public int NumberOfOpens { get { return _NumCalls; } }

		#endregion

		#region Connection state management
		/// <summary>
		/// Call checkout to get a hold of the contained connection object.
		/// This behaves just like a single IDbConnection, in that you can only
		/// execute one thing at a time...
		/// </summary>
		/// <returns></returns>
		public void Open()
		{
			if (!_Open)
			{
				Log(Messages.DbConn_Open);
				mConnection.Open();
				Interlocked.Increment(ref _OpenInstanceCount);
				Log(Messages.DbConn_Opened);

				_Open = true;
			}
			else
			{
				Log(Messages.DbConn_ReOpen);

				if (mConnection.State != ConnectionState.Open)
				{
					Log(Messages.DbConn_Broken);
					mConnection.Open();
				}
			}
			_CheckedOut = true;
			_NumCalls++;
		}

		/// <summary>
		/// Call Close when you are done using the connection, it
		/// will decide whether to close the underlying db connection or
		/// not.
		/// </summary>
		public void Close()
		{
			if (_CloseOnClose && mConnection.State == ConnectionState.Open)
			{
				if (mTransaction != null)
				{
					// bail bail bail
					Log(Messages.AttemptToCloseConnWithTransaction);
					try
					{
						mTransaction.Rollback();
					}
					catch
					{
					}
				}
				Log(Messages.DbConn_Close);
				Interlocked.Decrement(ref _OpenInstanceCount);
				mConnection.Close();
				_Open = false;
			}
			else if (mLog.IsInfoEnabled || mLog.IsDebugEnabled)
			{
				Log(Messages.DbConn_PsuedoClose);
			}
			_CheckedOut = false;
		}
		#endregion

		#region Execution functions
		/// <summary>
		/// Construct a new data reader, don't forget to dispose of it.
		/// </summary>
		/// <param name="sc"></param>
		/// <returns></returns>
		public DbConnectionReader ExecuteReader(IDbCommand sc)
		{
			CheckReadOnly(sc);
			return new DbConnectionReader(this, sc);
		}

		/// <summary>
		/// Construct a new data reader, don't forget to dispose of it.
		/// </summary>
		/// <param name="sc"></param>
		/// <returns></returns>
		public DbConnectionReader ExecuteReader(DbCommandBase sc)
		{
			CheckReadOnly(sc);
			return new DbConnectionReader(this, sc);
		}

		/// <summary>
		/// Helper method to open the connection, exec the command, and
		/// close even if there's an exception.
		/// </summary>
		/// <param name="sc"></param>
		public object ExecuteScalar(IDbCommand sc)
		{
			CheckReadOnly(sc);
			return ExecuteScalarCommand(sc);
		}

		/// <summary>
		/// Helper method to open the connection, exec the command, and
		/// close even if there's an exception.
		/// </summary>
		/// <param name="sc"></param>
		public object ExecuteScalar(DbCommandBase sc)
		{
			CheckReadOnly(sc);
			return ExecuteScalarCommand(sc);
		}

		private object ExecuteScalarCommand(IDbCommand sc)
		{
			if (sc == null)
			{
				throw new ArgumentNullException("sc");
			}
			Open();
			Stopwatch sw = null;
			if (mTrackCallsPerPage)
			{
				sw = Stopwatch.StartNew();
			}
			try
			{
				IDbTransaction sqt = (IDbTransaction)CurrentTransaction;
				if (sqt != null)
				{
					sc.Transaction = sqt;
				}
				if (_CommandTimeout != -1)
				{
					sc.CommandTimeout = _CommandTimeout;
				}
				DateTime start = DateTime.UtcNow;
				object ret = sc.ExecuteScalar();
				if (LongRunningCommandLogThresholdSeconds >= 0 && mCommandLog.IsWarnEnabled)
				{
					TimeSpan diff = DateTime.UtcNow.Subtract(start);
					if (diff.TotalSeconds > LongRunningCommandLogThresholdSeconds)
					{
						mCommandLog.Warn(String.Format(null, "A database command took {0} milliseconds to run: {1}", diff.TotalMilliseconds, sc.CommandText));
					}
				}
				return ret;
			}
			finally
			{
				if (!InTransaction)
				{
					Close();
				}
				if (mTrackCallsPerPage)
				{
					sw.Stop();
					DbConnectionBase.AddCallToPage(HttpContext.Current, sw);
				}
			}
		}


		/// <summary>
		/// Helper method to open the connection, exec the command, and
		/// close even if there's an exception.
		/// </summary>
		/// <param name="sc"></param>
		public int ExecuteNonQuery(IDbCommand sc)
		{
			CheckReadOnly(sc);
			return ExecuteNonQueryCommand(sc);
		}

		/// <summary>
		/// Helper method to open the connection, exec the command, and
		/// close even if there's an exception.
		/// </summary>
		/// <param name="sc"></param>
		public int ExecuteNonQuery(DbCommandBase sc)
		{
			CheckReadOnly(sc);
			return ExecuteNonQueryCommand(sc);
		}

		/// <summary>
		/// Helper method to open the connection, exec the command, and
		/// close even if there's an exception.
		/// </summary>
		/// <param name="sc"></param>
		private int ExecuteNonQueryCommand(IDbCommand sc)
		{
			if (sc == null)
			{
				throw new ArgumentNullException("sc");
			}
			Open();
			Stopwatch sw = null;
			if (mTrackCallsPerPage)
			{
				sw = Stopwatch.StartNew();
			}
			try
			{
				IDbTransaction sqt = CurrentTransaction;
				if (sqt != null)
				{
					sc.Transaction = sqt;
				}
				if (_CommandTimeout != -1)
				{
					sc.CommandTimeout = _CommandTimeout;
				}
				DateTime start = DateTime.UtcNow;
				int result = sc.ExecuteNonQuery();
				if (LongRunningCommandLogThresholdSeconds >= 0 && mCommandLog.IsWarnEnabled)
				{
					TimeSpan diff = DateTime.UtcNow.Subtract(start);
					if (diff.TotalSeconds > LongRunningCommandLogThresholdSeconds)
					{
						mCommandLog.Warn(String.Format(null, "A database command took {0} milliseconds to run: {1}", diff.TotalMilliseconds, sc.CommandText));
					}
				}
				return result;
			}
			finally
			{
				if (!InTransaction)
				{
					Close();
				}
				if (mTrackCallsPerPage)
				{
					sw.Stop();
					DbConnectionBase.AddCallToPage(HttpContext.Current, sw);
				}
			}
		}
		#endregion

		#region Transaction Handling

		/// <summary>
		/// Start a transaction and put it on the stack.
		/// </summary>
		/// <returns></returns>
		public virtual IDbTransaction BeginTransaction()
		{
			return BeginTransaction(IsolationLevel.Unspecified);
		}

		/// <summary>
		/// Start a transaction and put it on the stack.
		/// </summary>
		/// <returns></returns>
		public virtual IDbTransaction BeginTransaction(IsolationLevel il)
		{
			if (mTransaction != null)
			{
				throw new DataException("Nested transactions are not supported.");
			}
			mTransaction = mConnection.BeginTransaction(il);
			return mTransaction;
		}

		/// <summary>
		/// Commit the top transaction
		/// </summary>
		public virtual void CommitTransaction()
		{
			if (mTransaction == null)
			{
				throw new DataException("Cannot commit non-existent transaction.");
			}
			mTransaction.Commit();
			mTransaction = null;
		}

		/// <summary>
		/// Rollback the top transaction
		/// </summary>
		[Obsolete]
		public virtual void RollbackTransaction()
		{
			if (mTransaction == null)
			{
				throw new DataException("Cannot rollback non-existent transaction.");
			}
			mTransaction.Rollback();
			mTransaction = null;
		}

		/// <summary>
		/// Get the currently active transaction, if any.
		/// </summary>
		public IDbTransaction CurrentTransaction
		{
			get
			{
				return mTransaction;
			}
		}

		/// <summary>
		/// True if the connection is currently in a transaction
		/// </summary>
		public bool InTransaction
		{
			get
			{
				return mTransaction != null;
			}
		}
		#endregion

		#region IDisposable Members
		/// <summary>
		/// Close the database connection.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Close any resources you need to close also.
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			try
			{
				if (mConnection != null)
				{
					if (mLog.IsDebugEnabled || mLog.IsInfoEnabled)
					{
						string msg = String.Format(CultureInfo.CurrentCulture, Messages.DbConn_Dispose, _CheckedOut, _InstanceCount, _OpenInstanceCount);
						Log(msg, false);
					}
					Interlocked.Decrement(ref _InstanceCount);
					if (_Open)
					{
						_Open = false;
						Interlocked.Decrement(ref _OpenInstanceCount);
					}
					mConnection.Dispose();
				}
				else if (mLog.IsDebugEnabled || mLog.IsInfoEnabled)
				{
					string msg = String.Format(CultureInfo.CurrentCulture, Messages.DbConn_EmptyDispose, _CheckedOut, _InstanceCount, _OpenInstanceCount);
					Log(msg, false);
				}
				mConnection = null;
			}
			catch { }
			if (_LogEntries != null && mLog.IsInfoEnabled)
			{
				mLog.Info(String.Join("\n----\n", _LogEntries.ToArray()));
			}
		}
		#endregion

		private void CheckReadOnly(IDbCommand cmd)
		{
			CheckReadOnly(cmd, false);
		}

		private void CheckReadOnly(DbCommandBase cmd)
		{
			CheckReadOnly(cmd, cmd.IsSafeForReadOnly);
		}

		private void CheckReadOnly(IDbCommand cmd, bool isSafeForReadOnlyCommand)
		{
			if (_IsReadOnly && !isSafeForReadOnlyCommand)
			{
				throw new ReadOnlyException(cmd.CommandText + " is not safe for read-only databases.");
			}
		}

		/// <summary>
		/// Logs the specified string.
		/// </summary>
		/// <param name="formatString">The format string.</param>
		protected internal void Log(string formatString)
		{
			Log(formatString, true);
		}

		/// <summary>
		/// Logs the specified string.
		/// </summary>
		/// <param name="formatString">The format string.</param>
		/// <param name="format">if set to <c>true</c> [format].</param>
		protected internal void Log(string formatString, bool format)
		{
			if (mLog.IsDebugEnabled)
			{
				string msg = format ? String.Format(CultureInfo.CurrentCulture, formatString, _InstanceCount, _OpenInstanceCount) : formatString;
				mLog.Debug(msg);
			}
			else if (mLog.IsInfoEnabled)
			{
				string msg = format ? String.Format(CultureInfo.CurrentCulture, formatString, _InstanceCount, _OpenInstanceCount) : formatString;
				if (_LogEntries == null)
				{
					_LogEntries = new List<string>();
					if (HttpContext.Current != null)
						_LogEntries.Add(HttpContext.Current.Request.Url.ToString());
				}
				_LogEntries.Add(msg);
			}
		}

		#region IDbConnection Members

		/// <summary>
		/// Changes the current database for an open Connection object.
		/// </summary>
		/// <param name="databaseName">The name of the database to use in place of the current database.</param>
		public void ChangeDatabase(string databaseName)
		{
			mConnection.ChangeDatabase(databaseName);
		}

		/// <summary>
		/// Gets or sets the string used to open a database.
		/// </summary>
		/// <value></value>
		/// <returns>A string containing connection settings.</returns>
		public string ConnectionString
		{
			get
			{
				return mConnection.ConnectionString;
			}
			set
			{
				throw new NotImplementedException(Messages.DbConnectionBase_DoNotSetConnectionString);
			}
		}

		/// <summary>
		/// Gets the time to wait while trying to establish a connection before terminating the attempt and generating an error.
		/// </summary>
		/// <value></value>
		/// <returns>The time (in seconds) to wait for a connection to open. The default value is 15 seconds.</returns>
		public int ConnectionTimeout
		{
			get { return mConnection.ConnectionTimeout; }
		}

		/// <summary>
		/// Gets the name of the current database or the database to be used after a connection is opened.
		/// </summary>
		/// <value></value>
		/// <returns>The name of the current database or the name of the database to be used once a connection is open. The default value is an empty string.</returns>
		public string Database
		{
			get { return mConnection.Database; }
		}

		/// <summary>
		/// Gets the current state of the connection.
		/// </summary>
		/// <value></value>
		/// <returns>One of the <see cref="T:System.Data.ConnectionState"></see> values.</returns>
		public ConnectionState State
		{
			get { return mConnection.State; }
		}

		#endregion

		/// <summary>
		/// Gets the database call info, if any, in the given HttpContext.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		protected static internal PageCallInfo GetCallInfo(HttpContext context)
		{
			return context.Items[cCallInfoKey] as PageCallInfo;
		}

		/// <summary>
		/// Adds the call timing data to the page context.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="watch">The watch.</param>
		protected static internal void AddCallToPage(HttpContext context, Stopwatch watch)
		{
			if (context != null)
			{
				DbConnectionBase.PageCallInfo pci = context.Items[cCallInfoKey] as DbConnectionBase.PageCallInfo;
				if (pci == null)
				{
					context.Items[cCallInfoKey] = pci = new DbConnectionBase.PageCallInfo();
				}
				pci.NumCalls++;
				pci.TotalTime += watch.ElapsedTicks;
			}
		}

		/// <summary>
		/// Simple class to store the information about calls to the db in the context of a web page.
		/// </summary>
		protected internal class PageCallInfo
		{
			/// <summary>
			/// The number of calls to the db
			/// </summary>
			public int NumCalls;
			/// <summary>
			/// The amount of time spent in those calls.
			/// </summary>
			public long TotalTime;
		}
	}
}
