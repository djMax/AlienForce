using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AlienForce.Utilities.Threading
{
	public class ReadLock : BaseLock
	{
		public ReadLock(ReaderWriterLockSlim locks)
			: base(locks)
		{
			ReaderWriterLocks.GetReadLock(this._Locks);
		}


		public override void Dispose()
		{
			ReaderWriterLocks.ReleaseReadLock(this._Locks);
		}
	}
}
