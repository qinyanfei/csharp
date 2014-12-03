using System;
using System.Collections;
using System.Collections.Generic;

namespace StateChart
{
    public enum EHistory
    { 
        Shallow,
        Deep,
    }

    public interface IStateMachine<FSM>
    {
        void Init(IState<FSM> state);
        void Process<EVENT>(EVENT evt) where EVENT : IEvent;
        void PostEvent<EVENT>(EVENT evt) where EVENT : IEvent;
        EResult Transit<TState>();
        void Suspend();
        void Resume();
    }

    //state machine also could be a state, but this time i will not do this again, it just make things more terrible.
    //supporting region is too complicate and not beautiful. region always could be replaced by more statemachine. 
    //you could directly create State<...> to use it. or you could inherit from it, this is a more powerful method.
    public class StateMachine<HOST> : IStateMachine<HOST> where HOST : StateMachine<HOST>
    {
        Dictionary<Type, IState<HOST>> typeStates = new Dictionary<Type, IState<HOST>>();
        List<IState<HOST>> activeStates = new List<IState<HOST>>();
        Queue<IEvent> eventQueue = new Queue<IEvent>();
        IState<HOST> outestState = null;
        bool bSuspend = false;

        public StateMachine() { }

        public void Init(IState<HOST> state) 
        {
            IState<HOST> pstate = state;

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
            foreach (IState<HOST> astate in activeStates) {
                astate.DoEntry((HOST)(this));
            }
        }

        void BuildStateTable(IState<HOST> state, int depth_) 
        {
            if (state == null) return;
            state.Depth = depth_;
            typeStates.Add(state.type, state);
            foreach (IState<HOST> sstate in state.IterateSubState()) { 
                BuildStateTable(sstate, depth_ + 1); 
            }
        }

        EResult Transit(IState<HOST> state)
        {
            IState<HOST> lstate = null;

            lstate = outestState;
            while (lstate.ActiveState != null) {  // we could save it if state tree is too high.
                lstate = lstate.ActiveState;
            }

            IState<HOST> rstate = state;
            if (state.History == EHistory.Shallow)
                while (rstate.InitState != null)
                    rstate = state.InitState;
            else
                while (rstate.ActiveState != null)
                    rstate = rstate.ActiveState;


            IState<HOST> ltail = lstate;  //save tail of active states
            IState<HOST> rtail = rstate;    //save tail of init states

            int dis = lstate.Depth - rstate.Depth;
            if (dis > 0)
                { IState<HOST> tstate = lstate; lstate = rstate; rstate = tstate; } //rstate will be deepest state

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
                ltail.DoExit((HOST)this);
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
                rstate.DoEntry((HOST)this);
            }

            activeStates.Sort((x, y) => x.Depth - y.Depth);
            return EResult.None;
        }

        public EResult Transit(Type stateType)
        {
            IState<HOST> state = null;
            if (!typeStates.TryGetValue(stateType, out state))
                return EResult.None;
            return Transit(state);
        }

        public EResult Transit<TSTATE>()
        { return Transit(typeof(TSTATE)); }

        public void Process<EVENT>(EVENT evt) where EVENT : IEvent
        {
            if (bSuspend) return;

            eventQueue.Enqueue(evt);

            while (eventQueue.Count > 0) {
                IEvent pevent = eventQueue.Dequeue();
                foreach (IState<HOST> state in activeStates)
                    if (bSuspend || state.Process((HOST)this, pevent) == EResult.None)
                        break;
            }
        }

        public void PostEvent<EVENT>(EVENT evt) where EVENT : IEvent
        {
            if (bSuspend) return;
            eventQueue.Enqueue(evt);
        }

        public void Suspend()
        { bSuspend = true; }
        public void Resume()
        { bSuspend = false; }
    }
}
