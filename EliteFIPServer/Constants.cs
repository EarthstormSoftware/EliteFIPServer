namespace EliteFIPServer {
    static class Constants {
        public const string GameStateFolder = @"\Saved Games\Frontier Developments\Elite Dangerous\";
        public const string EDProcessName = "EliteDangerous64";
        public const string MatricProcessName = "MatricServer.exe";       
        public const string StatusFileName = "Status.json";
        public const string Eyecatcher = "EDFIPSRV";
        public const string ConfigName = "EDFIPServerConfig.ini";
        public const string ButtonTextConfigFilename = "ButtonTextConfig.json";

        public const int MaxGameDataQueueSize = 250;
        public const int MaxSSEQueueSize = 250;
    }
}
