using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;


namespace PerfectService
{
	[RunInstaller(true)]
	public partial class Installer : System.Configuration.Install.Installer
	{
		public Installer()
		{
			InitializeComponent();
		}

		public override void Install(System.Collections.IDictionary stateSaver)
		{
			SetParams();
			base.Install(stateSaver);
		}

		public override void Uninstall(System.Collections.IDictionary savedState)
		{
			SetParams();
			base.Uninstall(savedState);
		}

		void SetParams()
		{
			if (this.Context.Parameters.ContainsKey("DisplayName"))
			{
				string dn = Context.Parameters["DisplayName"];
				if (dn != null && (dn = dn.Trim()).Length > 0)
				{
					this.Context.LogMessage("Using service display name: " + dn);
					sInstaller.DisplayName = dn;
				}
			}
			if (Context.Parameters.ContainsKey("ServiceName"))
			{
				string dn = Context.Parameters["servicename"];
				if (dn != null && (dn = dn.Trim()).Length > 0)
				{
					this.Context.LogMessage("Using service name: " + dn);
					sInstaller.ServiceName = dn;
				}
			}
		}
	}
}
