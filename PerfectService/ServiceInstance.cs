using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using log4net;

namespace PerfectService
{
	/// <summary>
	/// The PerfectService watches a directory for "hosted service directories" and then starts/stops individual app domains to
	/// allow one service to host several virtual Windows services.  This makes for easier deployments and more flexible
	/// configurations.
	/// </summary>
	public partial class ServiceInstance : ServiceBase
	{
		private FileSystemWatcher _Watch;
		private ILog _Log = LogManager.GetLogger(typeof(ServiceInstance));
		private Dictionary<string, HostedService> _Services = new Dictionary<string, HostedService>();


		public ServiceInstance()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			string baseDir;
			if (args != null && args.Length > 0)
			{
				baseDir = args[0];
			}
			else
			{
				baseDir = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
			}
			_Watch = new FileSystemWatcher(baseDir);
			_Watch.Created += new FileSystemEventHandler(_Watch_Created);
			_Watch.Renamed += new RenamedEventHandler(_Watch_Renamed);
			_Watch.EnableRaisingEvents = true;
			DirectoryInfo di = new DirectoryInfo(baseDir);
			DirectoryInfo[] services = di.GetDirectories();
			foreach (DirectoryInfo s in services)
			{
				if (s.Name.StartsWith("_"))
				{
					continue;
				}
				SetupHostedService(s);
			}
			foreach (KeyValuePair<string, HostedService> kv in _Services)
			{
				if (kv.Value.ShouldStart)
				{
					StartHostedService(kv);
				}
			}
		}

		protected override void OnStop()
		{
			if (_Watch != null)
			{
				_Watch.EnableRaisingEvents = false;
				_Watch.Dispose();
			}
			foreach (KeyValuePair<string, HostedService> kv in _Services)
			{
				try
				{
					kv.Value.Stop();
					_Log.InfoFormat("Succesfully shutdown {0}.", kv.Key);
				}
				catch (Exception e)
				{
					_Log.Error(String.Format("Could not stop service '{0}'", kv.Key), e);
				}
			}
		}

		public void ConsoleStart(string[] args)
		{
			string[] replace = null;
			if (args.Length > 1)
			{
				replace = new string[args.Length - 1];
				args.CopyTo(replace, 1);
			}
			OnStart(replace);
		}

		private void SetupHostedService(DirectoryInfo s)
		{
			try
			{
				HostedService hs = new HostedService(s);
				_Services[s.Name] = hs;
			}
			catch (Exception e)
			{
				_Log.Error(String.Format("Could not setup service '{0}'", s.Name), e);
			}
		}

		private void StartHostedService(KeyValuePair<string, HostedService> kv)
		{
			try
			{
				_Log.InfoFormat("Starting service '{0}'.", kv.Key);
				kv.Value.Start();
				_Log.InfoFormat("Startup completed for '{0}'.", kv.Key);
			}
			catch (Exception e)
			{
				_Log.Error(String.Format("Could not start service '{0}'", kv.Key), e);
			}
		}

		#region FS Event Handlers
		void _Watch_Renamed(object sender, RenamedEventArgs e)
		{
			DirectoryInfo di = new DirectoryInfo(e.FullPath);
			if (di.Exists && !di.Name.StartsWith("_"))
			{
				_Watch_Created(sender, new FileSystemEventArgs(e.ChangeType, di.FullName, e.Name));
			}
		}

		void _Watch_Created(object sender, FileSystemEventArgs e)
		{
			DirectoryInfo di = new DirectoryInfo(e.FullPath);
			if (di.Exists && !di.Name.StartsWith("_"))
			{
				int i;
				for (i = 0; i < 24; i++)
				{
					if (File.Exists(Path.Combine(di.FullName, "Service.config")))
					{
						break;
					}
					_Log.InfoFormat("Waiting for Service.config in {0}", di.Name);
					System.Threading.Thread.Sleep(5000);
				}
				if (i == 24)
				{
					_Log.ErrorFormat("Can't start service in {0} without config.  Ignoring directory until the master service is restarted.", di.Name);
					return;
				}
				SetupHostedService(di);
				if (_Services.ContainsKey(di.Name))
				{
					StartHostedService(new KeyValuePair<string, HostedService>(di.Name, _Services[di.Name]));
				}
			}
		}
		#endregion
	}
}
