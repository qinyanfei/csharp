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

    public interface IState
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
        EHistory History { get; set; }
    }

    public class State<T> : IState
    {
        public Type type { get { return typeof(T); } }
        public EHistory History { get; set; }
        public Reaction Entry { get; set; }
        public Reaction Exit { get; set; }

        //could calc on runtime, but we need more fast spped this time.
        public int Depth { get; set; }
        public IState OuterState { get; set; }
        public IState ActiveState { get; set; }

        Dictionary<Type, Reaction<Event>> reactions = new Dictionary<Type, Reaction<Event>>();
        StateList subStates = new StateList();

        IState initState = null;
        public IState InitState  { 
            get  {
                if (initState == null)
                    if (subStates.Count > 0) 
                        initState = subStates[0];
                return initState;
            } 
            set { initState = value; }
        }

        public State(IState ostate) {
            Entry = OnEntry; Exit = OnExit;
            History = EHistory.Shallow;
            OuterState = ostate;
            if (OuterState != null) OuterState.AddSubState(this);
        }

        public State(IState ostate, EHistory history_) {
            Entry = OnEntry; Exit = OnExit;
            OuterState = ostate;
            History = history_;
            if (OuterState != null) OuterState.AddSubState(this);
        }
		
        public void DoEntry() {
            if (Entry != null) Entry();
            else OnEntry();
        }
        public void DoExit()  {
            if (Exit != null) Exit();
            else OnExit();
        }

        public virtual void OnEntry() { }
        public virtual void OnExit() { }

        public EResult Process(Event evt)   { 
            Reaction<Event> reaction = null;
            bool hasit = reactions.TryGetValue(evt.GetType(), out reaction);
            if (!hasit) return EResult.Forwad;
            return reaction(evt);
        }

        public void Bind<E>(Reaction<Event> reaction)
        { reactions.Add(typeof(E), reaction); }

        public void AddSubState(IState sstate) {
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
