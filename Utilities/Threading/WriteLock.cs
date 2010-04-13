using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AlienForce.Utilities.Threading
{
	public class WriteLock : BaseLock
	{
		public WriteLock(ReaderWriterLockSlim locks)
			: base(locks)
		{
			ReaderWriterLocks.GetWriteLock(this._Locks);
		}


		public override void Dispose()
		{
			ReaderWriterLocks.ReleaseWriteLock(this._Locks);
		}
	}
}
