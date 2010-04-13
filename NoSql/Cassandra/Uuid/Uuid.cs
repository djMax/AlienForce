using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.NoSql.Cassandra.Uuid
{
    public class Uuid : ICloneable, IComparable
    {
        public static readonly byte IndexClockHigh = 6;
        public static readonly byte IndexClockMiddle = 4;
        public static readonly byte IndexClockLow = 0;

        public static readonly byte IndexType = 6;
        public static readonly byte IndexClockSequence = 8;
        public static readonly byte IndexVariation = 8;

        private static readonly Uuid mNullUuid = new Uuid();
        
        public enum UuidType : byte
        {
            Null = 0,
            TimeBased = 1,
            Dce = 2,
            NameBased = 3,
            RandomBased = 4
        }

        private byte[] mBytes = new byte[16];

        public byte[] Bytes
        {
            get
            {
                return mBytes;
            }
        }

        public UuidType Type
        {
            get
            {
                return (UuidType)((mBytes[IndexType] & 0xFF) >> 4);
            }
        }

        public Uuid()
        {
        }

        public Uuid(byte[] bytes)
        {
            Array.Copy(bytes, mBytes, bytes.Length);
        }

        public Uuid(UuidType type, byte[] bytes)
            : this(bytes)
        {
            // Type is multiplexed with time_hi:
            mBytes[IndexType] &= (byte)0x0F;
            mBytes[IndexType] |= (byte)((byte)type << 4);
            // Variant masks first two bits of the clock_seq_hi:
            mBytes[IndexVariation] &= (byte)0x3F;
            mBytes[IndexVariation] |= (byte)0x80;
        }

        public static Uuid Null
        {
            get
            {
                return mNullUuid;
            }
        }

        public bool IsNull
        {
            get
            {
                return (this == mNullUuid || this.Equals(mNullUuid));
            }
        }

        public override bool Equals(object obj)
        {
            Uuid uuid = obj as Uuid;
            if (uuid == null || uuid.mBytes.Length != mBytes.Length)
            {
                return false;
            }
            for (int i = 0; i < mBytes.Length; i++)
            {
                if (mBytes[i] != uuid.mBytes[i])
                {
                    return false;
                }
            }
            return true;
        }

        private int mHashCode;
        static readonly int[] mHashShifts = new int[]{3, 7, 17, 21, 29, 4, 9};
        public override int GetHashCode()
        {
            if (mHashCode == 0)
            {
                // Let's handle first and last byte separately:
                int result = mBytes[0] & 0xFF;
    	    
                result |= (result << 16);
                result |= (result << 8);
    	    
                for (int i = 1; i < 15; i += 2) {
                    int curr = (mBytes[i] & 0xFF) << 8 | (mBytes[i+1] & 0xFF);
                    int shift = mHashShifts[i >> 1];
    		
                    if (shift > 16) {
                        result ^= (curr << shift) | (curr >> (32 - shift));
                    } else {
                        result ^= (curr << shift);
                    }
                }

                // and then the last byte:
                int last = mBytes[15] & 0xFF;
                result ^= (last << 3);
                result ^= (last << 13);

                result ^= (last << 27);
                // Let's not accept hash 0 as it indicates 'not hashed yet':
                if (result == 0) {
                    mHashCode = -1;
                } else {
                    mHashCode = result;
                }
            }
            return mHashCode;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 16; ++i)
            {
                // Need to bypass hyphens:
                switch (i)
                {
                    case 4:
                    case 6:
                    case 8:
                    case 10:
                        sb.Append('-');
                        break;
                }
                sb.AppendFormat("{0:x2}", mBytes[i]);
            }
            return sb.ToString();
        }

        #region ICloneable Members

        public object Clone()
        {
            return new Uuid(mBytes);
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
