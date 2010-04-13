using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

namespace AlienForce.NoSql.Cassandra.Uuid
{
    public class UuidTimer
    {
        private static readonly DateTime baseTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private static readonly long kClockOffset = 0x01b21dd213814000L;
        private static readonly long kClockMultiplier = 10000;
        private static readonly long kClockMultiplierL = 10000L;
        private static readonly long kMaxClockAdvance = 100L;

        private readonly RandomNumberGenerator mRandom;
        private readonly byte[] mClockSequence = new byte[3];
        private long mLastSystemTimestamp = 0L;
        private long mLastUsedTimestamp = 0L;
        private long mFirstUnsafeTimestamp = long.MaxValue;
        private int mClockCounter = 0;
        private ITimestampSynchronizer mSync = null;

        public UuidTimer(RandomNumberGenerator rnd)
        {
            mRandom = rnd;
            InitCounters(rnd);
            mLastSystemTimestamp = 0L;
            // This may get overwritten by the synchronizer
            mLastUsedTimestamp = 0L;
        }

        private void InitCounters(RandomNumberGenerator rnd)
        {
            rnd.GetBytes(mClockSequence);
            mClockCounter = mClockSequence[2] & 0xFF;
        }

        public void GetTimestamp(byte[] uuidBytes)
        {
            // First the clock sequence:
            uuidBytes[Uuid.IndexClockSequence] = mClockSequence[0];
            uuidBytes[Uuid.IndexClockSequence + 1] = mClockSequence[1];

            long systime = (long)DateTime.UtcNow.Subtract(baseTime).TotalMilliseconds;

            if (systime < mLastSystemTimestamp)
            {
               // Logger.logWarning("System time going backwards! (got value " + systime + ", last " + mLastSystemTimestamp);
                // Let's write it down, still
                mLastSystemTimestamp = systime;
            }

            /* But even without it going backwards, it may be less than the
             * last one used (when generating UUIDs fast with coarse clock
             * resolution; or if clock has gone backwards over reboot etc).
             */
            if (systime <= mLastUsedTimestamp)
            {
                /* Can we just use the last time stamp (ok if the counter
                 * hasn't hit max yet)
                 */
                if (mClockCounter < kClockMultiplier)
                { // yup, still have room
                    systime = mLastUsedTimestamp;
                }
                else
                { // nope, have to roll over to next value and maybe wait
                    long actDiff = mLastUsedTimestamp - systime;
                    long origTime = systime;
                    systime = mLastUsedTimestamp + 1L;

                    //Logger.logWarning("Timestamp over-run: need to reinitialize random sequence");

                    /* Clock counter is now at exactly the multiplier; no use
                     * just anding its value. So, we better get some random
                     * numbers instead...
                     */
                    InitCounters(mRandom);

                    /* But do we also need to slow down? (to try to keep virtual
                     * time close to physical time; ie. either catch up when
                     * system clock has been moved backwards, or when coarse
                     * clock resolution has forced us to advance virtual timer
                     * too far)
                     */
                    if (actDiff >= kMaxClockAdvance)
                    {
                        SlowDown(origTime, actDiff);
                    }
                }
            }
            else
            {
                /* Clock has advanced normally; just need to make sure counter is
                 * reset to a low value (need not be 0; good to leave a small
                 * residual to further decrease collisions)
                 */
                mClockCounter &= 0xFF;
            }

            mLastUsedTimestamp = systime;

            /* Ok, we have consistent clock (virtual or physical) value that
             * we can and should use.
             * But do we need to check external syncing now?
             */
            if (mSync != null && systime >= mFirstUnsafeTimestamp) {
                try {
                    mFirstUnsafeTimestamp = mSync.Update(systime);
                } catch (Exception ioe) {
                    throw new Exception("Failed to synchronize timestamp: "+ioe);
                }
            }

            /* Now, let's translate the timestamp to one UUID needs, 100ns
             * unit offset from the beginning of Gregorian calendar...
             */
            systime *= kClockMultiplierL;
            systime += kClockOffset;

            // Plus add the clock counter:
            systime += mClockCounter;
            // and then increase
            ++mClockCounter;

            /* Time fields are nicely split across the UUID, so can't just
             * linearly dump the stamp:
             */
            int clockHi = (int) (systime >> 32);
            int clockLo = (int) systime;

            uuidBytes[Uuid.IndexClockHigh] = (byte) (clockHi >> 24);
            uuidBytes[Uuid.IndexClockHigh + 1] = (byte) (clockHi >> 16);
            uuidBytes[Uuid.IndexClockMiddle] = (byte) (clockHi >> 8);
            uuidBytes[Uuid.IndexClockMiddle + 1] = (byte) clockHi;

            uuidBytes[Uuid.IndexClockLow] = (byte) (clockLo >> 24);
            uuidBytes[Uuid.IndexClockLow + 1] = (byte) (clockLo >> 16);
            uuidBytes[Uuid.IndexClockLow + 2] = (byte) (clockLo >> 8);
            uuidBytes[Uuid.IndexClockLow + 3] = (byte) clockLo;
        }

        private static readonly int MaxWaitCount = 50;

        /**
         * Simple utility method to use to wait for couple of milliseconds,
         * to let system clock hopefully advance closer to the virtual
         * timestamps used. Delay is kept to just a millisecond or two,
         * to prevent excessive blocking; but that should be enough to
         * eventually synchronize physical clock with virtual clock values
         * used for UUIDs.
         *
         * @param msecs Number of milliseconds to wait for from current 
         *    time point
         */
        private static void SlowDown(long startTime, long actDiff)
        {
            /* First, let's determine how long we'd like to wait.
             * This is based on how far ahead are we as of now.
             */
            long ratio = actDiff / kMaxClockAdvance;
            int delay;
            
            if (ratio < 2L) { // 200 msecs or less
                delay = 1;
            } else if (ratio < 10L) { // 1 second or less
                delay = 2;
            } else if (ratio < 600L) { // 1 minute or less
                delay = 3;
            } else {
                delay = 5;
            }
            //Logger.logWarning("Need to wait for "+delay+" milliseconds; virtual clock advanced too far in the future");
            long waitUntil = startTime + delay;
            int counter = 0;
            do {
                try 
                {
                    Thread.Sleep(delay);
                } 
                catch (ThreadInterruptedException) { }
                delay = 1;
                /* This is just a sanity check: don't want an "infinite"
                 * loop if clock happened to be moved backwards by, say,
                 * an hour...
                 */
                if (++counter > MaxWaitCount) {
                    break;
                }
            } while ((long)DateTime.UtcNow.Subtract(baseTime).TotalMilliseconds < waitUntil);
        }
    }
}
