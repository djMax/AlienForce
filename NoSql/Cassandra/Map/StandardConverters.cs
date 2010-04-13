using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using AlienForce.NoSql.Cassandra;

namespace AlienForce.NoSql.Cassandra.Map
{
	public class StandardConverters
	{
		public sealed class NullConverter : IByteConverter
		{
			public static NullConverter Default = new NullConverter();
			public byte[] ToByteArray(object o) { return (byte[])o; }
			public object ToObject(byte[] b) { return b; }
		}

		public sealed class StringConverter : IByteConverter
		{
			public static StringConverter Default = new StringConverter();
			public byte[] ToByteArray(object o) { return Encoding.UTF8.GetBytes((string)o); }
			public object ToObject(byte[] b) { return Encoding.UTF8.GetString(b); }
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
			public byte[] ToByteArray(object o) { return ((int)o).ToCassandra(); }
			public object ToObject(byte[] b) { return b.ReadInt(0); }
		}

		public sealed class LongConverter : IByteConverter
		{
			public static LongConverter Default = new LongConverter();
			public byte[] ToByteArray(object o) { return ((long)o).ToCassandra(); }
			public object ToObject(byte[] b) { return b.ReadLong(0); }
		}

		public sealed class ShortConverter : IByteConverter
		{
			public static ShortConverter Default = new ShortConverter();
			public byte[] ToByteArray(object o) { return ((short)o).ToCassandra(); }
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
				using (var ms = new System.IO.MemoryStream(b))
				{
					return Bitmap.FromStream(ms);
				}
			}
		}
	}
}
