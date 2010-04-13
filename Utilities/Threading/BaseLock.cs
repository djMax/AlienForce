using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AlienForce.Utilities.Threading
{
	public abstract class BaseLock : IDisposable
	{
		protected ReaderWriterLockSlim _Locks;

		public BaseLock(ReaderWriterLockSlim locks)
		{
			_Locks = locks;
		}

		public abstract void Dispose();
	}
}
