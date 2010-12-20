using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AlienForce.Utilities.Threading;

namespace AlienForce.Utilities.Collections
{
	public interface IThreadSafeDictionary<TKey, TValue> : IDictionary<TKey, TValue>
	{
		/// <summary>
		/// Merge is similar to the SQL merge or upsert statement.  
		/// </summary>
		/// <param name="key">Key to lookup</param>
		/// <param name="newValue">New Value</param>
		void MergeSafe(TKey key, TValue newValue);


		/// <summary>
		/// This is a blind remove. Prevents the need to check for existence first.
		/// </summary>
		/// <param name="key">Key to Remove</param>
		void RemoveSafe(TKey key);
	}


	[Serializable]
	public class ThreadSafeDictionary<TKey, TValue> : IThreadSafeDictionary<TKey, TValue>
	{
		//This is the internal dictionary that we are wrapping
	    readonly IDictionary<TKey, TValue> _dict = new Dictionary<TKey, TValue>();


		[NonSerialized] readonly ReaderWriterLockSlim _dictionaryLock = ReaderWriterLocks.GetLockInstance(LockRecursionPolicy.NoRecursion); //setup the lock;


		/// <summary>
		/// This is a blind remove. Prevents the need to check for existence first.
		/// </summary>
		/// <param name="key">Key to remove</param>
		public void RemoveSafe(TKey key)
		{
			using (new ReadLock(_dictionaryLock))
			{
				if (_dict.ContainsKey(key))
				{
					using (new WriteLock(_dictionaryLock))
					{
						_dict.Remove(key);
					}
				}
			}
		}


		/// <summary>
		/// Merge does a blind remove, and then add.  Basically a blind Upsert.  
		/// </summary>
		/// <param name="key">Key to lookup</param>
		/// <param name="newValue">New Value</param>
		public void MergeSafe(TKey key, TValue newValue)
		{
			using (new WriteLock(_dictionaryLock)) // take a writelock immediately since we will always be writing
			{
				if (_dict.ContainsKey(key))
				{
					_dict.Remove(key);
				}


				_dict.Add(key, newValue);
			}
		}


		public virtual bool Remove(TKey key)
		{
			using (new WriteLock(_dictionaryLock))
			{
				return _dict.Remove(key);
			}
		}


		public virtual bool ContainsKey(TKey key)
		{
			using (new ReadOnlyLock(_dictionaryLock))
			{
				return _dict.ContainsKey(key);
			}
		}


		public virtual bool TryGetValue(TKey key, out TValue value)
		{
			using (new ReadOnlyLock(_dictionaryLock))
			{
				return _dict.TryGetValue(key, out value);
			}
		}


		public virtual TValue this[TKey key]
		{
			get
			{
				using (new ReadOnlyLock(_dictionaryLock))
				{
					return _dict[key];
				}
			}
			set
			{
				using (new WriteLock(_dictionaryLock))
				{
					_dict[key] = value;
				}
			}
		}


		public virtual ICollection<TKey> Keys
		{
			get
			{
				using (new ReadOnlyLock(_dictionaryLock))
				{
					return new List<TKey>(_dict.Keys);
				}
			}
		}


		public virtual ICollection<TValue> Values
		{
			get
			{
				using (new ReadOnlyLock(_dictionaryLock))
				{
					return new List<TValue>(_dict.Values);
				}
			}
		}


		public virtual void Clear()
		{
			using (new WriteLock(_dictionaryLock))
			{
				_dict.Clear();
			}
		}


		public virtual int Count
		{
			get
			{
				using (new ReadOnlyLock(_dictionaryLock))
				{
					return _dict.Count;
				}
			}
		}


		public virtual bool Contains(KeyValuePair<TKey, TValue> item)
		{
			using (new ReadOnlyLock(_dictionaryLock))
			{
				return _dict.Contains(item);
			}
		}


		public virtual void Add(KeyValuePair<TKey, TValue> item)
		{
			using (new WriteLock(_dictionaryLock))
			{
				_dict.Add(item);
			}
		}


		public virtual void Add(TKey key, TValue value)
		{
			using (new WriteLock(_dictionaryLock))
			{
				_dict.Add(key, value);
			}
		}


		public virtual bool Remove(KeyValuePair<TKey, TValue> item)
		{
			using (new WriteLock(_dictionaryLock))
			{
				return _dict.Remove(item);
			}
		}


		public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			using (new ReadOnlyLock(_dictionaryLock))
			{
				_dict.CopyTo(array, arrayIndex);
			}
		}


		public virtual bool IsReadOnly
		{
			get
			{
				using (new ReadOnlyLock(_dictionaryLock))
				{
					return _dict.IsReadOnly;
				}
			}
		}


		public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			throw new NotSupportedException("Cannot enumerate a threadsafe dictionary.  Instead, enumerate the keys or values collection");
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotSupportedException("Cannot enumerate a threadsafe dictionary.  Instead, enumerate the keys or values collection");
		}
	}

}