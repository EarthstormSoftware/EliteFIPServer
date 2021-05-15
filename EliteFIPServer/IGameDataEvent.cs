using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliteFIPServer {
    interface IGameDataEvent {
        void GameDataEvent(GameEventType eventType, Object evt);
    }
}
