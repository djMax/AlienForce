using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Configuration;

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
		public static T GetById(IMongoCollection<T> c, ID id)
		{
			return c.FindOne(new Document("_id", id));
		}

		/// <summary>
		/// Lookup an instance of this type of object from its default database and collection
		/// given the object id.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public static T GetById(Mongo client, ID id)
		{
			return client.GetCollection<T>().FindOne(new Document("_id", id));
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
			if (body.NodeType == System.Linq.Expressions.ExpressionType.MemberAccess)
			{
				// Single member.
				MemberExpression mexp = body as MemberExpression;
				ConstantExpression cexp;
				if (mexp == null || (cexp = mexp.Expression as ConstantExpression) == null)
				{
					if (mexp.NodeType == ExpressionType.MemberAccess && (cexp = ((MemberExpression)mexp.Expression).Expression as ConstantExpression) != null)
					{
						var mexpinner = mexp.Member;
						var pi = mexpinner as PropertyInfo;
						var od = MongoConfiguration.Default.SerializationFactory.GetObjectDescriptor(mexp.Expression.Type);
						return od.GetMongoPropertyName(null, pi != null ? pi.Name : ((FieldInfo)mexpinner).Name);
					}
					else
					{
						throw new InvalidOperationException(String.Format("AddProperty only allows field or property access, such as 'new {{ this.Field1, this.Property }}' (found {0})", body.ToString()));
					}
				}
			}
			throw new InvalidOperationException(String.Format("AddProperty only allows field or property access, such as 'new {{ this.Field1, this.Property }}' (found {0})", body.ToString()));
		}

		/// <summary>
		/// Get a document consisting solely of the object id for this instance.
		/// </summary>
		/// <returns></returns>
		public Document GetIdSelector()
		{
			var od = MongoConfiguration.Default.SerializationFactory.GetObjectDescriptor(typeof(T));
			return new Document("_id", od.GetPropertyValue(this, "_id"));
		}

		/// <summary>
		/// Given a document full of fields to set and another of fields to unset, modify this
		/// record.  Really just a convenience method to avoid creating the id selector document
		/// and the container with $set/$unset calls.
		/// </summary>
		/// <param name="mongo"></param>
		/// <param name="toSet"></param>
		/// <param name="unSet"></param>
		public void SetAndUnset(Mongo mongo, Document toSet, Document unSet)
		{
			mongo.GetCollection<T>().Update(
				new Document("$set", toSet).Add("$unset", unSet), GetIdSelector()
			);
		}

		/// <summary>
		/// Set a set of fields in toSet on this object.  Convenience method for getting the id
		/// selector and adding a $set directive.
		/// </summary>
		/// <param name="mongo"></param>
		/// <param name="unSet"></param>
		public void Set(Mongo mongo, Document toSet)
		{
			mongo.GetCollection<T>().Update(new Document("$set", toSet), GetIdSelector());
		}

		/// <summary>
		/// Unset the set of fields in unSet on this object.  Convenience method for getting the id
		/// selector and adding an $unset directive.
		/// </summary>
		/// <param name="mongo"></param>
		/// <param name="toSet"></param>
		public void Unset(Mongo mongo, Document unSet)
		{
			mongo.GetCollection<T>().Update(new Document("$unset", unSet), GetIdSelector());
		}

		/// <summary>
		/// Shortcut for making a new document, setting a value on it using AddProperty and then calling Set with that document.
		/// </summary>
		/// <param name="mongo"></param>
		/// <param name="propertyExpression"></param>
		/// <param name="value"></param>
		public void SetOne(Mongo mongo, Expression<Func<object>> propertyExpression, object value)
		{
			var doc = new Document().AddProperty(propertyExpression, value);
			Set(mongo, doc);
		}
	}
}
