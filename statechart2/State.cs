using System;
using System.Collections;
using System.Collections.Generic;

namespace StateChart
{
    public enum EResult
    { 
        None,
        Forward,
        Resume,
        Defered,
    }

    public enum EHistory
    {
        Shallow,
        Deep,
    }
    
    public delegate void Reaction<T>(T fsm);
    public delegate EResult Reaction<U, T>(U fsm, T evt);
    interface IReaction {
        EResult Execute<FSM, EVENT>(FSM fsm_, EVENT evt);
    }
    public class CReaction<FSM, EVENT> : IReaction {
        Reaction<FSM, EVENT> reaction;
        public CReaction(Reaction<FSM, EVENT> reaction_) { reaction = reaction_; }
        public EResult Execute<F, E>(F fsm_, E evt)
        { return reaction((FSM)(object)fsm_, (EVENT)(object)evt); }
    }

    public abstract class IState<FSM> where FSM : IStateMachine<FSM>
    {
        public Type type { get { return GetType(); } }
        public EHistory History { get; set; }
        public Reaction<FSM> Entry { get; set; }
        public Reaction<FSM> Exit { get; set; }

        //could calc on runtime, but we need more fast spped this time.
        public int Depth { get; set; }
        public IState<FSM> OuterState { get; set; }
        public IState<FSM> ActiveState { get; set; }

        Dictionary<Type, IReaction> reactions = new Dictionary<Type, IReaction>();
        Dictionary<Type, Type> transitions = new Dictionary<Type, Type>();
        List<IState<FSM>> subStates = new List<IState<FSM>>();

        IState<FSM> initState = null;
        public IState<FSM> InitState
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

        public IState(IState<FSM> ostate)
        {
            History = EHistory.Shallow;
            OuterState = ostate;
            if (OuterState != null) OuterState.AddSubState(this);
        }

        public IState(IState<FSM> ostate, EHistory history_)
        {
            OuterState = ostate;
            History = history_;
            if (OuterState != null) OuterState.AddSubState(this);
        }

        public void DoEntry(FSM fsm_)
        {
            //UnityEngine.Debug.Log("Entry: " + type.ToString());
            Console.WriteLine("Entry: " + type.ToString());
            if (Entry != null) Entry(fsm_);
            else OnEntry(fsm_);
        }
        public void DoExit(FSM fsm_)
        {
            //UnityEngine.Debug.Log("Exit : " + type.ToString());
            Console.WriteLine("Exit : " + type.ToString());
            if (Exit != null) Exit(fsm_);
            else OnExit(fsm_);
        }

        protected virtual void OnEntry(FSM fsm_) { }
        protected virtual void OnExit(FSM fsm_) { }

        public EResult Process<EVENT>(FSM fsm_, EVENT evt) where EVENT : IEvent 
        {
            IReaction reaction = null;
            bool hasit = reactions.TryGetValue(evt.type, out reaction);
            if (!hasit) return EResult.Forward;
            return reaction.Execute<FSM, EVENT>(fsm_, evt);
        }

        public void Bind<EVENT>(Reaction<FSM, EVENT> reaction) where EVENT : IEvent
        {
            if (transitions.ContainsKey(typeof(EVENT)))
                throw new System.InvalidOperationException();
            IReaction ireaction = new CReaction<FSM, EVENT>(reaction);
            reactions.Add(typeof(EVENT), ireaction); 
        }

        public void Bind<EVENT, TSTATE>()
            where EVENT : IEvent
            where TSTATE : IState<FSM>
        {
            if (reactions.ContainsKey(typeof(EVENT)))
                throw new System.InvalidOperationException();
            transitions.Add(typeof(EVENT), typeof(TSTATE));
        }

        public void AddSubState(IState<FSM> sstate)
        {
            IState<FSM> state = subStates.Find((x) => x.type == sstate.type);
            if (state != null) return;
            subStates.Add(sstate);
        }

        public IEnumerable<IState<FSM>> IterateSubState()
        {
            foreach (IState<FSM> state in subStates) 
                yield return state;
        }
    }
}
