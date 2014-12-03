using System;
using System.Collections.Generic;

namespace StateChart
{
    public interface IEvent
    {
        Type type { get; }
    }
    public class Event<T> : IEvent
    {
        public Type type { get { return typeof(T); } }
    }
}
