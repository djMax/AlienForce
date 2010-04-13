using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.NoSql.Cassandra.Map
{
	/// <summary>
	/// A weakly typed interface (because it's used with reflection it's easier this way) to convert to and from Cassandra byte arrays
	/// </summary>
	public interface IByteConverter
	{
		byte[] ToByteArray(object o);

		object ToObject(byte[] b);
	}
}
