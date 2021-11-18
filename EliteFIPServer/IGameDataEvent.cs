using System;

namespace EliteFIPServer {
    interface IGameDataEvent {
        void GameDataEvent(GameEventType eventType, Object evt);
    }
}
