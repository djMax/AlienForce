using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.DefaultSerializer;

namespace AlienForce.NoSql.MongoDB
{
	public static class MongoExtensions
	{
		private static readonly DateTime kBaseTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		private static readonly long kClockOffset = 0x01b21dd213814000L;
		private static readonly long kClockMultiplierL = 10000L;

		/// <summary>
		/// Construct an Oid and then replace the time component.  Obviously make sure you know what you're doing here
		/// as you may create Oid clashes if you are not careful.
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		public static ObjectId OidFromTime(this DateTime time)
		{
			byte[] exArr = ObjectId.GenerateNewId().ToByteArray();
			long msecSinceEpoch = (long) time.Subtract(kBaseTime).TotalSeconds;
			exArr[0] = (byte)((msecSinceEpoch >> 24) & 0xFF);
			exArr[1] = (byte)((msecSinceEpoch >> 16) & 0xFF);
			exArr[2] = (byte)((msecSinceEpoch >> 8) & 0xFF);
			exArr[3] = (byte)((msecSinceEpoch) & 0xFF);
			return new ObjectId(exArr);
		}

		/// <summary>
		/// Convert a Cassandra time based UUID into a MongoDB object id in a deterministic way.
		/// Removes some entropy obviously, but generally "should be fine."  Specifically,
		/// the OID clock is seconds not 100-nanosecond units.  We get the low byte of the UUID
		/// back as the process id of the OID, so not as bad as it sounds.
		/// </summary>
		/// <param name="timebasedUuid"></param>
		/// <returns></returns>
		public static ObjectId ConvertUUIDToOid(this Guid timebasedUuid)
		{
			byte[] arr = timebasedUuid.ToByteArray();
			// UUID FORMAT:
			// 0-3 - low 4 bytes of clock, in big endian
			// 4-5 - bytes 5-6 of clock, big endian
			// 6-7 - bytes 7-8 of clock, big endian, top 4 bits of byte 7 masked off
			// 8-9 - Clock sequence
			// 10-15 - ethernet MAC
			long msecSinceEpoch = (((long)arr[0]) << 24) + (arr[1] << 16) + (arr[2] << 8) + arr[3];
			msecSinceEpoch += (((long)arr[4]) << 40) + (((long)arr[5]) << 32) +
				((((long)arr[6]) & 0x0F) << 56) + (((long)arr[7]) << 48);
			msecSinceEpoch -= kClockOffset;
			msecSinceEpoch /= kClockMultiplierL;

			byte[] oid = new byte[12];
			uint secSinceEpoch = (uint)(msecSinceEpoch / 1000);
			oid[0] = (byte)((secSinceEpoch >> 24) & 0xFF);
			oid[1] = (byte)((secSinceEpoch >> 16) & 0xFF);
			oid[2] = (byte)((secSinceEpoch >> 8) & 0xFF);
			oid[3] = (byte)((secSinceEpoch) & 0xFF);
			Array.Copy(arr, 11, oid, 4, 5);
			oid[9] = arr[3]; // we lopped this off of the time, so might as well get it back
			oid[10] = arr[8];
			oid[11] = arr[9];
			return new ObjectId(oid);
		}

		/// <summary>
		/// Add a property to a Document with value 1 (useful for unset and/or indexes).  Using
		/// expressions allows us to get compile time safety while still dealing with MongoAlias attributes.
		/// </summary>
		/// <param name="doc"></param>
		/// <param name="expression"></param>
		public static T AddProperty<T>(this T doc, Expression<Func<object>> expression) where T : BsonDocument
		{
			return AddProperty<T>(doc, expression, 1);
		}

		/// <summary>
		/// Add a property to a document with a specified value. Using
		/// expressions allows us to get compile time safety while still dealing with MongoAlias attributes.
		/// </summary>
		/// <param name="doc"></param>
		/// <param name="expression"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static T AddProperty<T>(this T doc, Expression<Func<object>> expression, object value) where T : BsonDocument
		{
			return doc.AddProperty<T>(expression, null, value);
		}

		/// <summary>
		/// Add a property to a document with a specified value. rejoinder is appended to the generated name.
		/// This is useful when you have dynamic property names such as when using a dictionary type member. Using
		/// expressions allows us to get compile time safety for the non-dynamic bits while still dealing with 
		/// MongoAlias attributes or other name translations.
		/// </summary>
		/// <param name="doc"></param>
		/// <param name="expression"></param>
		/// <param name="rejoinder"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static T AddProperty<T>(this T doc, Expression<Func<object>> expression, string rejoinder, object value) where T : BsonDocument
		{
			var body = expression.Body;
			if (body.NodeType == ExpressionType.Convert)
			{
				body = ((System.Linq.Expressions.UnaryExpression)body).Operand;
			}
			if (body.NodeType == System.Linq.Expressions.ExpressionType.MemberAccess)
			{
				// Single member.
				var mexp = body as MemberExpression;
				if (mexp != null)
				{
					var expName = BuildName(mexp, body);
					if (!String.IsNullOrEmpty(rejoinder))
					{
						if (rejoinder[0] == '[')
						{
							expName = String.Concat(expName, rejoinder);
						}
						else
						{
							expName = String.Concat(expName, ".", rejoinder);
						}
					}
					doc.Add(expName, BsonValue.Create(value));
					return doc;
				}
			}
			throw new InvalidOperationException(String.Format("AddProperty only allows field or property access, such as 'new {{ this.Field1, this.Property }}' (found {0})", body.ToString()));
		}

		/// <summary>
		/// Call AddProperty if the value parameter is non-null
		/// </summary>
		/// <param name="doc"></param>
		/// <param name="expression"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static T AddPropertyIfNotNull<T>(this T doc, Expression<Func<object>> expression, object value) where T : BsonDocument
		{
			return AddPropertyIf<T>(doc, expression, value != null, value);
		}

		/// <summary>
		/// Call AddProperty if the shouldAdd parameter is true.  Helpful for Fluent sytles.
		/// </summary>
		/// <param name="doc"></param>
		/// <param name="expression"></param>
		/// <param name="shouldAdd"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static T AddPropertyIf<T>(this T doc, Expression<Func<object>> expression, bool shouldAdd, object value) where T : BsonDocument
		{
		    return shouldAdd ? AddProperty<T>(doc, expression, value) : doc;
		}

	    public static T AddIfNotNull<T>(this T doc, string key, object value) where T : BsonDocument
		{
			if (value != null)
			{
				doc.Add(key, BsonValue.Create(value));
			}
			return doc;
		}

		static readonly Dictionary<Type, KeyValuePair<string, string>> _Collections = new Dictionary<Type, KeyValuePair<string, string>>();

		public static MongoCollection GetUntypedCollection<T>(this MongoServer mongo)
		{
			var kv = GetAttribute(typeof(T));
			return mongo[kv.Key].GetCollection(kv.Value);
		}

		public static MongoCollection<T> GetCollection<T>(this MongoServer mongo)
			where T : class
		{
			var kv = GetAttribute(typeof(T));
			return mongo[kv.Key].GetCollection<T>(kv.Value);
		}

		public static MongoDatabase GetDatabase<T>(this MongoServer mongo)
		{
			var kv = GetAttribute(typeof(T));
			return mongo[kv.Key];
		}

		static KeyValuePair<string, string> GetAttribute(Type t)
		{
			KeyValuePair<string, string> kv;
			if (_Collections.TryGetValue(t, out kv))
			{
				return kv;
			}
			var atts = t.GetCustomAttributes(typeof(MongoCollectionAttribute), false);
			if (atts.Length == 0)
			{
				atts = t.GetCustomAttributes(typeof(MongoCollectionAttribute), true);
			}
			if (atts == null || atts.Length == 0)
			{
				throw new TypeLoadException(String.Format("Type {0} or its super classes must define the default database or collection by using the MongoCollection attribute.", t.FullName));
			}
			var ma = (MongoCollectionAttribute)atts[0];
			_Collections[t] = new KeyValuePair<string, string>(ma.Database, ma.Collection);
			return new KeyValuePair<string, string>(ma.Database, ma.Collection);
		}

		/// <summary>
		/// Get the Mongo property name for a given field.  The expression should be simple, such as
		/// GetMongoPropertyName(() => myObject.Property) or a nested property such as
		/// GetMongoPropertyName(() => myObject.Subobject.Property)
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		public static string GetMongoPropertyName(this MongoServer unused, Expression<Func<object>> expression)
		{
			var body = expression.Body;
			if (body.NodeType == ExpressionType.Convert)
			{
				body = ((System.Linq.Expressions.UnaryExpression)body).Operand;
			}
			if (body.NodeType == System.Linq.Expressions.ExpressionType.MemberAccess)
			{
				// Single member.
				var mexp = body as MemberExpression;
				if (mexp != null)
				{
					return BuildName(mexp, body);
				}
			}
			throw new InvalidOperationException(String.Format("GetMongoPropertyName only allows field or property access, such as '() => obj.Field1' (found {0})", body.ToString()));
		}

		static string BuildName(MemberExpression mexp, Expression body)
		{
			MemberExpression minn;
			if (mexp.NodeType == ExpressionType.MemberAccess && mexp.Expression == null)
			{
				return null;
			}
			if (mexp.Expression != null && mexp.Expression is ConstantExpression)
			{
				var mexpinner = mexp.Member;
				return BsonClassMap.LookupClassMap(mexp.Expression.Type).GetAnyMemberMap(mexpinner.Name).ElementName;
			}
			if (mexp.NodeType == ExpressionType.MemberAccess &&
				(((MemberExpression)mexp.Expression).Expression as ConstantExpression) != null)
			{
				var mexpinner = mexp.Member;
				return BsonClassMap.LookupClassMap(mexp.Expression.Type).GetAnyMemberMap(mexpinner.Name).ElementName;
			}
			else if (mexp.NodeType == ExpressionType.MemberAccess && (minn = ((MemberExpression)mexp.Expression as MemberExpression)) != null)
			{
				var mexpinner = mexp.Member;
				var od = BsonClassMap.LookupClassMap(mexp.Expression.Type);
				var topName = BuildName(minn, body);
				if (topName != null)
				{
					return String.Concat(BuildName(minn, body), ".", od.GetAnyMemberMap(mexpinner.Name).ElementName);
				}
				return String.Concat(od.GetAnyMemberMap(mexpinner.Name).ElementName);
			}
			else
			{
				throw new InvalidOperationException(String.Format("AddProperty only allows field or property access, such as 'new {{ this.Field1, this.Property }}' (found {0})", body.ToString()));
			}
		}

		/// <summary>
		/// Escape special characters in a string so it is suitable as a Mongo property name (i.e. dot and dollar and percent sign)
		/// </summary>
		/// <param name="rawKey"></param>
		/// <returns></returns>
		public static string ToMongoKey(this string rawKey)
		{
			return rawKey.Replace("%", "%25").Replace("$", "%24").Replace(".", "%2E");
		}

		/// <summary>
		/// Unescape something that was once passed through ToMongoKey
		/// </summary>
        /// <param name="mongoKey"></param>
		/// <returns></returns>
		public static string FromMongoKey(this string mongoKey)
		{
			return mongoKey.Replace("%2E", ".").Replace("%24", "$").Replace("%25", "%");
		}

		/// <summary>
		/// Get the document property "_id"
		/// </summary>
		/// <param name="d"></param>
		/// <returns></returns>
		public static object GetId(this BsonDocument d)
		{
			return d["_id"];
		}

		/// <summary>
		/// Convert a JSON string to a document.  Most useful for taking "admin user" input of a query to run
		/// and run via "real code"
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static BsonDocument ParseJsonToDocument(this string json)
		{
			var dict = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
			var document = new BsonDocument();
			ToDoc(dict, document);
			return document;
		}

		static void ToDoc(IEnumerable<KeyValuePair<string, object>> fields, BsonDocument doc)
		{
			foreach (var kv in fields)
			{
				if (kv.Value == null)
				{
					doc.Add(kv.Key, null);
				}
				else
				{
					var t = kv.Value.GetType();
					// May need to handle arrays differently
					if (t.IsValueType || t == typeof(string))
					{
						doc.Add(kv.Key, BsonValue.Create(kv.Value));
					}
					else if (kv.Value is IDictionary<string, object>)
					{
						var subdoc = new BsonDocument();
						ToDoc((IDictionary<string, object>)kv.Value, subdoc);
						doc.Add(kv.Key, subdoc);
					}
				}
			}
		}

	}
}
