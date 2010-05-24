using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using AlienForce.NoSql.Cassandra;
using AlienForce.Utilities.Collections;

namespace AlienForce.NoSql.Cassandra.Map
{
	public static class StandardConverters
	{
		static ThreadSafeDictionary<Type, IByteConverter> _Converters = new ThreadSafeDictionary<Type,IByteConverter>();

		public static Func<Type, IByteConverter> UnknownTypeHandler;

		public static IByteConverter GetConverter(Type t)
		{
			IByteConverter c;
			if (!_Converters.TryGetValue(t, out c) || c == null)
			{
				if (t.IsEnum)
				{
					return GetConverter(Enum.GetUnderlyingType(t));
				}

				if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					c = GetConverter(t.GetGenericArguments()[0]);
					if (c != null)
					{
						return new StandardConverters.NullableTypeConverter(c);
					}
				}

				if (UnknownTypeHandler != null)
				{
					c = UnknownTypeHandler(t);
				}
			}
			return c;
		}

		public static void RegisterConverter(Type t, IByteConverter c)
		{
			_Converters[t] = c;
		}

		static StandardConverters()
		{
			_Converters[typeof(string)] = StringConverter.Default;
			_Converters[typeof(int)] = IntConverter.Default;
			_Converters[typeof(long)] = LongConverter.Default;
			_Converters[typeof(byte[])] = NullConverter.Default;
			_Converters[typeof(Bitmap)] = BitmapConverter.Default;
			_Converters[typeof(decimal)] = DecimalConverter.Default;
			_Converters[typeof(short)] = ShortConverter.Default;
			_Converters[typeof(byte)] = ByteConverter.Default;
			_Converters[typeof(DateTime)] = DateTimeConverter.Default;
			_Converters[typeof(Guid)] = GuidConverter.Default;
		}

		public sealed class NullConverter : IByteConverter
		{
			public static NullConverter Default = new NullConverter();
			public byte[] ToByteArray(object o) { return (byte[])o; }
			public object ToObject(byte[] b) { return b; }
		}

		public sealed class GuidConverter : IByteConverter
		{
			public static GuidConverter Default = new GuidConverter();
			public byte[] ToByteArray(object o) { return (o != null && ((Guid)o) != Guid.Empty) ? ((Guid)o).ToByteArray() : null; }
			public object ToObject(byte[] b) { return b != null ? new Guid(b) : Guid.Empty; }
		}

		public sealed class DateTimeConverter : IByteConverter
		{
			public static DateTimeConverter Default = new DateTimeConverter();
			public byte[] ToByteArray(object o) 
			{
				if (o == null) { return null; }
				if (o is DateTime)
				{
					DateTime dt = (DateTime)o;
					if (dt == default(DateTime))
					{
						return null;
					}
				}
				return LongConverter.Default.ToByteArray(((DateTime)o).Ticks); 
			}
			public object ToObject(byte[] b) 
			{
				return b != null ? new DateTime((long)LongConverter.Default.ToObject(b)) : default(DateTime); 
			}
		}

		public sealed class NullableTypeConverter : IByteConverter
		{
			private IByteConverter BaseConverter;

			public NullableTypeConverter(IByteConverter converter)
			{
				BaseConverter = converter;
			}

			public byte[] ToByteArray(object o) { return o != null ? BaseConverter.ToByteArray(o) : null; }
			public object ToObject(byte[] b) { return b != null ? BaseConverter.ToObject(b) : null; }
		}

		public sealed class StringConverter : IByteConverter
		{
			public static StringConverter Default = new StringConverter();
			public byte[] ToByteArray(object o) { return o != null ? Encoding.UTF8.GetBytes((string)o) : null; }
			public object ToObject(byte[] b) { return b != null ? Encoding.UTF8.GetString(b) : null; }
		}

		public sealed class DecimalConverter : IByteConverter
		{
			public static DecimalConverter Default = new DecimalConverter();
			public byte[] ToByteArray(object o) { return Encoding.UTF8.GetBytes(((decimal)o).ToString()); }
			public object ToObject(byte[] b) { return decimal.Parse(Encoding.UTF8.GetString(b)); }
		}

		public sealed class IntConverter : IByteConverter
		{
			public static IntConverter Default = new IntConverter();
			public byte[] ToByteArray(object o) { return ((int)o).ToNetwork(); }
			public object ToObject(byte[] b) { return b.ReadInt(0); }
		}

		public sealed class LongConverter : IByteConverter
		{
			public static LongConverter Default = new LongConverter();
			public byte[] ToByteArray(object o) { return ((long)o).ToNetwork(); }
			public object ToObject(byte[] b) { return b.ReadLong(0); }
		}

		public sealed class ShortConverter : IByteConverter
		{
			public static ShortConverter Default = new ShortConverter();
			public byte[] ToByteArray(object o) { return ((short)o).ToNetwork(); }
			public object ToObject(byte[] b) { return b.ReadShort(0); }
		}

		public sealed class ByteConverter : IByteConverter
		{
			public static ByteConverter Default = new ByteConverter();
			public byte[] ToByteArray(object o) { return new byte[1] { (byte)o }; }
			public object ToObject(byte[] b) { return b[0]; }
		}

		public sealed class BitmapConverter : IByteConverter
		{
			public static BitmapConverter Default = new BitmapConverter();
			public byte[] ToByteArray(object o) 
			{
				Bitmap bmp = (Bitmap)o;
				using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
				{
					bmp.Save(ms, bmp.RawFormat);
					return ms.ToArray();
				}
			}
			public object ToObject(byte[] b) 
			{
				var ms = new System.IO.MemoryStream(b);
				// Bitmap needs the stream to stay open
				return Bitmap.FromStream(ms);
			}
		}
	}
}
