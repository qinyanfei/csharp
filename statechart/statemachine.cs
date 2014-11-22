using System;
using System.Collections;
using System.Collections.Generic;

namespace StateChart
{
    using StateList = List<IState>;
    using RegionTable = Dictionary<System.Int32, List<IState>>;

    public enum EHistory
    { 
        Shallow,
        Deep,
    }

    interface IStateMachine
    {
        void Init(IState state);
        void Suspend();
        void Resume();
        void Process(Event evt);
        EResult Transit<TState>();
    }

    //state machine also could be a state, but this time i will not do this again, it just make things more terrible.
    //supporting region is too complicate and not beautiful. region always could be replaced by more statemachine. 
    //you could directly create State<...> to use it. or you could inherit from it, this is a more powerful method.
    class StateMachine : IStateMachine
    {
        Dictionary<Type, IState> typeStates = new Dictionary<Type, IState>();
        StateList activeStates = new StateList();
        IState outestState = null;
        bool bSuspend = false;

        public StateMachine() { }

        public void Init(IState state) 
        {
            IState pstate = state;

            //add outer states
            while (pstate.OuterState != null) {
                pstate.OuterState.ActiveState = pstate;
                activeStates.Add(pstate);
                pstate = pstate.OuterState;
            } 
            activeStates.Add(pstate);
            outestState = pstate;
            
            //build global type-to-state table
            BuildStateTable(outestState, 0);

            //add init sub states
            pstate = state;
            while (pstate.InitState != null) {
                pstate.ActiveState = pstate.InitState;
                pstate = state.InitState;
                if(pstate != null) activeStates.Add(pstate);
            }

            activeStates.Sort((x, y) => x.Depth - y.Depth);
            foreach (IState astate in activeStates) {
                astate.DoEntry();
            }
        }

        void BuildStateTable(IState state, int depth_) 
        {
            if (state == null) return;
            state.Depth = depth_;
            typeStates.Add(state.type, state);
            foreach (IState sstate in state.IterateSubState()) { 
                BuildStateTable(sstate, depth_ + 1); 
            }
        }

        EResult Transit(IState state)
        {
            IState lstate = null;

            lstate = outestState;
            while (lstate.ActiveState != null) {  // we could save it.
                lstate = lstate.ActiveState;
            }

            IState rstate = state;
            if (state.History == EHistory.Shallow)
                while (rstate.InitState != null)
                    rstate = state.InitState;
            else
                while (rstate.ActiveState != null)
                    rstate = rstate.ActiveState;


            IState ltail = lstate;  //save tail of active states
            IState rtail = rstate;    //save tail of init states

            int dis = lstate.Depth - rstate.Depth;
            if (dis > 0) {
                IState tstate = lstate; lstate = rstate; rstate = tstate;  //rstate will be deepest state
            }
            dis = Math.Abs(dis);
            for (int i = 0; i < dis; i++)  {
                rstate = rstate.OuterState;
            }
            if (rstate == lstate)  //is family
                return EResult.None;
            do
            { //find nearest outer state
                rstate = rstate.OuterState;
                lstate = lstate.OuterState;
            } while (lstate != rstate);

            do  // call exit chain 
            {
                ltail.DoExit();
                ltail = ltail.OuterState;
            } while (ltail != lstate);

            //add tail chain active states
            activeStates.RemoveRange(rstate.Depth + 1, activeStates.Count - rstate.Depth - 1);
            do
            {
                activeStates.Add(rtail);
                lstate = rtail;
                rtail = rtail.OuterState;
                rtail.ActiveState = lstate;
            } while (rtail != rstate);

            // do entry chain
            while (rstate.ActiveState != null)
            {
                rstate = rstate.ActiveState;
                rstate.DoEntry();
            }

            activeStates.Sort((x, y) => x.Depth - y.Depth);
            return EResult.None;
        }

        public EResult Transit(Type stateType)
        {
            IState state = null;
            if (!typeStates.TryGetValue(stateType, out state))
                return EResult.None;
            return Transit(state);
        }

        public EResult Transit<TState>() 
        { return Transit(typeof(TState)); }

        public void Process(Event evt)
        {
            if (bSuspend) return;
            foreach (IState state in activeStates) {
                if (bSuspend || state.Process(evt) == EResult.None)
                    break;
            }
        }

        public void Suspend()
        { bSuspend = true; }
        public void Resume()
        { bSuspend = false; }
    }
}
