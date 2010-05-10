using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace AlienForce.NoSql.Cassandra.Uuid
{
	public class UuidGenerator
	{
		public Uuid GenerateTimeBasedUuid(EthernetAddress ethernetAddress)
		{
			byte[] contents = new byte[16];

			Array.Copy(ethernetAddress.Bytes, 0, contents, 10, 6);

			new UuidTimer(new RNGCryptoServiceProvider()).GetTimestamp(contents);

			return new Uuid(Uuid.UuidType.TimeBased, contents);
		}
	}
}
