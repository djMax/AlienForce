﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using AlienForce.Utilities.Collections;
using System.Drawing;
using Apache.Cassandra060;

namespace AlienForce.NoSql.Cassandra.Map
{
	internal static class MetadataCache
	{
		static ThreadSafeDictionary<Type, Metadata> Map = new ThreadSafeDictionary<Type, Metadata>();
		static ThreadSafeDictionary<Type, ConstructorInfo> RowRefConstructors = new ThreadSafeDictionary<Type, ConstructorInfo>();

		internal static IEnumerable<Type> GetKnownTypes()
		{
			return Map.Keys;
		}

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
				info.DefaultColumnFamily = ceAtt.ColumnFamily;
				info.DefaultKeyspace = ceAtt.Keyspace;
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
			info.WithRowKey = t.GetConstructor(new Type[] { info.RowKeyType });
			info.WithRowKeyAndSuperColumnName = t.GetConstructor(new Type[] { info.RowKeyType, typeof(byte[]) });

			foreach (MemberInfo mi in GetRelevant(t))
			{
				// Find out if this is a mapped property
				var att = GetAttribute<CassandraColumnAttribute>(mi);
				if (att != null)
				{
					if (att.CompositeKeySuffix != null)
					{
						info.UsesCompositeKeys = true;
						if (info.CompositeSuffixes == null) { info.CompositeSuffixes = new List<string>(); }
						if (!info.CompositeSuffixes.Contains(att.CompositeKeySuffix)) { info.CompositeSuffixes.Add(att.CompositeKeySuffix); }
					}
					var cname = att.ColumnNameBytes ?? mi.Name.ToNetwork();
					var cnamespec = new ColumnNameSpec(att.SuperColumnNameBytes != null ? null : att.CompositeKeySuffix, cname);
					var memType = GetMemberType(mi, att.SuperColumnNameBytes != null && att.ReadAllValues);
					var memberInfo = new CassandraMember(mi, att.SuperColumnNameBytes, cname, GetConverter(memType, att.Converter, t, mi.Name));
					// If there's a composite key suffix AND a super column name, the suffix only applies to the super column.  Not sure how this plays out.
					memberInfo.CompositeKeySuffix = att.SuperColumnNameBytes != null ? null : att.CompositeKeySuffix;
					if (att.SuperColumnNameBytes != null)
					{
						if (info.HasSuperColumnId) { throw new InvalidOperationException("You cannot assign a field or property a super column name in an entity that uses super column names for identifiers."); }
						if (info.Super == null) { info.Super = new Dictionary<ColumnNameSpec, Dictionary<ColumnNameSpec, CassandraMember>>(ColumnNameComparer.Default); }
						Dictionary<ColumnNameSpec, CassandraMember> cInfo;
						var scnspec = new ColumnNameSpec(att.CompositeKeySuffix, att.SuperColumnNameBytes);
						if (!info.Super.TryGetValue(scnspec, out cInfo))
						{
							info.Super[scnspec] = cInfo = new Dictionary<ColumnNameSpec, CassandraMember>(ColumnNameComparer.Default);
						}
						if (att.ReadAllValues)
						{
							memberInfo.ReadAll = true;
							cInfo[Metadata.ReadAllSuperKey] = memberInfo;
						}
						else
						{
							cInfo[cnamespec] = memberInfo;
						}
					}
					else
					{
						if (info.Columns == null) { info.Columns = new Dictionary<ColumnNameSpec, CassandraMember>(ColumnNameComparer.Default); }
						var cm = new CassandraMember(mi, null, cname, GetConverter(GetMemberType(mi), att.Converter, t, mi.Name));
						cm.CompositeKeySuffix = att.CompositeKeySuffix;
						if (memberInfo.CompositeKeySuffix != null && att.ReadAllValues)
						{
							cm.ReadAll = true;
							cnamespec.Name = Metadata.ReadAllSuperKey.Name;
						}
						info.Columns[cnamespec] = cm;
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
						var memberInfo = new CassandraMember(mi, refAtt.SuperColumnNameBytes, refAtt.ColumnNameBytes ?? mi.Name.ToNetwork(), refAtt, rowRefType);
						if (refAtt.SuperColumnNameBytes == null)
						{
							if (info.Columns == null) { info.Columns = new Dictionary<ColumnNameSpec, CassandraMember>(ColumnNameComparer.Default); }
							info.Columns[new ColumnNameSpec(null, memberInfo.CassandraName)] = memberInfo;
						}
						else
						{
							if (info.HasSuperColumnId) { throw new InvalidOperationException("You cannot assign a field or property a super column name in an entity that uses super column names for identifiers."); }
							if (info.Super == null) { info.Super = new Dictionary<ColumnNameSpec, Dictionary<ColumnNameSpec, CassandraMember>>(ColumnNameComparer.Default); }
							Dictionary<ColumnNameSpec, CassandraMember> cInfo;
							if (!info.Super.TryGetValue(new ColumnNameSpec(null, refAtt.SuperColumnNameBytes), out cInfo))
							{
								info.Super[new ColumnNameSpec(null, refAtt.SuperColumnNameBytes)] = cInfo = new Dictionary<ColumnNameSpec, CassandraMember>(ColumnNameComparer.Default);
							}
							cInfo[new ColumnNameSpec(null, memberInfo.CassandraName)] = memberInfo;
						}
					}
					else
					{
						var cIncAtt = GetAttribute<CassandraIncludeAttribute>(mi);
						if (cIncAtt != null)
						{
							if (info.Includes == null) { info.Includes = new List<MemberInfo>(); }
							info.Includes.Add(mi);
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

			IByteConverter c = StandardConverters.GetConverter(destinationType);
			if (c == null)
			{
				throw new InvalidCastException(String.Format("There is no converter specified for {0}.{1} and no default converters are available for {2}.", baseType.Name, propOrFieldName, destinationType.Name));
			}
			return c;
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

		static Type GetMemberType(MemberInfo mi, bool isReadAll)
		{
			Type t = GetMemberType(mi);
			if (isReadAll)
			{
				if (!t.IsGenericType)
				{
					throw new Exception("Cassandra Columns with ReadAll set must be Dictionary<K,V>");
				}
				var ga = t.GetGenericArguments();
				if (ga.Length != 2)
				{
					if (!t.IsGenericType)
					{
						throw new Exception("Cassandra Columns with ReadAll set must be Dictionary<K,V>");
					}
				}
				return t.GetGenericArguments()[1];
			}
			return t;
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

			public string GetRowKey(string rowKey)
			{
				if (CompositeKeySuffix == null)
				{
					return rowKey;
				}
				return String.Concat(rowKey, "_", CompositeKeySuffix);
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
			/// <summary>
			/// True if this is a special collection type
			/// </summary>
			public bool ReadAll;

			/// <summary>
			/// Suffix on row key for composite keys
			/// </summary>
			public string CompositeKeySuffix;

			public Type Type
			{
				get
				{
					PropertyInfo pi = Member as PropertyInfo;
					return pi != null ? pi.PropertyType : ((FieldInfo)Member).FieldType;
				}
			}

			public byte[] GetBytesFromObject(object thisObject)
			{
				return ConvertValue(RawValue(thisObject));
			}

			public object RawValue(object thisObject)
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
				return thisValue;
			}

			public byte[] ConvertValue(object thisValue)
			{
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
				if (this.ReadAll)
				{
					// read into a dictionary
					var dict = RawValue(thisObject);
					var kvTypes = Type.GetGenericArguments();
					IByteConverter keyConv = StandardConverters.GetConverter(kvTypes[0]);
					if (dict == null)
					{
						dict = Type.GetConstructor(Type.EmptyTypes).Invoke(null);
						SetRawValue(thisObject, dict);
					}
					var asDict = dict as System.Collections.IDictionary;
					if (asDict != null)
					{
						asDict[keyConv.ToObject(cs.Name)] = GetValueFromCassandra(cs);
					}

				}
				else
				{
					SetRawValue(thisObject, GetValueFromCassandra(cs));
				}
			}

			public void SetRawValue(object thisObject, object value)
			{
				PropertyInfo pi = Member as PropertyInfo;
				if (pi != null)
				{
					pi.SetValue(thisObject, value, null);
				}
				else
				{
					((FieldInfo)Member).SetValue(thisObject, value);
				}
			}
		}

		private sealed class ColumnNameComparer : IEqualityComparer<ColumnNameSpec>
		{
			public static ColumnNameComparer Default = new ColumnNameComparer();
			public bool Equals(ColumnNameSpec left, ColumnNameSpec right)
			{
				return left.Equals(right);
			}

			public int GetHashCode(ColumnNameSpec key)
			{
				return key.GetHashCode();
			}
		}

		internal class ColumnNameSpec : IEquatable<ColumnNameSpec>
		{
			public ColumnNameSpec(string ck, byte[] name)
			{
				Name = name;
				CompositeKeySuffix = ck;
			}

			public byte[] Name;
			public string CompositeKeySuffix;

			public override int GetHashCode()
			{
				if (CompositeKeySuffix != null)
				{
					return CompositeKeySuffix.GetHashCode() | ByteArrayComparer.Default.GetHashCode(Name);
				}
				return ByteArrayComparer.Default.GetHashCode(Name);
			}

			#region IEquatable<ColumnNameSpec> Members

			public bool Equals(ColumnNameSpec other)
			{
				return String.Equals(this.CompositeKeySuffix, other.CompositeKeySuffix) && ByteArrayComparer.Default.Equals(this.Name, other.Name);
			}

			#endregion
		}

		internal class Metadata
		{
			public static ColumnNameSpec ReadAllSuperKey = new ColumnNameSpec(null, new byte[0]);

			public Type RowKeyType;
			public string DefaultColumnFamily;
			public string DefaultKeyspace;
			public bool HasSuperColumnId;
			public bool UsesCompositeKeys;

			public Dictionary<ColumnNameSpec, CassandraMember> Columns;
			public Dictionary<ColumnNameSpec, Dictionary<ColumnNameSpec, CassandraMember>> Super;
			public List<MemberInfo> Includes;
			public List<string> CompositeSuffixes;

			public ConstructorInfo WithRowKey;
			public ConstructorInfo WithRowKeyAndSuperColumnName;

			public List<string> CompositeKeys(string rowKey)
			{
				List<string> ret = new List<string>();
				foreach (var s in CompositeSuffixes)
				{
					ret.Add(String.Concat(rowKey, "_", s));
				}
				return ret;
			}
		}
		#endregion
	
	}
}
