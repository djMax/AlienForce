using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using AlienForce.Utilities.Collections;
using System.Drawing;

namespace AlienForce.NoSql.Cassandra.Map
{
	internal static class MetadataCache
	{
		static ThreadSafeDictionary<Type, Metadata> Map = new ThreadSafeDictionary<Type, Metadata>();
		static ThreadSafeDictionary<Type, ConstructorInfo> RowRefConstructors = new ThreadSafeDictionary<Type, ConstructorInfo>();

		internal static Metadata EnsureMetadata(Type thisType)
		{
			Metadata map;
			if (!Map.TryGetValue(thisType, out map))
			{
				lock (Map)
				{
					if (!Map.TryGetValue(thisType, out map))
					{
						map = Map[thisType] = new Metadata();
						Discover(thisType, map);
					}
				}
			}
			return map;
		}

		#region The big one - metadata discovery
		static void Discover(Type t, Metadata info)
		{
			var ceAtt = GetAttribute<CassandraEntityAttribute>(t);
			if (ceAtt != null)
			{
				info.HasSuperColumnId = ceAtt.HasSuperColumnId;
			}

			// Rowkey type is it's own thing
			var baseType = t.BaseType;
			while (baseType.GetGenericTypeDefinition() != typeof(CassandraEntity<>) && baseType != typeof(object))
			{
				baseType = t.BaseType;
			}
			if (baseType == typeof(object))
			{
				throw new InvalidCastException("Classes used for Cassandra entities must derive from CassandraEntity at some point in their hierarchy");
			}
			info.RowKeyType = baseType.GetGenericArguments()[0];

			foreach (MemberInfo mi in GetRelevant(t))
			{
				// Find out if this is a mapped property
				var att = GetAttribute<CassandraColumnAttribute>(mi);
				if (att != null)
				{
					var memberInfo = new CassandraMember(mi, att.SuperColumnNameBytes, att.ColumnNameBytes, GetConverter(GetMemberType(mi), att.Converter, t, mi.Name));
					if (att.SuperColumnNameBytes != null)
					{
						if (info.HasSuperColumnId) { throw new InvalidOperationException("You cannot assign a field or property a super column name in an entity that uses super column names for identifiers."); }
						if (info.Super == null) { info.Super = new Dictionary<byte[], Dictionary<byte[], CassandraMember>>(ByteArrayComparer.Default); }
						Dictionary<byte[], CassandraMember> cInfo;
						if (!info.Super.TryGetValue(att.SuperColumnNameBytes, out cInfo))
						{
							info.Super[att.SuperColumnNameBytes] = cInfo = new Dictionary<byte[], CassandraMember>(ByteArrayComparer.Default);
						}
						cInfo[att.ColumnNameBytes] = memberInfo;
					}
					else
					{
						if (info.Columns == null) { info.Columns = new Dictionary<byte[], CassandraMember>(ByteArrayComparer.Default); }
						info.Columns[att.ColumnNameBytes ?? mi.Name.ToCassandra()] = new CassandraMember(mi, null, att.ColumnNameBytes, GetConverter(GetMemberType(mi), att.Converter, t, mi.Name));
					}
				}
				else
				{
					var refAtt = GetAttribute<CassandraRowAttribute>(mi);
					if (refAtt != null)
					{
						var rowRefType = typeof(string);
						if (GetMemberType(mi).GetGenericTypeDefinition() == typeof(CassandraRowReference<>))
						{
							rowRefType = GetMemberType(mi).GetGenericArguments()[0];
						}
						var memberInfo = new CassandraMember(mi, refAtt.SuperColumnNameBytes, refAtt.ColumnNameBytes ?? mi.Name.ToCassandra(), refAtt, rowRefType);
						if (refAtt.SuperColumnNameBytes == null)
						{
							if (info.Columns == null) { info.Columns = new Dictionary<byte[], CassandraMember>(ByteArrayComparer.Default); }
							info.Columns[memberInfo.CassandraName] = memberInfo;
						}
						else
						{
							if (info.HasSuperColumnId) { throw new InvalidOperationException("You cannot assign a field or property a super column name in an entity that uses super column names for identifiers."); }
							if (info.Super == null) { info.Super = new Dictionary<byte[], Dictionary<byte[], CassandraMember>>(ByteArrayComparer.Default); }
							Dictionary<byte[], CassandraMember> cInfo;
							if (!info.Super.TryGetValue(refAtt.SuperColumnNameBytes, out cInfo))
							{
								info.Super[refAtt.SuperColumnNameBytes] = cInfo = new Dictionary<byte[], CassandraMember>(ByteArrayComparer.Default);
							}
							cInfo[memberInfo.CassandraName] = memberInfo;
						}
					}
				}
			}
		}
		#endregion

		#region Private type reflection helpers
		static IByteConverter GetConverter(Type destinationType, Type converterType, Type baseType, string propOrFieldName)
		{
			if (converterType != null)
			{
				return (IByteConverter)converterType.GetConstructor(Type.EmptyTypes).Invoke(null);
			}
			if (destinationType == typeof(string))
			{
				return StandardConverters.StringConverter.Default;
			}
			else if (destinationType == typeof(int))
			{
				return StandardConverters.IntConverter.Default;
			}
			else if (destinationType == typeof(long))
			{
				return StandardConverters.LongConverter.Default;
			}
			else if (destinationType == typeof(byte[]))
			{
				return StandardConverters.NullConverter.Default;
			}
			else if (destinationType == typeof(Bitmap))
			{
				return StandardConverters.BitmapConverter.Default;
			}
			else if (destinationType == typeof(decimal))
			{
				return StandardConverters.DecimalConverter.Default;
			}
			else if (destinationType == typeof(short))
			{
				return StandardConverters.ShortConverter.Default;
			}
			else if (destinationType == typeof(byte))
			{
				return StandardConverters.ByteConverter.Default;
			}
			else if (destinationType.IsEnum)
			{
				return GetConverter(destinationType.BaseType, null, baseType, propOrFieldName);
			}
			throw new InvalidCastException(String.Format("There is no converter specified for {0}.{1} and no default converters are available for {2}.", baseType.Name, propOrFieldName, destinationType.Name));
		}

		static T GetAttribute<T>(MemberInfo p)
		{
			return p.GetCustomAttributes(typeof(T), false).Cast<T>().FirstOrDefault<T>();
		}

		static T GetAttribute<T>(Type t)
		{
			return t.GetCustomAttributes(typeof(T), false).Cast<T>().FirstOrDefault<T>();
		}

		static IEnumerable<MemberInfo> GetRelevant(Type t)
		{
			foreach (PropertyInfo pi in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)) { yield return pi; }
			foreach (FieldInfo fi in t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)) { yield return fi; }
		}

		static Type GetMemberType(MemberInfo mi)
		{
			PropertyInfo pi = mi as PropertyInfo;
			if (pi != null) { return pi.PropertyType; } else { return ((FieldInfo)mi).FieldType; }
		}
		#endregion

		#region Mapping information class/helpers
		internal class CassandraMember
		{
			public CassandraMember(MemberInfo m, byte[] superColumn, byte[] column, IByteConverter converter)
			{
				Member = m; SuperColumnCassandraName = superColumn; CassandraName = column; Converter = converter;
			}

			public CassandraMember(MemberInfo m, byte[] superColumn, byte[] column, CassandraRowAttribute rowRef, Type targetRowKeyType)
			{
				Member = m; SuperColumnCassandraName = superColumn; CassandraName = column;
				RowReference = new KeyValuePair<CassandraRowAttribute, Type>(rowRef, targetRowKeyType);
			}

			/// <summary>
			/// The field or property info object
			/// </summary>
			public MemberInfo Member;
			/// <summary>
			/// If this is part of a super-column, what's the name of that sucker?
			/// </summary>
			public byte[] SuperColumnCassandraName;
			/// <summary>
			/// The name of the Cassandra column this property comes from
			/// </summary>
			public byte[] CassandraName;
			/// <summary>
			/// The class responsible for converting to and from byte arrays, if this is not a row reference
			/// </summary>
			public IByteConverter Converter;
			/// <summary>
			/// The original attribute and the discovered type of the target entity
			/// </summary>
			public KeyValuePair<CassandraRowAttribute, Type> RowReference;

			public byte[] GetValueFromObject(object thisObject)
			{
				object thisValue;
				PropertyInfo pi = Member as PropertyInfo;
				if (pi != null)
				{
					thisValue = pi.GetValue(thisObject, null);
				}
				else
				{
					thisValue = ((FieldInfo)Member).GetValue(thisObject);
				}
				if (Converter != null)
				{
					return Converter.ToByteArray(thisValue);
				}
				// row reference - figure out the type and then convert it to bytes appropriately
				if (thisValue == null) { return null; } // empty row reference, don't save

				if (thisValue is ICassandraRowReference)
				{
					return ((ICassandraRowReference)thisValue).RowKeyForReference;
				}

				return RowKeyConverter.ToBytes(RowKeyConverter.GetRowKeyType(RowReference.Value), thisValue);
			}

			public object GetValueFromCassandra(Column cs)
			{
				if (Converter != null)
				{
					return Converter.ToObject(cs.Value);
				}
				ConstructorInfo ci;
				if (!RowRefConstructors.TryGetValue(RowReference.Value, out ci))
				{
					var rr = new Type[] { RowReference.Value };
					RowRefConstructors[RowReference.Value] = ci = typeof(CassandraRowReference<>).MakeGenericType(rr).GetConstructor(new Type[] { typeof(string) });
				}
				// TODO skip the intermediate object on the way to a string row key
				return ci.Invoke(new object[] { RowKeyConverter.ToRowKey(RowKeyConverter.FromRowKeyBytes(RowKeyConverter.GetRowKeyType(RowReference.Value), cs.Value)) });
			}

			public void SetValueFromCassandra(object thisObject, Column cs)
			{
				PropertyInfo pi = Member as PropertyInfo;
				if (pi != null)
				{
					pi.SetValue(thisObject, GetValueFromCassandra(cs), null);
				}
				else
				{
					((FieldInfo)Member).SetValue(thisObject, GetValueFromCassandra(cs));
				}
			}
		}

		internal class Metadata
		{
			public Type RowKeyType;
			public bool HasSuperColumnId;
			public Dictionary<byte[], CassandraMember> Columns = null;
			public Dictionary<byte[], Dictionary<byte[], CassandraMember>> Super = null;
		}
		#endregion
	
	}
}
