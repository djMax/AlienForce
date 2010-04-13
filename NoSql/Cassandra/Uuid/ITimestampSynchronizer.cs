using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienForce.NoSql.Cassandra.Uuid
{
    public interface ITimestampSynchronizer
    {
        long Initialize();
        void Deactivate();
        long Update(long now);
    }
}
