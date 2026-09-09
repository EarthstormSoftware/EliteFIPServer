namespace EliteFIPServer {
    public static class ClientConnect {
        private static EliteAPIIntegration DataProvider;

        public static void SetDataProvider(EliteAPIIntegration currentDataProvider) {
            DataProvider = currentDataProvider;
        }

        public static Task RequestDataUpdate(string connectionId) {
            if (DataProvider != null) {
                return DataProvider.FullClientUpdate(connectionId);
            }
            return Task.CompletedTask;
        }

    }
}
