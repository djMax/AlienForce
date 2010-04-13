using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace AlienForce.Utilities.Db
{
	public class DbConnectionReader : System.Data.Common.DbDataReader, IDisposable
	{
		private DbConnectionBase mConnection = null;
		private DbConnectionReader mReader;

		public override void Close()
		{
			mReader.Close();
		}

		public override int Depth
		{
			get { return mReader.Depth; }
		}

		public override int FieldCount
		{
			get { return mReader.FieldCount; }
		}

		public override bool GetBoolean(int ordinal)
		{
			return mReader.GetBoolean(ordinal);
		}

		public override byte GetByte(int ordinal)
		{
			return mReader.GetByte(ordinal);
		}

		public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
		{
			return mReader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
		}

		public override char GetChar(int ordinal)
		{
			return mReader.GetChar(ordinal);
		}

		public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
		{
			return mReader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
		}

		public override string GetDataTypeName(int ordinal)
		{
			return mReader.GetDataTypeName(ordinal);
		}

		public override DateTime GetDateTime(int ordinal)
		{
			return mReader.GetDateTime(ordinal);
		}

		public override decimal GetDecimal(int ordinal)
		{
			return mReader.GetDecimal(ordinal);
		}

		public override double GetDouble(int ordinal)
		{
			return mReader.GetDouble(ordinal);
		}

		public override System.Collections.IEnumerator GetEnumerator()
		{
			return mReader.GetEnumerator();
		}

		public override Type GetFieldType(int ordinal)
		{
			return mReader.GetFieldType(ordinal);
		}

		public override float GetFloat(int ordinal)
		{
			return mReader.GetFloat(ordinal);
		}

		public override Guid GetGuid(int ordinal)
		{
			return mReader.GetGuid(ordinal);
		}

		public override short GetInt16(int ordinal)
		{
			return mReader.GetInt16(ordinal);
		}

		public override int GetInt32(int ordinal)
		{
			return mReader.GetInt32(ordinal);
		}

		public override long GetInt64(int ordinal)
		{
			return mReader.GetInt64(ordinal);
		}

		public override string GetName(int ordinal)
		{
			return mReader.GetName(ordinal);
		}

		public override int GetOrdinal(string name)
		{
			return mReader.GetOrdinal(name);
		}

		public override System.Data.DataTable GetSchemaTable()
		{
			return mReader.GetSchemaTable();
		}

		public override string GetString(int ordinal)
		{
			return mReader.GetString(ordinal);
		}

		public override object GetValue(int ordinal)
		{
			return mReader.GetValue(ordinal);
		}

		public override int GetValues(object[] values)
		{
			return mReader.GetValues(values);
		}

		public override bool HasRows
		{
			get { return mReader.HasRows; }
		}

		public override bool IsClosed
		{
			get { return mReader.IsClosed; }
		}

		public override bool IsDBNull(int ordinal)
		{
			return mReader.IsDBNull(ordinal);
		}

		public override bool NextResult()
		{
			return mReader.NextResult();
		}

		public override bool Read()
		{
			return mReader.Read();
		}

		public override int RecordsAffected
		{
			get { return mReader.RecordsAffected; }
		}

		public override object this[string name]
		{
			get { return mReader[name]; }
		}

		public override object this[int ordinal]
		{
			get { return mReader[ordinal];  }
		}

		#region IDisposable Members

		void IDisposable.Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Perform disposal like FxCop wants us to.
		/// </summary>
		/// <param name="disposing">if set to <c>true</c> [disposing].</param>
		protected virtual void Dispose(bool disposing)
		{
			try
			{
				mReader.Dispose();
				base.Dispose();
			}
			finally
			{
				if (!mConnection.InTransaction)
				{
					mConnection.Close();
				}
			}
		}

		#endregion
	}
}
