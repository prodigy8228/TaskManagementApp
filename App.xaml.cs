namespace TaskManagement
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Application.Current.UserAppTheme = AppTheme.Light;

            MainPage = new AppShell();

        }

        private async Task MigrateDatabaseAsync(int oldVersion, int newVersion)
        {
            MISDatabase taskService = new MISDatabase();
            if (oldVersion == 1 && newVersion == 2)
            {
                await taskService.ExecuteAsync("ALTER TABLE Settings ADD COLUMN AppVersion DOUBLE DEFAULT 1.0;");
            }
        }

        protected override async void OnStart()
        {
            //  await Database.LoadSettingsToGlobalsAsync();
            // Now GlobalVariables.Theme and Language are ready to use
        }
    }
}