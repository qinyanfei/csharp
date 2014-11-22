using System;
using System.Collections;
using System.Collections.Generic;

namespace StateChart
{
    using StateList = List<IState>;

    public enum EResult
    { 
        None,
        Forwad,
        Resume,
        Defered,
    }
    
    public delegate void Reaction();
    public delegate EResult Reaction<T>(T obj);

    interface IState
    {
        void DoEntry();
        void DoExit();
        EResult Process(Event evt);
        void AddSubState(IState state);
        IEnumerable<IState> IterateSubState();

        Type type { get; }
        Reaction Entry { get; set; }
        Reaction Exit { get; set; }
        IState OuterState { get; }
        IState ActiveState { get; set; }
        IState InitState { get; set; }
        int Depth { get; set; }
    }

    class State<T> : IState
    {
        int depth = -1;  //could calc on runtime, but we need more fast spped this time.
        public int Depth { get { return depth; } set { depth = value; } }
        Reaction entryCallback = null;
        public Reaction Entry { get { return entryCallback; } set { entryCallback = value; } }
        Reaction exitCallback = null;
        public Reaction Exit { get { return exitCallback; } set { exitCallback = value; } }

        Dictionary<Type, Reaction<Event>> reactions = new Dictionary<Type, Reaction<Event>>();
        StateList subStates = new StateList();

        IState outerState = null;
        public IState OuterState 
        { get { return outerState; } }

        IState activeState = null;
        public IState ActiveState { get { return activeState; } set { activeState = value; } }

        IState initState = null;
        public IState InitState 
        { 
            get 
            {
                if (initState == null)
                    if (subStates.Count > 0)
                        initState = subStates[0];
                return initState;
            } 
            set { initState = value; }
        }

        public Type type { get { return typeof(T); } }

        public State(IState ostate)
        {
            Entry = OnEntry; Exit = OnExit;
            outerState = ostate;
            if(outerState != null)
                outerState.AddSubState(this);
        }

        public void DoEntry()
        { if (Entry != null) Entry(); Console.WriteLine("entry: " + typeof(T).ToString()); }
        public void DoExit()
        { if (Exit != null) Exit(); Console.WriteLine("exit: " + typeof(T).ToString()); }

        protected virtual void OnEntry() { }
        protected virtual void OnExit() { }

        public EResult Process(Event evt) 
        { 
            Reaction<Event> reaction = null;
            bool hasit = reactions.TryGetValue(evt.GetType(), out reaction);
            if (!hasit) return EResult.Forwad;
            return reaction(evt);
        }

        public void Bind<E>(Reaction<Event> reaction)
        { reactions.Add(typeof(E), reaction); }

        public void AddSubState(IState sstate)
        {
            IState state = subStates.Find((x) => x.type == sstate.type);
            if (state != null) return;
            subStates.Add(sstate);
        }

        public IEnumerable<IState> IterateSubState() {
            foreach (IState state in subStates) {
                yield return state;
            }
        }
    }
}
