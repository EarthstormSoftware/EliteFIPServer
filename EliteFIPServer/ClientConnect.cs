namespace EliteFIPServer {
    public static class ClientConnect {
        private static ServerCore serverCore;

        public static void setServerCore(ServerCore currentServerCore) { 
            serverCore = currentServerCore; 
        }

        public static void requestDataUpdate() { 
            if (serverCore != null) {
                serverCore.fullClientUpdate();
            }
        }

    }
}
