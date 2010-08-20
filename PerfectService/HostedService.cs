using System;
using System.IO;
using System.Reflection;
using System.Threading;
using log4net;

namespace PerfectService
{
	/// <summary>
	/// A hosted service dynamically loads an assembly in a separate app domain.
	/// </summary>
	internal class HostedService
	{
		private ILog mLog = LogManager.GetLogger(typeof(HostedService));

		private DirectoryInfo _Home;
		private AppDomain _Domain;
		private AppDomainSetup _Setup;
		private string[] _TypeInfo;
		private System.Runtime.Remoting.ObjectHandle _RemoteType = null;
		private FileSystemWatcher _Watcher;

		/// <summary>
		/// Create a hosted service in an assembly
		/// </summary>
		/// <param name="assemblies"></param>
		/// <exception cref="InvalidProgramException">If a config file is not found.</exception>
		public HostedService(DirectoryInfo assemblies)
		{
			_Home = assemblies;
			FileInfo[] fi = assemblies.GetFiles("Service.config");
			if (fi == null || fi.Length != 1)
			{
				throw new InvalidProgramException(String.Format("Could not find Service.config file for the service in directory {0}.", assemblies.FullName)); // no config file
			}
			_Setup = new AppDomainSetup();
			_Setup.ApplicationBase = assemblies.FullName;
			_Setup.ConfigurationFile = fi[0].FullName;
			_Setup.ShadowCopyFiles = "true";
			_Setup.ShadowCopyDirectories = assemblies.FullName;
			_Setup.PrivateBinPath = _Setup.ApplicationBase;
			_Setup.ApplicationName = assemblies.Name;
			string fullTypeName = StealTypeName(_Setup);
			_TypeInfo = fullTypeName.Split(new char[] { ',' }, 2);
			_Watcher = new FileSystemWatcher(assemblies.FullName, "*.control");
			_Watcher.Created += new FileSystemEventHandler(_Watcher_Created);
			_Watcher.EnableRaisingEvents = true;
		}

		void _Watcher_Created(object sender, FileSystemEventArgs e)
		{
			bool restart = String.Compare(e.Name, "restart.control", true) == 0;
			if (restart || String.Compare(e.Name, "stop.control", true) == 0)
			{
				try
				{
					Stop();
					File.Delete(e.FullPath);
					mLog.InfoFormat("Service '{0}' stopped.", _Home.Name);
				}
				catch (Exception ex)
				{
					mLog.Error(String.Format("Failed to stop service '{0}'.  Please try again.", _Home.Name), ex);
				}
			}
			if (restart || String.Compare(e.Name, "start.control", true) == 0)
			{
				if (_Domain == null)
				{
					try
					{
						Start();
						File.Delete(e.FullPath);
						mLog.InfoFormat("Service '{0}' started.", _Home.Name);
					}
					catch (Exception ex)
					{
						mLog.Error(String.Format("Failed to stop service '{0}'.  Please try again.", _Home.Name), ex);
					}
				}
			}
		}

		private string StealTypeName(AppDomainSetup setup)
		{
			AppDomainSetup tmpSetup = new AppDomainSetup();
			tmpSetup.ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
			tmpSetup.ApplicationName = setup.ApplicationName + " - Temporary Config Reader";
			tmpSetup.PrivateBinPath = AppDomain.CurrentDomain.SetupInformation.PrivateBinPath;
			tmpSetup.ConfigurationFile = setup.ConfigurationFile;
			AppDomain tmpDomain = AppDomain.CreateDomain(setup.ApplicationName, null, tmpSetup);
			BodySnatcher b = (BodySnatcher)tmpDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(BodySnatcher).FullName);
			string tn = b.GetSettings();
			AppDomain.Unload(tmpDomain);
			return tn;
		}

		public bool ShouldStart
		{
			get
			{
				FileInfo[] fi = _Home.GetFiles("nostart.control");
				return (fi == null || fi.Length == 0);
			}
		}

		public void Start()
		{
			_Domain = AppDomain.CreateDomain(_Setup.ApplicationName, null, _Setup);
			// Construct the target class, it must take over from there.
			_RemoteType = _Domain.CreateInstance(_TypeInfo[1], _TypeInfo[0]);
		}

		public void Stop()
		{
			if (_Domain != null)
			{
				EventWaitHandle shutdown = _Domain.GetData("ShutdownEvent") as EventWaitHandle;
				EventWaitHandle shutdownAck = _Domain.GetData("ShutdownAckEvent") as EventWaitHandle;
				if (shutdown != null)
				{
					int msecTimeout = 10000;
					object to = _Domain.GetData("ShutdownTimeout");
					if (to != null)
					{
						msecTimeout = (int)to;
					}
					shutdown.Set();
					if (shutdownAck != null)
					{
						if (!shutdownAck.WaitOne(msecTimeout, false))
						{
							mLog.WarnFormat("Timed out waiting for soft shutdown of '{0}', forcing shutdown.", _Home.Name);
						}
						else
						{
							mLog.InfoFormat("Successful soft shutdown of '{0}'.", _Home.Name);
						}
					}
					else
					{
						mLog.WarnFormat("No shutdown ack available for '{0}', waiting {1} msec and then forcing shutdown.", _Home.Name, msecTimeout);
						Thread.Sleep(msecTimeout);
					}
				}
				else
				{
					mLog.WarnFormat("No shutdown handshake available, performing hard shutdown for '{0}'.", _Home.Name);
				}
				_RemoteType = null;
				AppDomain.Unload(_Domain);
				_Domain = null;
			}
		}
	}
}
