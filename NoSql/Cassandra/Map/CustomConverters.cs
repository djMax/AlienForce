using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.NoSql.Cassandra.Map
{
	public class CustomConverters
	{
		/// <summary>
		/// A good converter for image sizes, simple string of WxH.
		/// </summary>
		public class SizeConverter : IByteConverter
		{
			#region IByteConverter Members

			public byte[] ToByteArray(object o)
			{
				var sz = (System.Drawing.Size) o;
				return Encoding.ASCII.GetBytes(String.Format("{0}x{1}", sz.Width, sz.Height));
			}

			public object ToObject(byte[] b)
			{
				string[] wbyh = Encoding.ASCII.GetString(b).Split('x');
				return new System.Drawing.Size(int.Parse(wbyh[0]), int.Parse(wbyh[1]));
			}

			#endregion
		}
	}
}
