using System;
using System.IO;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.DefaultSerializer;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace AlienForce.NoSql.MongoDB
{
	/// <summary>
	/// A base class for objects stored in Mongo.  Mostly useful for compile-time safety
	/// on property references, easy lookup/save.  ID should be the object id type of these
	/// items in Mongo, e.g. usually Oid.  Passing yourself as T allows us to call the 
	/// proper static constructor.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="ID"></typeparam>
	public class MongoBase<T, ID>
		where T : class, new()
	{
		/// <summary>
		/// A readonly instance of T for use in AddProperty/GetMongoProperty name situations where
		/// you don't have/need to make your own T before setting things.
		/// </summary>
		public readonly static T Instance = new T();

		/// <summary>
		/// Lookup an instance of this type of object from the supplied collection
		/// given the object id.
		/// </summary>
		/// <param name="c"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public static T GetById(MongoCollection<T> c, ID id)
		{
			return c.FindOne(new QueryDocument { { "_id", BsonValue.Create(id) } });
		}

		/// <summary>
		/// Lookup an instance of this type of object from its default database and collection
		/// given the object id.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public static T GetById(MongoServer client, ID id)
		{
			var q = Query.EQ("_id", BsonValue.Create(id));
			return client.GetCollection<T>().FindOne(q);
		}

		/// <summary>
		/// Get the Mongo property name for a given field.  The expression should be simple, such as
		/// GetMongoPropertyName(() => myObject.Property) or a nested property such as
		/// GetMongoPropertyName(() => myObject.Subobject.Property)
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		public static string GetMongoPropertyName(Expression<Func<object>> expression)
		{
			var body = expression.Body;
			if (body.NodeType == ExpressionType.MemberAccess)
			{
				// Single member.
				var mexp = body as MemberExpression;
				ConstantExpression cexp;
				if (mexp == null || (cexp = mexp.Expression as ConstantExpression) == null)
				{
				    if (mexp.NodeType == ExpressionType.MemberAccess && (cexp = ((MemberExpression)mexp.Expression).Expression as ConstantExpression) != null)
					{
						var mexpinner = mexp.Member;
						return BsonClassMap.LookupClassMap(mexp.Expression.Type).GetAnyMemberMap(mexpinner.Name).ElementName;
					}
				    throw new InvalidOperationException(String.Format("AddProperty only allows field or property access, such as 'new {{ this.Field1, this.Property }}' (found {0})", body));
				}
			}
			throw new InvalidOperationException(String.Format("AddProperty only allows field or property access, such as 'new {{ this.Field1, this.Property }}' (found {0})", body));
		}

		/// <summary>
		/// Get a document consisting solely of the object id for this instance.
		/// </summary>
		/// <returns></returns>
        public IMongoQuery GetIdSelector()
		{
			var od = BsonClassMap.LookupClassMap(typeof(T));
			return new QueryDocument("_id", BsonValue.Create(od.IdMemberMap.Getter(this)));
		}

		/// <summary>
		/// Given a document full of fields to set and another of fields to unset, modify this
		/// record.  Really just a convenience method to avoid creating the id selector document
		/// and the container with $set/$unset calls.
		/// </summary>
		/// <param name="mongo"></param>
		/// <param name="toSet"></param>
		/// <param name="unSet"></param>
		public void SetAndUnset(MongoServer mongo, BsonDocument toSet, BsonDocument unSet)
		{
		    mongo.GetCollection<T>().Update(
		        GetIdSelector(), new UpdateDocument
		                             {
		                                 {"$set", toSet},
		                                 {"$unset", unSet}
		                             }, UpdateFlags.Upsert);
		}

		/// <summary>
		/// Set a set of fields in toSet on this object.  Convenience method for getting the id
		/// selector and adding a $set directive.
		/// </summary>
		/// <param name="mongo"></param>
        /// <param name="toSet"></param>
		public void Set(MongoServer mongo, BsonDocument toSet)
		{
            mongo.GetCollection<T>().Update(GetIdSelector(), new UpdateDocument("$set", toSet), UpdateFlags.Upsert);
		}

	    /// <summary>
	    /// Unset the set of fields in unSet on this object.  Convenience method for getting the id
	    /// selector and adding an $unset directive.
	    /// </summary>
	    /// <param name="mongo"></param>
	    /// <param name="unSet"></param>
	    public void Unset(MongoServer mongo, BsonDocument unSet)
		{
			mongo.GetCollection<T>().Update(GetIdSelector(), new UpdateDocument("$unset", unSet), UpdateFlags.Upsert);
		}

		/// <summary>
		/// Shortcut for making a new document, setting a value on it using AddProperty and then calling Set with that document.
		/// </summary>
		/// <param name="mongo"></param>
		/// <param name="propertyExpression"></param>
		/// <param name="value"></param>
		public void SetOne(MongoServer mongo, Expression<Func<object>> propertyExpression, object value)
		{
			var doc = new BsonDocument().AddProperty(propertyExpression, value);
			Set(mongo, doc);
		}

		public byte[] ToBSON()
		{
			byte[] bson;
			using (var ms = new MemoryStream())
			{
				using (var writer = BsonWriter.Create(ms))
				{
					BsonSerializer.Serialize(writer, this);
					writer.Close();
					ms.Flush();
					bson = ms.ToArray();
				}
			}
			return bson;
		}

		public static T FromBSON(byte[] b)
		{
			using (var ms = new MemoryStream(b))
			{
				return FromBSON(ms);
			}
		}

		public static T FromBSON(Stream s)
		{
			return BsonSerializer.Deserialize<T>(s);
		}
	}
}
