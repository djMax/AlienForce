using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AlienForce.Utilities.Threading
{
	public class ReadOnlyLock : BaseLock
	{
		public ReadOnlyLock(ReaderWriterLockSlim locks)
			: base(locks)
		{
			ReaderWriterLocks.GetReadOnlyLock(this._Locks);
		}


		public override void Dispose()
		{
			ReaderWriterLocks.ReleaseReadOnlyLock(this._Locks);
		}
	}

}
