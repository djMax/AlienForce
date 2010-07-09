using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using AlienForce.NoSql.Cassandra;
using AlienForce.Utilities.Collections;
using System.Drawing;
using Apache.Cassandra060;
using System.Linq.Expressions;

namespace AlienForce.NoSql.Cassandra.Map
{
	/// <summary>
	/// A simple interface to allow containers to temporarily "borrow" a record
	/// from another CF to include in this record.  This interface allows the
	/// CassandraInclude attribute to work.  It's gross, but I don't see how
	/// to get access to CassandraEntity&lt;T&gt;'s private members from
	/// CassandraEntity&lt;U&gt; without some crazy reflection mess.
	/// </summary>
	internal interface _ICassandraEntity
	{
		string RowKeyStringOverride { get; set; }
		byte[] SuperColumnOverride { get; set; }
	}

	/// <summary>
	/// Common base class for all things read from and written to Cassandra using the Map framework.
	/// </summary>
	/// <remarks>Could be argued there should be separate classes for Supercolumn-based entities
	/// and column based.  Would remove some squirrely logic below and take that determination out
	/// of the attribute.</remarks>
	/// <typeparam name="RowKeyType"></typeparam>
	public abstract class CassandraEntity<RowKeyType> : ICassandraEntity, _ICassandraEntity
	{
		List<MemberInfo> Deletes;
		List<ColumnOrSuperColumn> LoadedFrom;

		protected CassandraEntity() { }
		protected CassandraEntity(RowKeyType rowKey)
		{
			RowKey = rowKey;
		}
		protected CassandraEntity(RowKeyType rowKey, byte[] superColumnId)
		{
			RowKey = rowKey;
			SuperColumnId = superColumnId;
		}

		private object _RowKey;
		
		public RowKeyType RowKey 
		{
			get { return _RowKey != null ? ((RowKeyType)_RowKey) : default(RowKeyType); }
			set { _RowKey = value; }
		}

		/// <summary>
		/// When this entity is "included" in another via the CassandraInclude attribute,
		/// this string stores the row key of the original row (same goes for SuperColumns).
		/// 
		/// DO NOT CALL externally unless you really know what you're doing.
		/// This is used to implement CassandraInclude efficiently. (Please tell me a better way to do this)
		/// </summary>
		public byte[] SuperColumnOverride { get; set; }
		/// <summary>
		/// <seealso cref="RowKeyStringOverride"/> This one in slightly fancier in that a
		/// zero-length array indicates "No Super Column Name" even if there was one.
		///
		/// DO NOT CALL externally unless you really know what you're doing.
		/// This is used to implement CassandraInclude efficiently. (Please tell me a better way to do this)
		/// </summary>
		public string RowKeyStringOverride { get; set; }

		#region ICassandraEntity Members

		private byte[] _Super;
		public byte[] SuperColumnId 
		{
			get
			{
				if (SuperColumnOverride != null)
				{
					return SuperColumnOverride.Length == 0 ? null : SuperColumnOverride;
				}
				return _Super;
			}
			set
			{
				_Super = value;
			}
		}

		public string RowKeyString
		{
			get
			{
				return RowKeyStringOverride ?? RowKeyConverter.ToRowKey(typeof(RowKeyType), _RowKey);
			}
		}

		public byte[] RowKeyForReference
		{
			get
			{
				return RowKeyConverter.ToBytes(typeof(RowKeyType), _RowKey);
			}
		}

		/// <summary>
		/// Add all available data to the change request
		/// </summary>
		/// <param name="request"></param>
		/// <param name="columnFamily"></param>
		public void AddChanges(BatchMutateRequest request, string columnFamily)
		{
			AddChanges(request, columnFamily, null);
		}

		/// <summary>
		/// Add data to a change request for this entity.  Call the shouldSave() delegate for each
		/// item to find out if you should indeed save it. VERY IMPORTANT: if your object overrides
		/// this (and if it might want to be included in other objects via the CassandraInclude
		/// attribute), make SURE to call the RowKeyString and SuperColumn properties rather than
		/// getting those values from some other places, because you will spill over into unintended
		/// objects (though probably not in your native column family).
		/// </summary>
		/// <param name="request"></param>
		/// <param name="columnFamily"></param>
		/// <param name="shouldSave"></param>
		public virtual void AddChanges(BatchMutateRequest request, string columnFamily, Func<MemberInfo,byte[],byte[],string,bool> shouldSave)
		{
			var md = MetadataCache.EnsureMetadata(this.GetType());

			if (md.HasSuperColumnId)
			{
				// In this case, the super column id is some sort of version identifier or instance identifier, so all the columns are 
				// actually inside the SuperColumnId
				foreach (var memberInfo in md.Columns.Values)
				{
					if (shouldSave == null || shouldSave(memberInfo.Member, memberInfo.CassandraName, memberInfo.SuperColumnCassandraName, memberInfo.CompositeKeySuffix))
					{
						if (Deletes != null && Deletes.Contains(memberInfo.Member))
						{
							request.AddMutation(columnFamily, memberInfo.GetRowKey(RowKeyString), request.GetSupercolumnDeletion(SuperColumnId, memberInfo.CassandraName));
						}
						else
						{
							byte[] o = memberInfo.GetBytesFromObject(this);
							if (o != null)
							{
								request.AddMutation(columnFamily, memberInfo.GetRowKey(RowKeyString), request.GetSupercolumnMutation(SuperColumnId, memberInfo.CassandraName, o));
							}
						}
					}
				}
				HandleIncludes(md.Includes, request, columnFamily, shouldSave);
			}
			else if (md.Columns != null)
			{
				// Regular column entity, easy as pie.
				foreach (var memberInfo in md.Columns.Values)
				{
					if (shouldSave == null || shouldSave(memberInfo.Member, memberInfo.CassandraName, memberInfo.SuperColumnCassandraName, memberInfo.CompositeKeySuffix))
					{
						if (Deletes != null && Deletes.Contains(memberInfo.Member))
						{
							request.AddMutation(columnFamily, memberInfo.GetRowKey(RowKeyString), request.GetColumnDeletion(memberInfo.CassandraName));
						}							
						else
						{
							if (md.UsesCompositeKeys && memberInfo.ReadAll)
							{
								// This is a ReadAll composite key, so the key is the column name, the value is the value.
								Type[] kvTypes = memberInfo.Type.GetGenericArguments();
								// The key has to be one of the standard types for now.
								var keyConverter = StandardConverters.GetConverter(kvTypes[0]);
								object propVal = memberInfo.RawValue(this);
								System.Collections.IDictionary d = propVal as System.Collections.IDictionary;
								if (d != null)
								{
									foreach (var kv in d.Keys)
									{
										var val = memberInfo.ConvertValue(d[kv]);
										var cname = keyConverter.ToByteArray(kv);
										request.AddMutation(columnFamily, memberInfo.GetRowKey(RowKeyString), request.GetColumnMutation(cname, val));
									}
								}
							}
							else
							{
								byte[] o = memberInfo.GetBytesFromObject(this);
								if (o != null)
								{
									request.AddMutation(columnFamily, memberInfo.GetRowKey(RowKeyString), request.GetColumnMutation(memberInfo.CassandraName, o));
								}
							}
						}
					}
				}
				HandleIncludes(md.Includes, request, columnFamily, shouldSave);
			}
			else if (md.Super != null)
			{
				#region This is a SuperColumn entity
				foreach (var superColumnInfo in md.Super.Values)
				{
					foreach (var columnInfo in superColumnInfo.Values)
					{
						if (shouldSave == null || shouldSave(columnInfo.Member, columnInfo.CassandraName, columnInfo.SuperColumnCassandraName, columnInfo.CompositeKeySuffix))
						{
							if (columnInfo.ReadAll && columnInfo.Type.IsGenericType && columnInfo.Type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
							{
								// This is a bit sketchy - you mark this for delete we're going to delete it all.
								// Perhaps we should look to the keys in the member to figure out what to delete?
								if (Deletes != null && Deletes.Contains(columnInfo.Member))
								{
									request.AddMutation(columnFamily, columnInfo.GetRowKey(RowKeyString), request.GetSupercolumnDeletion(columnInfo.SuperColumnCassandraName, columnInfo.CassandraName));
								}
								else
								{
									#region Multicolumn value
									// This is a "multi value" column where the super column name is fixed, but any column under it is
									// an dictionary entry whose key is the column name and value is the column value.
									Type[] kvTypes = columnInfo.Type.GetGenericArguments();
									// The key has to be one of the standard types for now.
									var keyConverter = StandardConverters.GetConverter(kvTypes[0]);
									object propVal = columnInfo.RawValue(this);
									System.Collections.IDictionary d = propVal as System.Collections.IDictionary;
									if (d != null)
									{
										foreach (var kv in d.Keys)
										{
											var val = columnInfo.ConvertValue(d[kv]);
											var cname = keyConverter.ToByteArray(kv);
											request.AddMutation(columnFamily, columnInfo.GetRowKey(RowKeyString), request.GetSupercolumnMutation(columnInfo.SuperColumnCassandraName, cname, val));
										}
									}
									#endregion
								}
							}
							else
							{
								if (Deletes != null && Deletes.Contains(columnInfo.Member))
								{
									request.AddMutation(columnFamily, columnInfo.GetRowKey(RowKeyString), request.GetSupercolumnDeletion(columnInfo.SuperColumnCassandraName, columnInfo.CassandraName));
								}
								else
								{
									byte[] o = columnInfo.GetBytesFromObject(this);
									if (o != null)
									{
										request.AddMutation(columnFamily, columnInfo.GetRowKey(RowKeyString), request.GetSupercolumnMutation(columnInfo.SuperColumnCassandraName, columnInfo.CassandraName, o));
									}
								}
							}
						}
					}
				}
				HandleIncludes(md.Includes, request, columnFamily, shouldSave);
				#endregion
			}
		}

		/// <summary>
		/// Includes allow one Cassandra entity to include the properties of another as if they were it's own, or as a
		/// group of properties under a supercolumn.
		/// </summary>
		/// <param name="list"></param>
		/// <param name="request"></param>
		/// <param name="columnFamily"></param>
		/// <param name="shouldSave"></param>
		private void HandleIncludes(List<MemberInfo> list, BatchMutateRequest request, string columnFamily, Func<MemberInfo, byte[], byte[], string, bool> shouldSave)
		{
			if (list != null)
			{
				#region Include handling
				foreach (var include in list)
				{
					_ICassandraEntity ice;
					PropertyInfo pi = include as PropertyInfo;
					if (pi != null) { ice = pi.GetValue(this, null) as _ICassandraEntity; }
					else { ice = ((FieldInfo)include).GetValue(this) as _ICassandraEntity; }
					if (ice != null)
					{
						// TODO there's something missing here in regards to Composite Keys.  Not sure exactly how to handle it yet.
						var exRow = ice.RowKeyStringOverride;
						var exSuper = ice.SuperColumnOverride;
						ice.RowKeyStringOverride = this.RowKeyString;
						ice.SuperColumnOverride = this.SuperColumnId;
						try
						{
							((ICassandraEntity)ice).AddChanges(request, columnFamily, shouldSave);
						}
						finally
						{
							ice.RowKeyStringOverride = exRow;
							ice.SuperColumnOverride = exSuper;
						}
					}
				}
				#endregion
			}
		}

		public void Load(List<ColumnOrSuperColumn> source)
		{
			Load(null, source);
		}

		/// <summary>
		/// Load a CLR entity from a Cassandra result.  If you want to add custom fields, use LoadUnknownColumn rather than overriding
		/// this.  But if you do override this, be very careful about the CassandraInclude attribute, because it's immensely confusing.
		/// (Where one entity includes another, so you'll have to handle which one wins with name conflicts, etc)
		/// TODO handle cassandra include ourselves
		/// </summary>
		/// <param name="source"></param>
		public virtual void Load(string compositeKeyPrefix, List<ColumnOrSuperColumn> source)
		{
			var map = MetadataCache.EnsureMetadata(this.GetType());

			if (map.HasSuperColumnId)
			{
				// In this case, while we will receive SuperColumns, we want to treat them like columns, but where we only
				// take those matching our SuperColumnId.  In the case of straight single-row lookup, SuperColumnId will be null,
				// so we'll take the first one we see.  If the case of "load multiple", we will have gotten that super column id set,
				// so we can just match on that.
				List<ColumnOrSuperColumn> newSource = new List<ColumnOrSuperColumn>();
				var matchingSuper = source[0];
				if (SuperColumnId != null)
				{
					matchingSuper = source.FirstOrDefault<ColumnOrSuperColumn>((cand) => (cand.Super_column != null && ByteArrayComparer.Default.Equals(cand.Super_column.Name, SuperColumnId)));
				}
				if (matchingSuper != null && matchingSuper.Super_column != null && matchingSuper.Super_column.Columns != null)
				{
					matchingSuper.Super_column.Columns.ForEach((x) => newSource.Add(new ColumnOrSuperColumn() { Column = x }));
					source = newSource;
				}
				else if (!source.Any<ColumnOrSuperColumn>((cand) => cand.Super_column != null))
				{
					// It appears this means that you asked for just one super column, so C* doesn't bother to fill it out
					// nothing to do, just let it go through
				}
				else
				{
					return;
				}
			}
			LoadedFrom = source;

			MetadataCache.CassandraMember memberInfo;
			Dictionary<MetadataCache.ColumnNameSpec, MetadataCache.CassandraMember> superInfo;

			foreach (var cs in source)
			{
				if (cs.Column != null)
				{
					if (map.Columns != null && map.Columns.TryGetValue(new MetadataCache.ColumnNameSpec(compositeKeyPrefix, cs.Column.Name), out memberInfo))
					{
						memberInfo.SetValueFromCassandra(this, cs.Column);
					}
					else if (map.Columns != null && map.Columns.TryGetValue(new MetadataCache.ColumnNameSpec(compositeKeyPrefix, MetadataCache.Metadata.ReadAllSuperKey.Name), out memberInfo))
					{
						memberInfo.SetValueFromCassandra(this, cs.Column);
					}
					else
					{
						LoadUnknownColumn(cs.Column);
					}
				}
				else if (cs.Super_column != null)
				{
					// This row is super columns.  Does the entity expect a super column with this prefix/name?
					if (map.Super != null && map.Super.TryGetValue(new MetadataCache.ColumnNameSpec(compositeKeyPrefix, cs.Super_column.Name), out superInfo))
					{
						// Is this a "read em all" super column?
						if (superInfo.Count == 1 && superInfo.TryGetValue(MetadataCache.Metadata.ReadAllSuperKey, out memberInfo))
						{
							#region Read all columns under this super column into a dictionary
							var dict = memberInfo.RawValue(this);
							var kvTypes = memberInfo.Type.GetGenericArguments();
							IByteConverter keyConv = StandardConverters.GetConverter(kvTypes[0]);
							if (dict == null)
							{
								dict = memberInfo.Type.GetConstructor(Type.EmptyTypes).Invoke(null);
								memberInfo.SetRawValue(this, dict);
							}
							var asDict = dict as System.Collections.IDictionary;
							if (asDict != null)
							{
								foreach (var scol in cs.Super_column.Columns)
								{
									asDict[keyConv.ToObject(scol.Name)] = memberInfo.GetValueFromCassandra(scol);
								}
							}
							#endregion
						}
						else
						{
							// Regular super-column based column, iterate over the columns 
							// (ignore the composite key prefix because we've decided they are consumed by the super)
							foreach (var scol in cs.Super_column.Columns)
							{
								if (superInfo.TryGetValue(new MetadataCache.ColumnNameSpec(null, scol.Name), out memberInfo))
								{
									memberInfo.SetValueFromCassandra(this, scol);
								}
								else
								{
									LoadUnknownColumn(scol);
								}
							}
						}
					}
					else
					{
						LoadUnknownSuperColumn(cs.Super_column);
					}
				}
			}
		}

		#endregion

		/// <summary>
		/// Mark columns for deletion (still must call Save or SavePartial)
		/// </summary>
		/// <param name="expression"></param>
		public virtual void Delete(Expression<Func<object>> expression)
		{
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
					if (entity == null || entity != this)
					{
						throw new InvalidOperationException(String.Format("Delete only opeates on a single object instance. {0} does not resolve to this ICassandraEntity.", cexp.Type.Name));
					}
					List<MemberInfo> minfo;
					if (Deletes == null)
					{
						Deletes = new List<MemberInfo>();
					}
					Deletes.Add(mexp.Member);
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

		/// <summary>
		/// Override to read your own jagged columns
		/// </summary>
		/// <param name="c"></param>
		protected virtual void LoadUnknownColumn(Column c)
		{
		}

		/// <summary>
		/// Override to read your own jagged super columns
		/// </summary>
		/// <param name="c"></param>
		protected virtual void LoadUnknownSuperColumn(SuperColumn c)
		{
		}

	}
}
