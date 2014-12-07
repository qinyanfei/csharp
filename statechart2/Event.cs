using System;
using System.Collections.Generic;

namespace StateChart
{
    public abstract class IEvent
    {
        public Type type { get { return GetType(); } }
    }
}
