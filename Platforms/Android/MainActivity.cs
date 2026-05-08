using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using AndroidX.Core.App;
using TaskManagement.Platforms;

namespace TaskManagement
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Fix for CS1503 and CS1660:
            // The correct overload is: CrossFirebase.Initialize(Activity activity, Func<Activity> activityLocator, FirebaseOptions? firebaseOptions = null, string? name = null)
            // So, pass 'this' as the first argument, and the lambda as the second argument.
            //dipti changed on 2024-06-20
            /*  CrossFirebase.Initialize(this, () => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity);*/

            // 2. Setup Notifications
            SetupNotificationChannel();
            CheckAndRequestNotificationPermissions();

            // 3. Schedule Background Tasks
            var backgroundService = new BackgroundTaskService();
            backgroundService.ScheduleDailyTaskReminder(this);
        }

        private void SetupNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel("task_channel", "Task Reminders", NotificationImportance.Default)
                {
                    Description = "Reminders for pending tasks"
                };

                var manager = (NotificationManager)GetSystemService(NotificationService);
                manager?.CreateNotificationChannel(channel);
            }
        }

        private void CheckAndRequestNotificationPermissions()
        {
            // Request permission for Android 13+
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Permission.Granted)
                {
                    RequestPermissions(new string[] { Android.Manifest.Permission.PostNotifications }, 2001);
                }
            }

            // Redirect to settings if disabled
            if (!NotificationManagerCompat.From(this).AreNotificationsEnabled())
            {
                OpenNotificationSettings();
            }
        }

        private void OpenNotificationSettings()
        {
            var intent = new Intent();
            intent.SetAction("android.settings.APP_NOTIFICATION_SETTINGS");
            intent.PutExtra("android.provider.extra.APP_PACKAGE", PackageName);
            StartActivity(intent);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            // REQUIRED for Firebase Auth flows (Phone auth, Google Sign-in, etc.)
            //   FirebaseAuthImplementation.HandleActivityResultAsync(requestCode, resultCode, data);

            // Your Audio Recording Logic
            if (requestCode == 1001 && resultCode == Result.Ok && data?.Data != null)
            {
                string audioPath = GetRealPathFromURI(data.Data);
                MessagingCenter.Send(this, "AudioRecorded", audioPath);
            }
        }

        private string GetRealPathFromURI(Android.Net.Uri contentUri)
        {
            string[] proj = { MediaStore.Audio.Media.InterfaceConsts.Data };
            using var cursor = ContentResolver?.Query(contentUri, proj, null, null, null);
            if (cursor != null && cursor.MoveToFirst())
            {
                int columnIndex = cursor.GetColumnIndexOrThrow(MediaStore.Audio.Media.InterfaceConsts.Data);
                return cursor.GetString(columnIndex);
            }
            return null;
        }
    }
}
