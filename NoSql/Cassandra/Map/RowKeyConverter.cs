using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlienForce.NoSql.Cassandra;

namespace AlienForce.NoSql.Cassandra.Map
{
	/// <summary>
	/// A helper class to convert to and from string RowKeys
	/// </summary>
	public static class RowKeyConverter
	{
		public static Type GetRowKeyType(Type t)
		{
			return MetadataCache.EnsureMetadata(t).RowKeyType;
		}

		public static string ToRowKey(object o)
		{
			if (o == null)
			{
				throw new NullReferenceException("Null is not a valid row key.");
			}
			return ToRowKey(o.GetType(), o);
		}

		/// <summary>
		/// No need to waste space with special characters.
		/// </summary>
		/// <param name="g"></param>
		/// <returns></returns>
		static string ToString(Guid g)
		{
			return g.ToString("N");
		}

		static Guid ToGuid(string g)
		{
			return new Guid(g);
		}

		/// <summary>
		/// Convert from a typed RowKey to a string
		/// </summary>
		/// <param name="t"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		public static string ToRowKey(Type t, object o)
		{
			if (t == typeof(byte[])) { return Convert.ToBase64String((byte[])o); }
			if (t == typeof(string)) { return (string)o; }
			if (t == typeof(Guid)) { return ToString((Guid)o); }
			if (t.IsPrimitive) { return o.ToString(); }
			throw new InvalidCastException(String.Format("Don't know how to use type {0} as a row key.", t.Name));
		}

		/// <summary>
		/// Convert from a string RowKey to a typed one
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="rowKey"></param>
		/// <returns></returns>
		public static T FromRowKey<T>(string rowKey)
		{
			var t = typeof(T);
			return (T) FromRowKey(t, rowKey);
		}

		/// <summary>
		/// Convert from a string RowKey to one of type <param name="t"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="rowKey"></param>
		/// <returns></returns>
		public static object FromRowKey(Type t, string rowKey)
		{
			if (t == typeof(byte[])) { return Convert.FromBase64String(rowKey); }
			if (t == typeof(string)) { return rowKey; }
			if (t == typeof(Guid)) { return ToGuid(rowKey); }
			if (t.IsPrimitive) { return Convert.ChangeType(rowKey, t); }
			throw new InvalidCastException(String.Format("Don't know how to use type {0} as a row key.", t.Name));
		}

		public static object FromRowKeyBytes(Type t, byte[] rowKey)
		{
			if (t == typeof(byte[])) { return rowKey; }
			if (t == typeof(string)) { return Encoding.UTF8.GetString(rowKey); }
			if (t == typeof(Guid)) { return new Guid(rowKey); }
			if (t == typeof(int)) { return rowKey.ReadInt(0); }
			if (t == typeof(long)) { return rowKey.ReadLong(0); }
			if (t == typeof(short)) { return rowKey.ReadShort(0); }
			throw new InvalidCastException(String.Format("Don't know how to use type {0} as a row key.", t.Name));
		}

		/// <summary>
		/// Type determined by o.GetType()
		/// <see cref="ToBytes(Type,object)"/>
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		public static byte[] ToBytes(object o)
		{
			if (o == null)
			{
				throw new NullReferenceException("Null is not a valid row key.");
			}
			return ToBytes(o.GetType(), o);
		}

		/// <summary>
		/// Convert a typed RowKey to a byte array suitable for storing on the "foreign" side of a row reference
		/// </summary>
		/// <param name="t"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		public static byte[] ToBytes(Type t, object o)
		{
			if (t == typeof(byte[])) { return (byte[])o; }
			if (t == typeof(string)) { return ((string)o).ToNetwork(); }
			if (t == typeof(Guid)) { return ((Guid)o).ToByteArray(); }
			if (t == typeof(int)) { return ((int)o).ToNetwork(); }
			if (t == typeof(long)) { return ((long)o).ToNetwork(); }
			if (t == typeof(short)) { return ((short)o).ToNetwork(); }
			throw new InvalidCastException(String.Format("Don't know how to use type {0} as a row key.", t.Name));
		}
	}
}
