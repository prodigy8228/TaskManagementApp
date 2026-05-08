using TaskManagement.View;
namespace TaskManagement
{
    public partial class App : Application
    {
        public static FirestoreService Firestore { get; private set; }

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            Application.Current.UserAppTheme = AppTheme.Light;

            MainPage = serviceProvider.GetService<SignInPage>();


        }
        public static async Task InitializeSession(string idToken)
        {
            // 1. Initialize the global Firestore service
            Firestore = new FirestoreService();
            await Firestore.LoadSettingsToGlobalsAsync();

            // 2. Switch the root page to AppShell to allow main app access
            Current.MainPage = new AppShell();
        }

        protected override async void OnStart()
        {
            try
            {
                //  await InitFirestoreAsync();
                // MainPage = new AppShell();
            }
            catch (Exception ex)
            {
                // Log or handle initialization error
            }
        }


    }
}