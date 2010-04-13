using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.Utilities.Db
{
	/// <summary>
	/// Metadata about a database connection
	/// </summary>
	public class DbConnectionInfo
	{
		/// <summary>
		/// Gets the name of the connection.
		/// </summary>
		/// <value>The name.</value>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the connection string for the connection.
		/// </summary>
		/// <value>The connection string.</value>
		public string ConnectionString { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this instance is read only.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is read only; otherwise, <c>false</c>.
		/// </value>
		public bool IsReadOnly { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:DbConnectionInfo"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="connectionString">The connection string.</param>
		public DbConnectionInfo(string name, string connectionString)
			: this(name, connectionString, false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:DbConnectionInfo"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="isReadOnly">if set to <c>true</c> [is read only].</param>
		public DbConnectionInfo(string name, string connectionString, bool isReadOnly)
		{
			Name = name;
			ConnectionString = connectionString;
			IsReadOnly = isReadOnly;
		}
	}
}
