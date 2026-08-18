using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EliteFIPServer.ComponentState;

namespace EliteFIPServer {
    public enum RunState {
        Stopped,
        Starting,
        Started,
        Stopping
    }

    public class ComponentState {
        public event EventHandler<RunState> onStateChange;
        public RunState State { get; private set;}
        
        public ComponentState() {
            State = RunState.Stopped;
        }

        public void Set(RunState newState) {
            State = newState;
            onStateChange?.Invoke(this, State);
        }
    }
}
