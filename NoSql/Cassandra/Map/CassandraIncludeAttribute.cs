using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.NoSql.Cassandra.Map
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple=false)]
	public class CassandraIncludeAttribute : System.Attribute
	{
	}
}
