using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.NoSql.Cassandra.Map
{
	[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
	public class CassandraRowAttribute : System.Attribute
	{
		public byte[] SuperColumnNameBytes { get; set; }
		public byte[] ColumnNameBytes { get; set; }
		public string Keyspace { get; set; }
		public string ColumnFamily { get; set; }

		public string SuperColumnName
		{
			set { SuperColumnNameBytes = Encoding.UTF8.GetBytes(value); }
			get { return Encoding.UTF8.GetString(SuperColumnNameBytes); }
		}

		public CassandraRowAttribute(string keyspace, string columnFamily)
		{
			Keyspace = keyspace;
			ColumnFamily = columnFamily;
		}

		public CassandraRowAttribute()
		{
		}
	}
}
