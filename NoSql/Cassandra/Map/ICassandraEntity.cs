using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlienForce.NoSql.Cassandra;
using System.Reflection;

namespace AlienForce.NoSql.Cassandra.Map
{
	public interface ICassandraEntity
	{
		void Load(List<ColumnOrSuperColumn> source);

		/// <summary>
		/// Add all relevant changes to a BatchMutateRequest. Since there are cases where
		/// you might want to store the same entity in multiple column families, you
		/// must pass that along.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="columnFamily"></param>
		void AddChanges(BatchMutateRequest request, string columnFamily);

		/// <summary>
		/// <seealso cref="AddChanges"/>.  Call shouldSave for each proposed addition to the
		/// mutation request.  This allows partial object saves.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="columnFamily"></param>
		/// <param name="shouldSave"></param>
		void AddChanges(BatchMutateRequest request, string columnFamily, Func<MemberInfo, bool> shouldSave);

		string RowKeyString { get; }
		byte[] RowKeyForReference { get; }

		/// <summary>
		/// If non-null, all properties will be saved under a super-column with this name. This allows
		/// runtime handling of "lists of things".
		/// </summary>
		byte[] SuperColumnId { get; }
	}
}
