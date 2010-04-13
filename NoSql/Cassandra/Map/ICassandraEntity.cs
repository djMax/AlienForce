using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlienForce.NoSql.Cassandra;

namespace AlienForce.NoSql.Cassandra.Map
{
	public interface ICassandraEntity
	{
		void Load(List<ColumnOrSuperColumn> source);
		void AddChanges(BatchMutateRequest request, string columnFamily);

		string RowKeyString { get; }
		byte[] RowKeyForReference { get; }

		/// <summary>
		/// If non-null, all properties will be saved under a super-column with this name. This allows
		/// runtime handling of "lists of things".
		/// </summary>
		byte[] SuperColumnId { get; }
	}
}
