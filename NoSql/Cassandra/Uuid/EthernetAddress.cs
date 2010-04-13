using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            return FromBytes(nics[0].GetPhysicalAddress().GetAddressBytes());
        }

        public static EthernetAddress FromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length != 6)
            {
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
