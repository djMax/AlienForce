﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlienForce.NoSql.Cassandra;
using System.Reflection;
using System.Linq.Expressions;

namespace AlienForce.NoSql.Cassandra.Map
{
	public static class CassandraMapper<T> where T : ICassandraEntity, new()
	{
		public static T Map(string rowKey, List<ColumnOrSuperColumn> record)
		{
			var md = MetadataCache.EnsureMetadata(typeof(T));
			if (record != null && record.Count > 0)
			{
				T ret;
				if (md.WithRowKey != null)
				{
					ret = (T)md.WithRowKey.Invoke(new object[] { RowKeyConverter.FromRowKey(md.RowKeyType, rowKey) });
				}
				else
				{
					ret = new T();
				}
				ret.Load(record);
				return ret;
			}

			return default(T);
		}

		/// <summary>
		/// Return a list of objects from a supercolumn family where the supercolum 
		/// name is an object id of some sort
		/// </summary>
		/// <param name="record"></param>
		/// <returns></returns>
		public static List<T> MapMultiple(IEnumerable<ColumnOrSuperColumn> records)
		{
			return new List<T>();
		}
	}

	public static class CassandraMapperExtensions
	{
		/// <summary>
		/// Figure out the row key from the type, return all columns
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="c"></param>
		/// <param name="rowKey"></param>
		/// <returns></returns>
		public static T SelectByRowKey<T>(this PooledClient c, object rowKey) where T : ICassandraEntity, new()
		{
			return c.SelectByRowKey<T>(RowKeyConverter.ToRowKey(RowKeyConverter.GetRowKeyType(typeof(T)), rowKey));
		}

		/// <summary>
		/// Lookup a row by string rowkey, returning all columns
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="c"></param>
		/// <param name="rowKey"></param>
		/// <returns></returns>
		public static T SelectByRowKey<T>(this PooledClient c, string rowKey) where T : ICassandraEntity, new()
		{
			var md = MetadataCache.EnsureMetadata(typeof(T));
			var rec = c.get_slice(
				md.DefaultKeyspace, 
				rowKey, 
				new ColumnParent() { Column_family = md.DefaultColumnFamily }, 
				SlicePredicate.Everything,
				ConsistencyLevel.ONE);
			return CassandraMapper<T>.Map(rowKey, rec);
		}

		/// <summary>
		/// For supercolumn families, lookup a set of columns based on a rowkey and super column name
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="c"></param>
		/// <param name="rowKey"></param>
		/// <param name="super"></param>
		/// <returns></returns>
		public static T SelectByRowKeyAndSuperColumn<T>(this PooledClient c, string rowKey, byte[] super) where T : ICassandraEntity, new()
		{
			var md = MetadataCache.EnsureMetadata(typeof(T));
			var rec = c.get_slice(
				md.DefaultKeyspace,
				rowKey,
				new ColumnParent() { Column_family = md.DefaultColumnFamily, Super_column = super },
				SlicePredicate.Everything,
				ConsistencyLevel.ONE);
			return CassandraMapper<T>.Map(rowKey, rec);
		}

		/// <summary>
		/// Save an entity to Cassandra.
		/// </summary>
		/// <param name="c"></param>
		/// <param name="l"></param>
		/// <param name="row"></param>
		public static void Save(this PooledClient c, ConsistencyLevel l, ICassandraEntity row)
		{
			var md = MetadataCache.EnsureMetadata(row.GetType());
			var mut = new BatchMutateRequest(md.DefaultKeyspace, l);
			row.AddChanges(mut, md.DefaultColumnFamily);
			c.batch_mutate(mut);
		}

		/// <summary>
		/// Save multiple entities to Cassandra in one go (remember that while writes are atomic,
		/// that does not apply across rows or keyspaces.  This means if you send in multiple rows
		/// or keyspaces, some might work and some might not.)
		/// </summary>
		/// <param name="c"></param>
		/// <param name="l"></param>
		/// <param name="rows"></param>
		public static void Save(this PooledClient c, ConsistencyLevel l, params ICassandraEntity[] rows)
		{
			Dictionary<string, BatchMutateRequest> keyspaces = new Dictionary<string, BatchMutateRequest>();
			foreach (var r in rows)
			{
				var md = MetadataCache.EnsureMetadata(r.GetType());
				BatchMutateRequest br;
				if (!keyspaces.TryGetValue(md.DefaultKeyspace, out br))
				{
					keyspaces[md.DefaultKeyspace] = br = new BatchMutateRequest(md.DefaultKeyspace, l);
				}
				r.AddChanges(br, md.DefaultColumnFamily);
			}
			foreach (var kv in keyspaces)
			{
				c.batch_mutate(kv.Value);
			}
		}

		public static void SavePartial(this PooledClient c, ConsistencyLevel l, Expression<Func<object>> expression)
		{
			Dictionary<ICassandraEntity, List<MemberInfo>> saves = new Dictionary<ICassandraEntity, List<MemberInfo>>();
			Dictionary<string, BatchMutateRequest> keyspaces = new Dictionary<string, BatchMutateRequest>();

			var body = expression.Body;
			if (body.NodeType == ExpressionType.New)
			{
				// List of members.
				var nexp = (NewExpression)body;
				foreach (var mem in nexp.Arguments)
				{
					MemberExpression mexp = mem as MemberExpression;
					ConstantExpression cexp;
					if (mexp == null || (cexp = mexp.Expression as ConstantExpression) == null)
					{
						throw new InvalidOperationException(String.Format("SavePartial only allows field or property access, such as 'new {{ this.Field1, this.Property }}' (found {0})", mem.ToString()));
					}
					ICassandraEntity entity = cexp.Value as ICassandraEntity;
					if (entity == null)
					{
						throw new InvalidOperationException(String.Format("SavePartial only allows ICassandraEntity objects to be saved. {0} does not inherit from ICassandraEntity.", cexp.Type.Name));
					}
					List<MemberInfo> minfo;
					if (!saves.TryGetValue(entity, out minfo))
					{
						saves[entity] = minfo = new List<MemberInfo>();
					}
					minfo.Add(mexp.Member);
				}

				// Now iterate over the map of objects needing saves and build batch mutate requests
				foreach (var kvsave in saves)
				{
					var md = MetadataCache.EnsureMetadata(kvsave.Value.GetType());
					BatchMutateRequest br;
					if (!keyspaces.TryGetValue(md.DefaultKeyspace, out br))
					{
						keyspaces[md.DefaultKeyspace] = br = new BatchMutateRequest(md.DefaultKeyspace, l);
					}
					kvsave.Key.AddChanges(br, md.DefaultColumnFamily, (mi) => kvsave.Value.Contains(mi));
				}
				foreach (var kv in keyspaces)
				{
					c.batch_mutate(kv.Value);
				}
			}
			else if (body.NodeType == System.Linq.Expressions.ExpressionType.MemberAccess)
			{
				// Single member.
			}
			else if (body.NodeType == ExpressionType.Convert)
			{
			}
		}
	}
}
