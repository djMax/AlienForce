using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.NoSql.Cassandra.Map
{
	public class AggregateBuilder<SourceType, AggregateType, AggregateBagType>
		where SourceType : ICassandraEntity, new()
		where AggregateType : ICassandraEntity, new()
	{
		Action<SourceType> _Updater;
		Func<List<AggregateType>> _Generator;
		int _ChunkSize;

		public AggregateBuilder(
			Action<SourceType> updater,
			Func<List<AggregateType>> generator,
			int chunkSize = 100)
		{
			_Updater = updater;
			_Generator = generator;
			_ChunkSize = chunkSize;
		}

		/// <summary>
		/// Call IndexChunks until it's done.
		/// </summary>
		/// <param name="client"></param>
		public void AggregateAll(PooledClient client)
		{
			foreach (var i in AggregateChunks(client))
			{
			}
			var aggs = _Generator();
			if (aggs != null)
			{
				var tgt = MetadataCache.EnsureMetadata(typeof(AggregateType));
				BatchMutateRequest bmr = new BatchMutateRequest(tgt.DefaultKeyspace, Apache.Cassandra060.ConsistencyLevel.QUORUM);
				foreach (var a in aggs)
				{
					a.AddChanges(bmr, tgt.DefaultColumnFamily);
				}
				client.batch_mutate(bmr);
			}
		}

		/// <summary>
		/// Iterate over all the rows in the column family for entity type T, combine them using
		/// the updater function (you are responsible for storing a dictionary by period key)
		/// and then save the resulting entities of type I.  Returns after each ChunkSize rows
		/// are processed.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public IEnumerable<int> AggregateChunks(PooledClient client)
		{
			var md = MetadataCache.EnsureMetadata(typeof(SourceType));
			var sp = client.SlicePredicateAll();
			var kr = new Apache.Cassandra060.KeyRange() { Count = _ChunkSize, Start_key = String.Empty, End_key = String.Empty };
			var cp = new Apache.Cassandra060.ColumnParent(md.DefaultColumnFamily);

			while (true)
			{
				int minCount = kr.Start_key == String.Empty ? 0 : 1;
				kr.Count = _ChunkSize + minCount;
				var rks = client.get_range_slices(md.DefaultKeyspace, cp, sp, kr, Apache.Cassandra060.ConsistencyLevel.ONE);
				if (rks == null || rks.Count <= minCount)
				{
					yield break;
				}
				int thisBatchInserts = 0;
				foreach (var c in rks)
				{
					SourceType exRow = CassandraMapper.Map<SourceType>(c.Key, c.Columns);
					_Updater(exRow);
					thisBatchInserts++;
				}
				yield return thisBatchInserts;
				if (rks.Count < kr.Count)
				{
					yield break;
				}
				kr.Start_key = rks[rks.Count - 1].Key;
			}
		}
	}
}
