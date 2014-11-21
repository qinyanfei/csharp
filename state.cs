using System;
using System.Collections;
using System.Collections.Generic;

namespace StateChart
{
    using RegionId = System.Int32;
    using StateList = List<IState>;
    using RegionTable = Dictionary<System.Int32, List<IState>>;

    public delegate void Reaction();
    public delegate EResult Reaction<T>(T obj);


    interface IState
    {
        Type type { get; }
        void Entry();
        void Exit();
        void Process(IEvent evt);
        IState GetOuterState();
        StateList GetsubStates();
        IState GetActiveState();
        void SetActiveState(IState state);
        void AddSubState(IState state);
        IState GetInitState();
        void SetInitState(IState state);
        int GetDepth();
        void SetDepth(int depth);
    }

    

    class State<T> : IState
    {
        int depth = -1;  //could calc on runtime, but we need more fast this time.
        Reaction entryCallback = null;
        Reaction exitCallback = null;

        Dictionary<Type, Reaction<IEvent>> reactions = new Dictionary<Type, Reaction<IEvent>>();
        StateList subStates = new StateList();
        IState outerState = null;
        IState activeState = null;
        IState initState = null;

        public Type type { get { return typeof(T); } }

        public State(IState ostate)
        {
            entryCallback = OnEntry; exitCallback = OnExit;
            outerState = ostate;
            if(outerState != null)
                outerState.AddSubState(this);
        }

        public void Entry()
        { if (entryCallback != null) entryCallback(); Console.WriteLine("entry: " + typeof(T).ToString()); }
        public void Exit()
        { if (exitCallback != null) exitCallback(); Console.WriteLine("exit: " + typeof(T).ToString()); }

        protected virtual void OnEntry() { }
        protected virtual void OnExit() { }

        public void SetEntry(Reaction callback)
        { entryCallback = callback; }
        public void SetExit(Reaction callback)
        { exitCallback = callback; }

        public void Process(IEvent evt) 
        { 
            Reaction<IEvent> reaction = null;
            bool hasit = reactions.TryGetValue(evt.GetType(), out reaction);
            if (!hasit) return;
            reaction(evt);
        }

        public void Bind<E>(Reaction<IEvent> reaction)
        {
            reactions.Add(typeof(E), reaction);
        }

        public void SetOuterState(IState ostate) { outerState = ostate; }
        public IState GetOuterState() { return outerState; }
        public void AddSubState(IState sstate) 
        {
            IState state = subStates.Find((x) => x.type == sstate.type);
            if (state != null) return;
            subStates.Add(sstate);
        }
        public StateList GetsubStates() 
        { return subStates; }

        public IState GetActiveState() 
        { return activeState; }
        public void SetActiveState(IState activeState_)
        { activeState = activeState_; }

        public IState GetInitState()
        {
            if (initState == null)
                if (subStates.Count > 0)
                    initState = subStates[0];
            return initState;
        }
        public void SetInitState(IState initstate)
        { initState = initstate;  }

        public void SetDepth(int depth_) { depth = depth_; }
        public int GetDepth() { return depth; }
    }
}
