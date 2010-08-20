namespace PerfectService
{
	partial class Installer
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.sInstaller = new System.ServiceProcess.ServiceInstaller();
			this.spInstaller = new System.ServiceProcess.ServiceProcessInstaller();
			// 
			// sInstaller
			// 
			this.sInstaller.DisplayName = "AALabs BenTen Service";
			this.sInstaller.ServiceName = "BenTen";
			// 
			// spInstaller
			// 
			this.spInstaller.Password = null;
			this.spInstaller.Username = null;
			// 
			// Installer
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.sInstaller,
            this.spInstaller});
		}

		#endregion

		private System.ServiceProcess.ServiceInstaller sInstaller;
		private System.ServiceProcess.ServiceProcessInstaller spInstaller;
	}
}