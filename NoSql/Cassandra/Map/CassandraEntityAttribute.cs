using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.NoSql.Cassandra.Map
{
	[AttributeUsage(AttributeTargets.Class)]
	public class CassandraEntityAttribute : System.Attribute
	{
		/// <summary>
		/// If true, all properties are assumed to be "under" a super column id as specified by the SuperColumnId property on the object.
		/// </summary>
		public bool HasSuperColumnId { get; set; }
		public string Keyspace { get; set; }
		public string ColumnFamily { get; set; }

		public CassandraEntityAttribute(string keyspace, string columnFamily)
		{
			Keyspace = keyspace;
			ColumnFamily = columnFamily;
		}
	}
}
