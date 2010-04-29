
namespace AlienForce.NoSql.Cassandra.Map
{
	public interface ICassandraRowReference
	{
		string RowKeyString { get; }
		byte[] RowKeyForReference { get; }
		bool EnsureValue(PooledClient c);
	}

	/// <summary>
	/// A reference to another Cassandra entity, which stores a key
	/// and then lazily loads a value.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class CassandraRowReference<ValueType> : ICassandraRowReference
		where ValueType : ICassandraEntity, new()
	{
		ValueType _V;
		bool _Loaded;

		public string RowKeyString { get; private set; }
		public byte[] RowKeyForReference
		{
			get
			{
				if (_Loaded) { return _V.RowKeyForReference; }
				var targetType = RowKeyConverter.GetRowKeyType(typeof(ValueType));
				return RowKeyConverter.ToBytes(targetType, RowKeyConverter.FromRowKey(targetType, RowKeyString));
			}
		}
		
		public CassandraRowReference(string rowKey)
		{
			RowKeyString = rowKey;
		}

		public CassandraRowReference(ValueType v)
		{
			_V = v;
			_Loaded = true;
			RowKeyString = v.RowKeyString;
		}

		public ValueType Value
		{
			get
			{
				if (!_Loaded)
				{
					throw new System.InvalidOperationException("Cassandra value not yet loaded.");
				}
				return _V;
			}
		}

		public bool EnsureValue(PooledClient c)
		{
			if (_Loaded) { return false; }
			_V = c.SelectByRowKey<ValueType>(RowKeyString);
			_Loaded = true;
			return true;
		}

		public static implicit operator CassandraRowReference<ValueType>(ValueType item)
		{
			return new CassandraRowReference<ValueType>(item.RowKeyString);
		}
	}
}
