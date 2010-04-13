using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using AlienForce.NoSql.Cassandra;
using AlienForce.Utilities.Collections;
using System.Drawing;

namespace AlienForce.NoSql.Cassandra.Map
{
	/// <summary>
	/// Common base class for all things read from and written to Cassandra using the Map framework.
	/// </summary>
	/// <remarks>Could be argued there should be separate classes for Supercolumn-based entities
	/// and column based.  Would remove some squirrely logic below and take that determination out
	/// of the attribute.</remarks>
	/// <typeparam name="RowKeyType"></typeparam>
	public abstract class CassandraEntity<RowKeyType> : ICassandraEntity
	{
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
			get { return (RowKeyType)_RowKey; }
			set { _RowKey = value; }
		}

		#region ICassandraEntity Members

		public byte[] SuperColumnId { get; set; }

		public string RowKeyString
		{
			get
			{
				return RowKeyConverter.ToRowKey(typeof(RowKeyType), _RowKey);
			}
		}

		public byte[] RowKeyForReference
		{
			get
			{
				return RowKeyConverter.ToBytes(typeof(RowKeyType), _RowKey);
			}
		}

		public virtual void AddChanges(BatchMutateRequest request, string columnFamily)
		{
			var md = MetadataCache.EnsureMetadata(this.GetType());
			if (md.HasSuperColumnId)
			{
				foreach (var memberInfo in md.Columns.Values)
				{
					byte[] o = memberInfo.GetValueFromObject(this);
					if (o != null)
					{
						request.AddMutation(columnFamily, RowKeyString, request.GetSupercolumnMutation(SuperColumnId, memberInfo.CassandraName, o));
					}
				}
			}
			if (md.Columns != null)
			{
				foreach (var memberInfo in md.Columns.Values)
				{
					byte[] o = memberInfo.GetValueFromObject(this);
					if (o != null)
					{
						request.AddMutation(columnFamily, RowKeyString, request.GetColumnMutation(memberInfo.CassandraName, o));
					}
				}
			}
			if (md.Super != null)
			{
				foreach (var superColumnInfo in md.Super.Values)
				{
					foreach (var columnInfo in superColumnInfo.Values)
					{
						byte[] o = columnInfo.GetValueFromObject(this);
						if (o != null)
						{
							request.AddMutation(columnFamily, RowKeyString, request.GetSupercolumnMutation(columnInfo.SuperColumnCassandraName, columnInfo.CassandraName, o));
						}
					}
				}
			}
		}

		public virtual void Load(List<ColumnOrSuperColumn> source)
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
					matchingSuper = source.FirstOrDefault<ColumnOrSuperColumn>((cand) => ByteArrayComparer.Default.Equals(cand.Super_column.Name, SuperColumnId));
				}
				if (matchingSuper != null)
				{
					matchingSuper.Super_column.Columns.ForEach((x) => newSource.Add(new ColumnOrSuperColumn() { Column = x }));
					source = newSource;
				}
				else
				{
					return;
				}
			}
			LoadedFrom = source;

			MetadataCache.CassandraMember memberInfo;
			Dictionary<byte[], MetadataCache.CassandraMember> superInfo;

			foreach (var cs in source)
			{
				if (cs.Column != null)
				{
					if (map.Columns != null && map.Columns.TryGetValue(cs.Column.Name, out memberInfo))
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
					if (map.Super != null && map.Super.TryGetValue(cs.Super_column.Name, out superInfo))
					{
						foreach (var scol in cs.Super_column.Columns)
						{
							if (superInfo.TryGetValue(scol.Name, out memberInfo))
							{
								memberInfo.SetValueFromCassandra(this, scol);
							}
							else
							{
								LoadUnknownColumn(scol);
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
