using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.Utilities.Collections
{
	/// <summary>
	/// Simple comparer to allow byte[] to be used as keys in collections.
	/// </summary>
	public sealed class ByteArrayComparer : IEqualityComparer<byte[]>
	{
		public readonly static ByteArrayComparer Default = new ByteArrayComparer();

		private ByteArrayComparer() { }

		public bool Equals(byte[] left, byte[] right)
		{
			if (left == null || right == null)
			{
				return left == right;
			}
			if (left.Length != right.Length)
			{
				return false;
			}
			for (int i = 0; i < left.Length; i++)
			{
				if (left[i] != right[i])
				{
					return false;
				}
			}
			return true;
		}

		public int GetHashCode(byte[] key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			int sum = 0;
			foreach (byte cur in key)
			{
				sum += cur;
			}
			return sum;
		}
	}
}
