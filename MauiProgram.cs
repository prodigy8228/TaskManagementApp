using CommunityToolkit.Maui;
//using CommunityToolkit.Maui.Core;
using Microsoft.Maui.LifecycleEvents;
using TaskManagement.View;
#if ANDROID
using TaskManagement.Services;   // for IBackgroundTaskService
using TaskManagement.Platforms;    // for BackgroundTaskService
#endif
using Microsoft.Extensions.Logging;

namespace TaskManagement
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>().UseMauiCommunityToolkit().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            }).UseMauiCommunityToolkitMediaElement();
#if WINDOWS
                            builder.ConfigureLifecycleEvents(events => // Ensure this method is available
                            {
                                events.AddWindows(wndLifeCycleBuilder =>
                                {
                                    wndLifeCycleBuilder.OnWindowCreated(window =>
                                    {
                                        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                                        Microsoft.UI.WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
                                        var _appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(myWndId);
                                        _appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
                        int width = _appWindow.Size.Width;
                        int height = _appWindow.Size.Height-35;
                         _appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);
                        _appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
                        var newPosition = new Windows.Graphics.PointInt32(0,0); // X=200, Y=100
            _appWindow.Move(newPosition);
                                    });
                                });
                            });

#endif
#if DEBUG
            builder.Logging.AddDebug();
#endif
            // Use the alias to resolve ambiguity and ensure correct type mapping
#if ANDROID
            builder.Services.AddSingleton<ISpeechToText, TaskManagement.Platforms.SpeechToTextImplementation>();

            builder.Services.AddSingleton<ITextToSpeech>(TextToSpeech.Default);
            builder.Services.AddSingleton<IBackgroundTaskService, TaskManagement.Platforms.BackgroundTaskService>();

#endif
            builder.Services.AddSingleton<MISDatabase>();
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
    }
}