using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.NoSql.Cassandra
{
	public static class AlienForceExtensions
	{
		static DateTime UTCBaseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static long ToCassandraTime(this DateTime d)
		{
			return (long)d.ToUniversalTime().Subtract(UTCBaseTime).TotalMilliseconds;
		}

		public static DateTime ToDateTime(this long msecSince1970)
		{
			return UTCBaseTime.AddMilliseconds(msecSince1970);
		}

		public static void batch_mutate(this Cassandra.Client client, BatchMutateRequest r)
		{
			client.batch_mutate(r.Keyspace, r._Request, r.ConsistencyLevel);
		}
		public static byte[] ToCassandra(this long l) { return Thrift.Protocol.TBinaryProtocol.ToNetwork(l); }
		public static byte[] ToCassandra(this int l) { return Thrift.Protocol.TBinaryProtocol.ToNetwork(l); }
		public static byte[] ToCassandra(this short l) { return Thrift.Protocol.TBinaryProtocol.ToNetwork(l); }
		public static byte[] ToCassandra(this string s) { return Encoding.UTF8.GetBytes(s); }
		public static long ReadLong(this byte[] b, int offset) { return Thrift.Protocol.TBinaryProtocol.ReadLong(b, offset); }
		public static int ReadInt(this byte[] b, int offset) { return Thrift.Protocol.TBinaryProtocol.ReadI32(b, offset); }
		public static short ReadShort(this byte[] b, int offset) { return Thrift.Protocol.TBinaryProtocol.ReadI16(b, offset); }
	}

	public partial class SlicePredicate
	{
		public readonly static SlicePredicate Everything = new SlicePredicate()
		{
			Slice_range = new SliceRange() { Count = int.MaxValue, Start = String.Empty.ToCassandra(), Finish = String.Empty.ToCassandra(), Reversed = false }
		};
	}
}
