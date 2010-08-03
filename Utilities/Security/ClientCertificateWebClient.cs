using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace AlienForce.Utilities.Security
{
	public class ClientCertificateWebClient : System.Net.WebClient
	{
		public X509Certificate Certificate { get; private set; }

		public ClientCertificateWebClient(X509Certificate cert)
		{
			Certificate = cert;
		}

		public ClientCertificateWebClient(string cn) : this(cn, StoreName.My, StoreLocation.LocalMachine)
		{
		}

		public ClientCertificateWebClient(string cn, StoreName n, StoreLocation l)
		{
			X509Store store = new X509Store(n,l);
			store.Open(OpenFlags.ReadOnly);
			try
			{
				var certs = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, cn, false);
				if (certs == null || certs.Count == 0)
				{
					StringBuilder sb = new StringBuilder();
					sb.Append("Invalid certificate subject name presented for store ").Append(Environment.UserName);
					sb.AppendLine(". Valid names include:");
					foreach (var c in store.Certificates)
					{
						sb.Append(" ").AppendLine(c.Subject);
					}
					throw new ArgumentException(sb.ToString());
				}
				// If we have a fully valid cert use it
				foreach (var cert in certs)
				{
					if (cert.Verify())
					{
						Certificate = cert;
						return;
					}
				}
				// If we have a time-valid self-signed or untrusted cert use it
				foreach (var cert in certs)
				{
					if (cert.NotAfter > DateTime.UtcNow && cert.NotBefore < DateTime.UtcNow)
					{
						Certificate = cert;
						return;
					}
				}
				// Ok, well uh... we'll just use the first one.
				Certificate = certs[0];
			}
			finally
			{
				store.Close();
			}
		}

		protected override System.Net.WebRequest GetWebRequest(Uri address)
		{
			HttpWebRequest wr = (HttpWebRequest) base.GetWebRequest(address);
			wr.ClientCertificates.Add(Certificate);
			return wr;
		}

		public static void Main(string[] args)
		{
			ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
			ClientCertificateWebClient wc = new ClientCertificateWebClient(args[0], StoreName.My, StoreLocation.CurrentUser);
			Console.WriteLine(wc.DownloadString(args[1]));
		}
	}
}
