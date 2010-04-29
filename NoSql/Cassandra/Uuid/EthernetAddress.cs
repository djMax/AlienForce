using System;
using System.Net.NetworkInformation;

namespace AlienForce.NoSql.Cassandra.Uuid
{
	public class EthernetAddress
	{
		private byte[] mBytes = new byte[6];

		public static EthernetAddress GetPrimaryAddress()
		{
			NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
			if (nics == null || nics.Length == 0)
			{
				return null;
			}
			foreach (var nic in nics)
			{
				byte[] addressBytes;
				if ((addressBytes = nic.GetPhysicalAddress().GetAddressBytes()) != null && addressBytes.Length == 6)
				{
					return FromBytes(addressBytes);
				}
			}
			throw new Exception("No ethernet cards could be found, so Time Based UUIDs cannot be generated.");
		}

		public static EthernetAddress FromBytes(byte[] bytes)
		{
			if (bytes == null || bytes.Length != 6)
			{
				System.Diagnostics.Debugger.Break();
				throw new ArgumentException("EthernetAddress requires a six byte address");
			}
			EthernetAddress ea = new EthernetAddress();
			Array.Copy(bytes, ea.mBytes, 6);
			return ea;
		}

		public byte[] Bytes
		{
			get
			{
				return mBytes;
			}
		}

		public override int GetHashCode()
		{
			int len = mBytes.Length;
			if (len == 0) return 0;

			int hc = mBytes[0];
			for (int i = 1; i < len; i++)
			{
				hc *= 37;
				hc += mBytes[i];
			}
			return hc;
		}

		public override bool Equals(object obj)
		{
			EthernetAddress ea = obj as EthernetAddress;
			if (ea == null || ea.mBytes.Length != mBytes.Length)
			{
				return false;
			}
			for (int i = 0; i < mBytes.Length; i++)
			{
				if (mBytes[i] != ea.mBytes[i])
				{
					return false;
				}
			}
			return true;
		}
	}
}
