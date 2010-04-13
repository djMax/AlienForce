using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AlienForce.Utilities.Threading
{
	public static class ReaderWriterLocks
	{
		public static void GetReadLock(ReaderWriterLockSlim locks)
		{
			bool lockAcquired = false;
			while (!lockAcquired)
				lockAcquired = locks.TryEnterUpgradeableReadLock(1);
		}


		public static void GetReadOnlyLock(ReaderWriterLockSlim locks)
		{
			bool lockAcquired = false;
			while (!lockAcquired)
				lockAcquired = locks.TryEnterReadLock(1);
		}


		public static void GetWriteLock(ReaderWriterLockSlim locks)
		{
			bool lockAcquired = false;
			while (!lockAcquired)
				lockAcquired = locks.TryEnterWriteLock(1);
		}


		public static void ReleaseReadOnlyLock(ReaderWriterLockSlim locks)
		{
			if (locks.IsReadLockHeld)
				locks.ExitReadLock();
		}


		public static void ReleaseReadLock(ReaderWriterLockSlim locks)
		{
			if (locks.IsUpgradeableReadLockHeld)
				locks.ExitUpgradeableReadLock();
		}


		public static void ReleaseWriteLock(ReaderWriterLockSlim locks)
		{
			if (locks.IsWriteLockHeld)
				locks.ExitWriteLock();
		}


		public static void ReleaseLock(ReaderWriterLockSlim locks)
		{
			ReleaseWriteLock(locks);
			ReleaseReadLock(locks);
			ReleaseReadOnlyLock(locks);
		}


		public static ReaderWriterLockSlim GetLockInstance()
		{
			return GetLockInstance(LockRecursionPolicy.SupportsRecursion);
		}


		public static ReaderWriterLockSlim GetLockInstance(LockRecursionPolicy recursionPolicy)
		{
			return new ReaderWriterLockSlim(recursionPolicy);
		}
	}

}
