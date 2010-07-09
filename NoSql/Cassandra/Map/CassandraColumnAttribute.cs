using System;
using System.Text;

namespace AlienForce.NoSql.Cassandra.Map
{
	[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
	public class CassandraColumnAttribute : System.Attribute
	{
		/// <summary>
		/// If this entity is stored using composite keys (e.g. customerid + "_Profile", add this to the RowKey to get this field.
		/// (and thus we'd use multiget_slice)
		/// </summary>
		public string CompositeKeySuffix { get; set; }

		public byte[] SuperColumnNameBytes { get; set; }
		public byte[] ColumnNameBytes { get; set; }
		public Type Converter { get; set; }
		/// <summary>
		/// When set to true, AND when super column name is set, the column type is a keyed collection type (e.g. Dictionary)
		/// and we should interpret any column under it as an instance of a type (Converter now operates on those)
		/// </summary>
		public bool ReadAllValues { get; set; }

		public CassandraColumnAttribute(string name, Type converter)
		{
			ColumnNameBytes = Encoding.UTF8.GetBytes(name);
			Converter = converter;
		}

		public CassandraColumnAttribute(string superName, string name, Type converter) : this(name, converter)
		{
			SuperColumnNameBytes = Encoding.UTF8.GetBytes(superName);
		}

		/// <summary>
		/// Drive the conversion off the type of the property
		/// </summary>
		/// <param name="name"></param>
		public CassandraColumnAttribute(string name)
		{
			ColumnNameBytes = Encoding.UTF8.GetBytes(name);
		}

		/// <summary>
		/// Drive the conversion off the type of the property
		/// </summary>
		/// <param name="name"></param>
		public CassandraColumnAttribute(string superName, string name) : this(name)
		{
			SuperColumnNameBytes = Encoding.UTF8.GetBytes(superName);
		}

		/// <summary>
		/// Drive the attribute name and conversion off the property/field declaration
		/// </summary>
		public CassandraColumnAttribute()
		{
		}

		public string SuperColumnName
		{
			set { SuperColumnNameBytes = Encoding.UTF8.GetBytes(value); }
			get { return Encoding.UTF8.GetString(SuperColumnNameBytes); }
		}
	}
}
