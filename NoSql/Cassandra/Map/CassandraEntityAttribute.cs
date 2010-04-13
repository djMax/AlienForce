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
		public string DefaultKeyspace { get; set; }
		public string DefaultColumnFamily { get; set; }

		public CassandraEntityAttribute(string keyspace, string columnFamily)
		{
			DefaultKeyspace = keyspace;
			DefaultColumnFamily = columnFamily;
		}
	}
}
