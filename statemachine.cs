using System;
using System.Collections;
using System.Collections.Generic;

namespace StateChart
{
    using StateList = List<IState>;
    using RegionTable = Dictionary<System.Int32, List<IState>>;

    interface IStateMachine
    {
        void Init(IState state);
        void Suspend();
        void Resume();
        void Process(IEvent evt);
        void Transit<TState>();
    }

    //state machine also could be a state, but this time i will not do this again, it just make things more terrible.
    //supporting region was too complicate and not beautiful. region always could be replace by more statemachine. 
    //you could direct create State<...> to use it. or you could inherit from it, this is a more powerful method.
    //deep history is useful, not so hard to support it. maybe later.
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
            while (pstate.GetOuterState() != null) {
                pstate.GetOuterState().SetActiveState(pstate);
                activeStates.Add(pstate);
                pstate = pstate.GetOuterState();
            } 
            activeStates.Add(pstate);
            outestState = pstate;
            
            //build global type-to-state table
            BuildStateTable(outestState, 0);

            //add init sub states
            pstate = state;
            while (pstate.GetInitState() != null) {
                pstate.SetActiveState(pstate.GetInitState());
                pstate = state.GetInitState();
                if(pstate != null) activeStates.Add(pstate);
            }

            activeStates.Sort((x, y) => x.GetDepth() - y.GetDepth());
            foreach (IState astate in activeStates) {
                astate.Entry();
            }
        }

        void BuildStateTable(IState state, int depth_) 
        {
            if (state == null) return;
            state.SetDepth(depth_);
            typeStates.Add(state.type, state);
            StateList statelsit = state.GetsubStates();
            foreach (IState sstate in statelsit)
            { BuildStateTable(sstate, depth_ + 1); }
        }

        void DoEntry() { }
        void DoExit() { }

        void Transit(IState state)
        {
            IState lstate = null;

            lstate = outestState;
            while (lstate.GetActiveState() != null) {  //maybe we could save it.
                lstate = lstate.GetActiveState();
            }

            IState rstate = state;
            while (rstate.GetInitState() != null) {
                    rstate = state.GetInitState();
            }

            IState ltail = lstate;  //save tail of active states
            IState rtail = rstate;    //save tail of init states

            int dis = lstate.GetDepth() - rstate.GetDepth();
            if (dis > 0) {
                IState tstate = lstate; lstate = rstate; rstate = tstate;  //rstate will be deepest state
            }
            dis = Math.Abs(dis);
            for (int i = 0; i < dis; i++)  {
                rstate = rstate.GetOuterState();
            }
            if (rstate == lstate)  //is family
                return;
            do
            { //find nearest outer state
                rstate = rstate.GetOuterState();
                lstate = lstate.GetOuterState();
            } while (lstate != rstate);

            do  // call exit chain 
            {
                ltail.Exit();
                ltail = ltail.GetOuterState();
            } while (ltail != lstate);

            //add tail chain active states
            activeStates.RemoveRange(rstate.GetDepth() + 1, rtail.GetDepth() - rstate.GetDepth());
            do
            {
                activeStates.Add(rtail);
                lstate = rtail;
                rtail = rtail.GetOuterState();
                rtail.SetActiveState(lstate);
            } while (rtail != rstate);

            // do entry chain
            while(rstate.GetActiveState() != null) {
                rstate = rstate.GetActiveState();
                rstate.Entry();
            }

            activeStates.Sort((x, y) => x.GetDepth() - y.GetDepth());
        }

        public void Transit(Type stateType)
        {
            IState state = null;
            if (!typeStates.TryGetValue(stateType, out state))
                return;
            Transit(state);
        }

        public void Transit<TState>() 
        { Transit(typeof(TState)); }

        public void Process(IEvent evt)
        {
            if (bSuspend) return;
            foreach (IState state in activeStates)
                state.Process(evt);
        }

        public void Suspend()
        { bSuspend = true; }
        public void Resume()
        { bSuspend = false; }
    }
}
