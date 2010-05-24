using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.NoSql.Cassandra.Map
{
	/// <summary>
	/// Read all entries from one column family and make sure they exist in another (e.g. an index).
	/// Make sure that this is done AFTER you update whatever code generates the entities
	/// </summary>
	/// <example>
	/// var ib = new IndexBuilder<MyCustomerEntity, MyCustomerByEmailEntity>((c) => new MyCustomerByEmailEntity(c.Email, c.RowKey));
	/// ib.IndexAll(client);
	/// </example>
	public class IndexBuilder<T,I> 
		where T : ICassandraEntity, new()
		where I : ICassandraEntity
	{
		int _ChunkSize;
		Func<T, I> _Indexer;

		public IndexBuilder(Func<T, I> transformer, int chunkSize = 100)
		{
			_Indexer = transformer;
			_ChunkSize = chunkSize;
		}

		/// <summary>
		/// Call IndexChunks until it's done.
		/// </summary>
		/// <param name="client"></param>
		public void IndexAll(PooledClient client)
		{
			foreach (var i in IndexChunks(client))
			{
			}
		}

		/// <summary>
		/// Iterate over all the rows in the column family for entity type T, transform them using the
		/// transformer function passed in the constructor (if it returns null, skip this one)
		/// and then save the resulting entities of type I.  Returns after every ChunkSize rows
		/// are processed.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public IEnumerable<int> IndexChunks(PooledClient client)
		{
			var md = MetadataCache.EnsureMetadata(typeof(T));
			var tgt = MetadataCache.EnsureMetadata(typeof(I));
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
				BatchMutateRequest bmr = new BatchMutateRequest(tgt.DefaultKeyspace, Apache.Cassandra060.ConsistencyLevel.QUORUM);
				int thisBatchInserts = 0;
				foreach (var c in rks)
				{
					T exRow = CassandraMapper.Map<T>(c.Key, c.Columns);
					I xForm = _Indexer(exRow);
					if (xForm != null)
					{
						xForm.AddChanges(bmr, tgt.DefaultColumnFamily);
						thisBatchInserts++;
					}
				}
				client.batch_mutate(bmr);
				yield return thisBatchInserts;
				if (rks.Count < kr.Count)
				{
					yield break;
				}
				kr.Start_key = rks[rks.Count-1].Key;
			}
		}
	}
}
