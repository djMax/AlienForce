using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.NoSql.Cassandra
{
	public static class ThriftByteOperations
	{
		public static byte[] ToNetwork(this long i64)
		{
			return new byte[]
			{
				(byte)(0xff & (i64 >> 56)),
				(byte)(0xff & (i64 >> 48)),
				(byte)(0xff & (i64 >> 40)),
				(byte)(0xff & (i64 >> 32)),
				(byte)(0xff & (i64 >> 24)),
				(byte)(0xff & (i64 >> 16)),
				(byte)(0xff & (i64 >> 8)),
				(byte)(0xff & i64)
			};
		}

		public static byte[] ToNetwork(this short s)
		{
			return new byte[]
			{
				(byte)(0xff & (s >> 8)),
				(byte)(0xff & s)
			};
		}

		public static byte[] ToNetwork(this int i32)
		{
			return new byte[]
			{
				(byte)(0xff & (i32 >> 24)),
				(byte)(0xff & (i32 >> 16)),
				(byte)(0xff & (i32 >> 8)),
				(byte)(0xff & i32)
			};
		}

		public static byte[] ToNetwork(this string s)
		{
			return Encoding.UTF8.GetBytes(s);
		}

		public static long ReadLong(this byte[] b, int offset) 
		{
			return (long)(((long)(b[offset] & 0xff) << 56) | ((long)(b[offset + 1] & 0xff) << 48) |
				((long)(b[offset + 2] & 0xff) << 40) | ((long)(b[offset + 3] & 0xff) << 32) |
				((long)(b[offset + 4] & 0xff) << 24) | ((long)(b[offset + 5] & 0xff) << 16) |
				((long)(b[offset + 6] & 0xff) << 8) | ((long)(b[offset + 7] & 0xff)));
		}

		public static int ReadInt(this byte[] b, int offset) 
		{
			return (int)(((b[offset] & 0xff) << 24) | ((b[offset + 1] & 0xff) << 16) | ((b[offset + 2] & 0xff) << 8) | ((b[offset + 3] & 0xff)));
		}

		public static short ReadShort(this byte[] b, int offset) 
		{
			return (short)(((b[offset] & 0xff) << 8) | ((b[offset + 1] & 0xff)));
		}

	}
}
