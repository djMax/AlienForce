using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlienForce.NoSql.Cassandra;
using System.Reflection;
using System.Linq.Expressions;
using Apache.Cassandra060;
using AlienForce.Utilities.Collections;

namespace AlienForce.NoSql.Cassandra.Map
{
	public static class CassandraMapper
	{
		public class ColumnInfo
		{
			public string Name;
			public object Value;
		}

		public static void DiscoverEntities(IEnumerable<Type> types)
		{
			foreach (Type t in types)
			{
				if (t.GetCustomAttributes(typeof(CassandraEntityAttribute), false).Cast<CassandraEntityAttribute>().FirstOrDefault<CassandraEntityAttribute>() != null)
				{
					MetadataCache.EnsureMetadata(t);
				}
			}
		}

		public static string GetRowKeySuffix(string rowKey, Type t)
		{
			if (rowKey.IndexOf('_') == -1)
			{
				// Row Key Suffixes have _ in them.
				return null;
			}
			var md = MetadataCache.EnsureMetadata(t);
			MetadataCache.CassandraMember mi;
			if (md.Super != null)
			{
				foreach (var superCol in md.Super)
				{
					if (superCol.Key.CompositeKeySuffix != null && rowKey.EndsWith(superCol.Key.CompositeKeySuffix))
					{
						return superCol.Key.CompositeKeySuffix;
					}
				}
			}
			else if (md.Columns != null)
			{
				foreach (var col in md.Columns)
				{
					if (col.Key.CompositeKeySuffix != null && rowKey.EndsWith(col.Key.CompositeKeySuffix))
					{
						return col.Key.CompositeKeySuffix;
					}
				}
			}
			return null;
		}

		public static ColumnInfo GetColumnInfo(string compositeKeySuffix, SuperColumn s, Column c, Type t)
		{
			var md = MetadataCache.EnsureMetadata(t);
			MetadataCache.CassandraMember mi;
			if (md.HasSuperColumnId)
			{
				MetadataCache.ColumnNameSpec cspec = new MetadataCache.ColumnNameSpec(compositeKeySuffix,c.Name);
				if (!md.Columns.TryGetValue(cspec, out mi))
				{
					return null;
				}
				return new ColumnInfo() { Name = mi.Member.Name, Value = mi.GetValueFromCassandra(c) };
			}
			Dictionary<MetadataCache.ColumnNameSpec, MetadataCache.CassandraMember> d;
			if (!md.Super.TryGetValue(new MetadataCache.ColumnNameSpec(compositeKeySuffix, s.Name), out d)) { return null; }
			if (!d.TryGetValue(new MetadataCache.ColumnNameSpec(null, c.Name), out mi)) { return null; }
			return new ColumnInfo() { Name = mi.Member.Name, Value = mi.GetValueFromCassandra(c) };
		}

		public static ColumnInfo GetColumnInfo(string compositeKeySuffix, Column c, Type t)
		{
			MetadataCache.ColumnNameSpec cspec = new MetadataCache.ColumnNameSpec(compositeKeySuffix, c.Name);
			var md = MetadataCache.EnsureMetadata(t);
			MetadataCache.CassandraMember mi;
			if (md.Columns == null || !md.Columns.TryGetValue(cspec, out mi))
			{
				return null;
			}
			return new ColumnInfo() { Name = mi.Member.Name, Value = mi.GetValueFromCassandra(c) };
		}

		public static Type[] GetCandidateTypes(string keyspace, string columnFamily)
		{
			List<Type> r = null;
			foreach (var t in MetadataCache.GetKnownTypes())
			{
				var md = MetadataCache.EnsureMetadata(t);
				if (md.DefaultKeyspace == keyspace && md.DefaultColumnFamily == columnFamily)
				{
					if (r == null) { r = new List<Type>(); }
					r.Add(t);
				}
			}
			return r != null ? r.ToArray() : null;
		}

		public static T Map<T>(string rowKey, byte[] superColumn, List<ColumnOrSuperColumn> record) where T : ICassandraEntity, new()
		{
			var md = MetadataCache.EnsureMetadata(typeof(T));
			if (record != null && record.Count > 0)
			{
				T ret;
				if (md.WithRowKeyAndSuperColumnName != null)
				{
					ret = (T)md.WithRowKeyAndSuperColumnName.Invoke(new object[] { RowKeyConverter.FromRowKey(md.RowKeyType, rowKey), superColumn });
				}
				else if (md.WithRowKey != null)
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

		public static T Map<T>(string rowKey, List<ColumnOrSuperColumn> record) where T : ICassandraEntity, new()
		{
			var md = MetadataCache.EnsureMetadata(typeof(T));
			if (record != null && record.Count > 0)
			{
				T ret;
				if (md.WithRowKey != null)
				{
					int ix;
					if (md.UsesCompositeKeys && (ix = rowKey.IndexOf('_')) > 0)
					{
						rowKey = rowKey.Substring(0, ix);
					}
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
		/// Read a composite-key based set of rows from Cassandra and put them back together into a single
		/// .Net entity.  This uses CompositeKeySuffix to sort things out.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="rowKey"></param>
		/// <param name="records"></param>
		/// <returns></returns>
		public static T Map<T>(string rowKey, Dictionary<string,List<ColumnOrSuperColumn>> records) where T : ICassandraEntity, new()
		{
			var md = MetadataCache.EnsureMetadata(typeof(T));
			if (records != null && records.Count > 0)
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
				foreach (var record in records)
				{
					ret.Load(record.Key.Substring(rowKey.Length + 1), record.Value);
				}
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
		public static List<T> MapMultiple<T>(string rowKey, IEnumerable<ColumnOrSuperColumn> records) where T : ICassandraEntity, new()
		{
			var ret = new List<T>();
			List<ColumnOrSuperColumn> ls = new List<ColumnOrSuperColumn>();
			foreach (var sc in records)
			{
				ls.Clear();
				foreach (var c in sc.Super_column.Columns)
				{
					ls.Add(new ColumnOrSuperColumn(c));
				}
				ret.Add(Map<T>(rowKey, sc.Super_column.Name, ls));
			}
			return ret;
		}

		/// <summary>
		/// Figure out the row key from the type, return all columns
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="c"></param>
		/// <param name="rowKey"></param>
		/// <returns></returns>
		public static T SelectByRowKey<T>(this PooledClient c, object rowKey) where T : ICassandraEntity, new()
		{
			return CassandraMapper.SelectByRowKey<T>(c, RowKeyConverter.ToRowKey(RowKeyConverter.GetRowKeyType(typeof(T)), rowKey));
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
			if (md.UsesCompositeKeys)
			{
				var recs = c.multiget_slice(
					md.DefaultKeyspace,
					md.CompositeKeys(rowKey),
					new ColumnParent() { Column_family = md.DefaultColumnFamily },
					c.SlicePredicateAll(),
					ConsistencyLevel.ONE);
				return CassandraMapper.Map<T>(rowKey, recs);
			}
			else
			{
				var rec = c.get_slice(
					md.DefaultKeyspace,
					rowKey,
					new ColumnParent() { Column_family = md.DefaultColumnFamily },
					c.SlicePredicateAll(),
					ConsistencyLevel.ONE);
				return CassandraMapper.Map<T>(rowKey, rec);
			}
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
				c.SlicePredicateAll(),
				ConsistencyLevel.ONE);
			return CassandraMapper.Map<T>(rowKey, super, rec);
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

		/// <summary>
		/// Save elements of this object that are in the specified super columns
		/// </summary>
		/// <param name="c"></param>
		/// <param name="l"></param>
		/// <param name="superColumns"></param>
		public static void SaveSuper(this PooledClient c, ConsistencyLevel l, ICassandraEntity entity, params string[] superColumns)
		{
			var md = MetadataCache.EnsureMetadata(entity.GetType());
			BatchMutateRequest br = new BatchMutateRequest(md.DefaultKeyspace, l);

			byte[][] supers = new byte[superColumns.Length][];
			for (int i = 0, len = superColumns.Length; i < len; i++)
			{
				supers[i] = Encoding.UTF8.GetBytes(superColumns[i]);
			}

			entity.AddChanges(br, md.DefaultColumnFamily, (mi, cn, scn, cks) =>
				{
					foreach (var b in supers)
					{
						if (ByteArrayComparer.Default.Equals(b, scn))
						{
							return true;
						}
					}
					return false;
				});
			c.batch_mutate(br);
		}

		/// <summary>
		/// Save elements of this object that are in the specified super columns
		/// </summary>
		/// <param name="c"></param>
		/// <param name="l"></param>
		/// <param name="superColumns"></param>
		public static void SaveComposite(this PooledClient c, ConsistencyLevel l, ICassandraEntity entity, params string[] keySuffixes)
		{
			var md = MetadataCache.EnsureMetadata(entity.GetType());
			BatchMutateRequest br = new BatchMutateRequest(md.DefaultKeyspace, l);

			entity.AddChanges(br, md.DefaultColumnFamily, (mi, cn, scn, cks) =>
			{
				foreach (var b in keySuffixes)
				{
					if (b == cks)
					{
						return true;
					}
				}
				return false;
			});
			c.batch_mutate(br);
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
					ICassandraEntity entity;
					if (mexp == null || (cexp = mexp.Expression as ConstantExpression) == null)
					{
						if (mexp.NodeType == ExpressionType.MemberAccess && (cexp = ((MemberExpression)mexp.Expression).Expression as ConstantExpression) != null)
						{
							var mexpinner = ((MemberExpression)mexp.Expression).Member;
							var propI = mexpinner as PropertyInfo;
							var fldI = mexpinner as FieldInfo;
							if (propI != null)
							{
								entity = propI.GetValue(cexp.Value, null) as ICassandraEntity;
							}
							else if (fldI != null)
							{
								entity = fldI.GetValue(cexp.Value) as ICassandraEntity;
							}
							else
							{
								throw new InvalidOperationException(String.Format("SavePartial only allows field or property access, such as 'new {{ this.Field1, this.Property }}' (found {0})", mexpinner.GetType()));
							}
						}
						else
						{
							throw new InvalidOperationException(String.Format("SavePartial only allows field or property access, such as 'new {{ this.Field1, this.Property }}' (found {0})", mem.ToString()));
						}
					}
					else
					{
						entity = cexp.Value as ICassandraEntity;
					}
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
			}
			else if (body.NodeType == System.Linq.Expressions.ExpressionType.MemberAccess)
			{
				// Single member.
				MemberExpression mexp = body as MemberExpression;
				ConstantExpression cexp;
				ICassandraEntity entity;
				if (mexp == null || (cexp = mexp.Expression as ConstantExpression) == null)
				{
					if (mexp.NodeType == ExpressionType.MemberAccess && (cexp = ((MemberExpression)mexp.Expression).Expression as ConstantExpression) != null)
					{
						var mexpinner = ((MemberExpression)mexp.Expression).Member;
						var propI = mexpinner as PropertyInfo;
						var fldI = mexpinner as FieldInfo;
						if (propI != null)
						{
							entity = propI.GetValue(cexp.Value, null) as ICassandraEntity;
						}
						else if (fldI != null)
						{
							entity = fldI.GetValue(cexp.Value) as ICassandraEntity;
						}
						else
						{
							throw new InvalidOperationException(String.Format("SavePartial only allows field or property access, such as 'new {{ this.Field1, this.Property }}' (found {0})", mexpinner.GetType()));
						}
					}
					else
					{
						throw new InvalidOperationException(String.Format("SavePartial only allows field or property access, such as 'new {{ this.Field1, this.Property }}' (found {0})", body.ToString()));
					}
				}
				else
				{
					entity = cexp.Value as ICassandraEntity;
				}
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
			else if (body.NodeType == ExpressionType.Convert)
			{
			}
			// Now iterate over the map of objects needing saves and build batch mutate requests
			foreach (var kvsave in saves)
			{
				var md = MetadataCache.EnsureMetadata(kvsave.Key.GetType());
				BatchMutateRequest br;
				if (!keyspaces.TryGetValue(md.DefaultKeyspace, out br))
				{
					keyspaces[md.DefaultKeyspace] = br = new BatchMutateRequest(md.DefaultKeyspace, l);
				}
				kvsave.Key.AddChanges(br, md.DefaultColumnFamily, (mi, cn, scn, cks) => kvsave.Value.Contains(mi));
			}
			foreach (var kv in keyspaces)
			{
				c.batch_mutate(kv.Value);
			}
		}
	}
}
