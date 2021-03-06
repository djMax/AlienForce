﻿using System.Configuration;
using System.ServiceProcess;
using System.Threading;

namespace PerfectService
{
	static class ServiceMain
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			var lfn = ConfigurationManager.AppSettings["log4net.configFile"];
			if (lfn != null)
			{
				log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(lfn));
			}
			else
			{
				log4net.Config.XmlConfigurator.Configure();
			}
			log4net.LogManager.GetLogger(typeof(ServiceMain)).Info("Starting up.");

		    var bsi = new ServiceInstance();
			var servicesToRun = new ServiceBase[] { bsi };

			if (args == null || args.Length == 0 || args[0] != "console")
			{
				ServiceBase.Run(servicesToRun);
			}
			else
			{
				log4net.LogManager.GetLogger(typeof(ServiceMain)).Info("Starting console version.");
				ThreadPool.QueueUserWorkItem(o => bsi.ConsoleStart(args));
				System.Windows.Forms.Application.Run(new ConsoleForm());
				log4net.LogManager.GetLogger(typeof(ServiceMain)).Info("Finished OnStart.");
			}
		}
	}
}
