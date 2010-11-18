using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization;

namespace AlienForce.NoSql.MongoDB
{
	public class NoIdGenerator<T> : IBsonIdGenerator
		where T : class
	{
		public object GenerateId()
		{
			throw new KeyNotFoundException(String.Format("{0} requires that an _id is set before using the object with MongoDB", typeof(T).FullName));
		}

		public bool IsEmpty(object id)
		{
			return id == null;
		}
	}

	public class NoIdIntGenerator : IBsonIdGenerator
	{
		public object GenerateId()
		{
			throw new KeyNotFoundException("_id must be set before using the object with MongoDB");
		}

		public bool IsEmpty(object id)
		{
			return ((int)id) == 0;
		}
	}
}
