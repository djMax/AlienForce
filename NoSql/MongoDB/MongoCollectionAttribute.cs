using System;

namespace AlienForce.NoSql.MongoDB
{
	/// <summary>
	/// An attribute for Mongo entities that allows you to specify the default
	/// collection and database for objects of this type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class MongoCollectionAttribute : Attribute
	{
		public string Database { get; set; }
		public string Collection { get; set; }

		public MongoCollectionAttribute(string database, string collection)
		{
			Database = database;
			Collection = collection;
		}
	}
}
