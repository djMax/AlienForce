using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Apache.Cassandra060;

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

		public static void batch_mutate(this PooledClient client, BatchMutateRequest r)
		{
			client.batch_mutate(r.Keyspace, r._Request, r.ConsistencyLevel);
		}

		public static SlicePredicate SlicePredicateAll(this PooledClient client)
		{
			return Everything;
		}

		public readonly static SlicePredicate Everything = new SlicePredicate()
		{
			Slice_range = new SliceRange() { Count = int.MaxValue, Start = String.Empty.ToNetwork(), Finish = String.Empty.ToNetwork(), Reversed = false }
		};
	}
}
