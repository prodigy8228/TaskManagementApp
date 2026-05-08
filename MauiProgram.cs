using CommunityToolkit.Maui;
using Microsoft.Maui.LifecycleEvents;
using TaskManagement.View;
#if ANDROID
using TaskManagement.Services;
using TaskManagement.Platforms;
using Plugin.Firebase.Core.Platforms.Android; // 2. ADD THIS FOR ANDROID INIT
#endif
using Plugin.Firebase.Auth; // 3. ADD FOR AUTH
using Plugin.Firebase.Firestore; // 4. ADD FOR FIRESTORE
using Microsoft.Extensions.Logging;

namespace TaskManagement
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>().UseMauiCommunityToolkit().RegisterFirebaseServices().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            }).UseMauiCommunityToolkitMediaElement();

            builder.ConfigureLifecycleEvents(events => // Ensure this method is available
            {
#if WINDOWS
                events.AddWindows(wndLifeCycleBuilder =>
                {
                    wndLifeCycleBuilder.OnWindowCreated(window =>
                    {
                        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        Microsoft.UI.WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
                        var _appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(myWndId);
                        _appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
                        int width = _appWindow.Size.Width;
                        int height = _appWindow.Size.Height - 35;
                        _appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);
                        _appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
                        var newPosition = new Windows.Graphics.PointInt32(0, 0); // X=200, Y=100
                        _appWindow.Move(newPosition);
                    });
                });
#endif

            });



#if DEBUG
            builder.Logging.AddDebug();
#endif
            // Use the alias to resolve ambiguity and ensure correct type mapping
#if ANDROID
            builder.Services.AddSingleton<ISpeechToText, TaskManagement.Platforms.SpeechToTextImplementation>();

            builder.Services.AddSingleton<ITextToSpeech>(TextToSpeech.Default);
            builder.Services.AddSingleton<IBackgroundTaskService, TaskManagement.Platforms.BackgroundTaskService>();
            builder.Services.AddSingleton<IFirestoreService, WindowsFirestoreService>();
#endif

#if WINDOWS
            // Windows/Desktop gets the REST API version
            builder.Services.AddSingleton<IFirestoreService, FirestoreService>();
#endif
            builder.Services.AddSingleton<DraftTaskRecordViewModel>();
            builder.Services.AddSingleton<DraftTaskPage>();
            builder.Services.AddSingleton<SignInViewModel>();
            builder.Services.AddSingleton<SignInPage>();
            builder.Services.AddSingleton<TaskTypeViewModel>();
            builder.Services.AddSingleton<BackupRestoreViewModel>();
            builder.Services.AddSingleton<SettingViewModel>();
            builder.Services.AddSingleton<TaskRecordViewModel>();
            builder.Services.AddSingleton<TaskDetailsViewModel>();
            builder.Services.AddSingleton<SettingPage>();
            builder.Services.AddSingleton<DetailsPage>();
            builder.Services.AddSingleton<MainPage>();
            return builder.Build();
        }
        private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
        {
            builder.ConfigureLifecycleEvents(events =>
            {
#if ANDROID
                 events.AddAndroid(android => android.OnCreate((activity, state) =>
    {
        // Just initialize. Persistence is ON by default in the native SDK.
        CrossFirebase.Initialize(activity, () => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity);
    }));
#endif
            });

#if ANDROID
            // Only register these on Android. 
            // On Windows, these will crash because the native SDK is missing.
            builder.Services.AddSingleton(_ => CrossFirebaseAuth.Current);
            builder.Services.AddSingleton(_ => CrossFirebaseFirestore.Current);
#endif

            return builder;
        }

    }


}