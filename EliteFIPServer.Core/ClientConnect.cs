namespace EliteFIPServer {
    public static class ClientConnect {
        private static EliteAPIIntegration DataProvider;

        public static void SetDataProvider(EliteAPIIntegration currentDataProvider) {
            DataProvider = currentDataProvider;
        }

        public static void RequestDataUpdate() {
            if (DataProvider != null) {
                DataProvider.FullClientUpdate();
            }
        }

    }
}
