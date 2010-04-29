using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Configuration;
using System.Web.Configuration;
using System.Xml;
using System.Web;

namespace AlienForce.Utilities.Security
{
	/// <summary>
	/// Save data to a protected section of configuration files.  This optionally uses .Net config file encryption 
	/// as well as DPAPI for securing individual values.  Since you can read this code, if you can break DPAPI
	/// you can get the contents.
	/// </summary>
	public static class MachineLocalPrivateStore
	{
		public static Configuration GetConfiguration()
		{
			if (HttpContext.Current != null)
			{
				return WebConfigurationManager.OpenWebConfiguration("/");
			}
			return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
		}

		/// <summary>
		/// Save an unencrypted value to the private store
		/// </summary>
		/// <param name="c"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void Save(Configuration c, string key, string value)
		{
			if (c == null)
			{
				c = GetConfiguration();
			}
			XmlConfigurationSection xcs = (XmlConfigurationSection)c.GetSection("PrivateStore");
			if (xcs == null)
			{
				c.Sections.Add("PrivateStore", xcs = new XmlConfigurationSection("PrivateStore"));
			}

			string xmlKey = XmlConvert.EncodeName(key);
			var exNode = xcs.Node.SelectSingleNode(xmlKey);
			if (exNode == null)
			{
				exNode = xcs.Node.OwnerDocument.CreateElement(xmlKey);
				xcs.Node.AppendChild(exNode);
			}
			exNode.InnerText = value;
			xcs.SectionInformation.ForceSave = true;
			c.Save();
		}

		/// <summary>
		/// Protect a value with the DPAPI and then store it in the private store
		/// </summary>
		/// <param name="c"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void ProtectAndSave(Configuration c, string key, string value)
		{
			byte[] protectedData = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), Encoding.UTF8.GetBytes(key), DataProtectionScope.LocalMachine);

			Save(c, key, Convert.ToBase64String(protectedData));
		}

		public static bool Unprotect(Configuration c)
		{
			if (c == null)
			{
				c = GetConfiguration();
			}

			XmlConfigurationSection xcs = (XmlConfigurationSection)c.GetSection("PrivateStore");
			if (xcs != null)
			{
				if (xcs.SectionInformation.IsProtected)
				{
					xcs.SectionInformation.UnprotectSection();
					xcs.SectionInformation.ForceSave = true;
					c.Save();
				}
				return true;
			}
			return false;
		}

		public static bool Protect(Configuration c)
		{
			if (c == null)
			{
				c = GetConfiguration();
			}

			XmlConfigurationSection xcs = (XmlConfigurationSection)c.GetSection("PrivateStore");
			if (xcs != null)
			{
				if (xcs.SectionInformation.IsProtected)
				{
					xcs.SectionInformation.UnprotectSection();
					xcs.SectionInformation.ForceSave = true;
					c.Save();
				}
				return true;
			}
			return false;
		}

		public static string GetString(Configuration c, string key)
		{
			if (c == null)
			{
				c = GetConfiguration();
			}

			XmlConfigurationSection xcs = (XmlConfigurationSection)c.GetSection("PrivateStore");
			if (xcs == null || xcs.Node == null)
			{
				return null;
			}
			var exNode = xcs.Node.SelectSingleNode(XmlConvert.EncodeName(key));
			if (exNode != null)
			{
				return exNode.InnerText;
			}
			return null;
		}

		public static string GetProtectedString(Configuration c, string key)
		{
			var str = GetString(c, key);
			if (str != null)
			{
				return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(str), Encoding.UTF8.GetBytes(key), DataProtectionScope.LocalMachine));
			}
			return null;
		}

		public static int Main(string[] args)
		{
			Configuration c;
			if (args.Length >= 3 && args[0] == "exe")
			{
				c = ConfigurationManager.OpenExeConfiguration(args[1]);
			}
			else if (args.Length >= 3 && args[0] == "web")
			{
				Uri u = new Uri(args[1]);
				c = WebConfigurationManager.OpenWebConfiguration(u.AbsolutePath, u.Host);
			}
			else
			{
				Console.WriteLine("Usage: Launcher.exe AlienForce.Utilities.dll MachineLocalPrivateStore [web|exe] [http://iis_site_name/path|path] <key name> <value>");
				return -1;
			}
			if (args.Length == 4)
			{
				Save(c, args[2], args[3]);
			}
			else if (args.Length == 3 && args[2] == "protect")
			{
				Protect(c);
			}
			else if (args.Length == 3 && args[2] == "unprotect")
			{
				Unprotect(c);
			}
			return 0;
		}
	}
}
