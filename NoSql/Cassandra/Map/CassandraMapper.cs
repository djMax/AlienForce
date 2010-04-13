using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlienForce.NoSql.Cassandra;

namespace AlienForce.NoSql.Cassandra.Map
{
	public static class CassandraMapper<T> where T : ICassandraEntity, new()
	{
		static string _Keyspace;
		static string _ColumnFamily;

		public static string DefaultKeyspace { get { return _Keyspace; } }
		public static string DefaultColumnFamily { get { return _ColumnFamily; } }

		static CassandraMapper()
		{
			foreach (var ca in typeof(T).GetCustomAttributes(typeof(CassandraEntityAttribute), true).Cast<CassandraEntityAttribute>())
			{
				if (!String.IsNullOrEmpty(ca.DefaultKeyspace))
				{
					_Keyspace = ca.DefaultKeyspace;
				}
				if (!String.IsNullOrEmpty(ca.DefaultColumnFamily))
				{
					_ColumnFamily = ca.DefaultColumnFamily;
				}
			}			
		}

		public static T Map(List<ColumnOrSuperColumn> record)
		{
			if (record != null && record.Count > 0)
			{
				var ret = new T();
				ret.Load(record);
				return ret;
			}
			return default(T);
		}

	}

	public static class CassandraMapperExtensions
	{
		/// <summary>
		/// Figure out the row key from the type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="c"></param>
		/// <param name="rowKey"></param>
		/// <returns></returns>
		public static T SelectByRowKey<T>(this PooledClient c, object rowKey) where T : ICassandraEntity, new()
		{
			return c.SelectByRowKey<T>(RowKeyConverter.ToRowKey(RowKeyConverter.GetRowKeyType(typeof(T)), rowKey));
		}

		public static T SelectByRowKey<T>(this PooledClient c, string rowKey) where T : ICassandraEntity, new()
		{
			var rec = c.get_slice(
				CassandraMapper<T>.DefaultKeyspace, 
				rowKey, 
				new ColumnParent() { Column_family = CassandraMapper<T>.DefaultColumnFamily }, 
				SlicePredicate.Everything,
				ConsistencyLevel.ONE);
			return CassandraMapper<T>.Map(rec);
		}

		public static void Save<T>(this PooledClient c, ICassandraEntity row, ConsistencyLevel l) where T : ICassandraEntity, new()
		{
			var mut = new BatchMutateRequest(CassandraMapper<T>.DefaultKeyspace, l);
			row.AddChanges(mut, CassandraMapper<T>.DefaultColumnFamily);
			c.batch_mutate(mut);
		}
	}
}
