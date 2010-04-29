using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Apache.Cassandra060;

namespace AlienForce.NoSql.Cassandra
{
	/// <summary>
	///  A helper for the confusing situation of batch_mutate
	/// </summary>
	public class BatchMutateRequest
	{
		internal Dictionary<string, Dictionary<string, List<Mutation>>> _Request = new Dictionary<string, Dictionary<string, List<Mutation>>>();
		internal string Keyspace;
		internal ConsistencyLevel ConsistencyLevel;
		public long Timestamp { get; set; }

		public BatchMutateRequest(string keyspace, ConsistencyLevel level)
		{
			Keyspace = keyspace;
			ConsistencyLevel = level;
			Timestamp = DateTime.UtcNow.ToCassandraTime();
		}

		public void AddMutation(string columnFamily, string rowKey, Mutation mutation)
		{
			Dictionary<string, List<Mutation>> ms;
			if (!_Request.TryGetValue(rowKey, out ms))
			{
				_Request[rowKey] = ms = new Dictionary<string, List<Mutation>>();
			}
			List<Mutation> m;
			if (!ms.TryGetValue(columnFamily, out m))
			{
				ms[columnFamily] = m = new List<Mutation>();
			}
			m.Add(mutation);
		}

		public void AddMutation(string columnFamily, string rowKey, params Mutation[] mutations)
		{
			Dictionary<string, List<Mutation>> ms;
			if (!_Request.TryGetValue(rowKey, out ms))
			{
				_Request[rowKey] = ms = new Dictionary<string, List<Mutation>>();
			}
			List<Mutation> m;
			if (!ms.TryGetValue(columnFamily, out m))
			{
				ms[columnFamily] = m = new List<Mutation>();
			}
			m.AddRange(mutations);
		}

		public Mutation GetSupercolumnMutation(byte[] superColumnName, params byte[][] keysAndValues)
		{
			List<Column> lc = new List<Column>();
			for (int i = 0, len = keysAndValues.Length; i < len; i += 2)
			{
				lc.Add(new Column() { Name = keysAndValues[i], Timestamp = Timestamp, Value = keysAndValues[i + 1] });
			}
			return new Mutation()
			{
				Column_or_supercolumn = new ColumnOrSuperColumn()
				{
					Super_column = new SuperColumn()
					{
						Name = superColumnName,
						Columns = lc
					}
				}
			};
		}

		public Mutation GetSupercolumnMutation(byte[] superColumnName, params KeyValuePair<byte[], string>[] values)
		{
			List<Column> lc = new List<Column>();
			foreach (var kvp in values)
			{
				lc.Add(new Column() { Name = kvp.Key, Timestamp = Timestamp, Value = Encoding.UTF8.GetBytes(kvp.Value) });
			}
			return new Mutation()
			{
				Column_or_supercolumn = new ColumnOrSuperColumn()
				{
					Super_column = new SuperColumn()
					{
						Name = superColumnName,
						Columns = lc
					}
				}
			};
		}

		public Mutation GetColumnMutation(byte[] name, string value)
		{
			return GetColumnMutation(name, Encoding.UTF8.GetBytes(value));
		}

		public Mutation GetColumnMutation(byte[] name, byte[] value)
		{
			return new Mutation()
			{
				Column_or_supercolumn = new ColumnOrSuperColumn()
				{
					Column = new Column()
					{
						Name = name,
						Value = value,
						Timestamp = Timestamp
					}
				}
			};
		}
	}
}
