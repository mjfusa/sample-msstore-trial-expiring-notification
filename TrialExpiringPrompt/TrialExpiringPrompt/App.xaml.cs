// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Services.Store;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TrialExpiringPrompt
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        public static Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; }
        public static bool IsAppTrial = false;
        public static bool IsBgTaskRegistered = false;
        public static int DaysToExpiration = 0;
        public static uint BGTaskPollingFrequency = 240; // Poll every four hours

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // TODO Determine if Trial
            var TimeRemaining = await IsTrial();

            if (TimeRemaining != new TimeSpan(0))
            {
                IsAppTrial = true;
                DaysToExpiration = TimeRemaining.Days;

                RegisterBackgroundTaskPrompt();
                
                // Save time remaining in local storage to fetch in background task
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["TrialTimeRemaining"] = TimeRemaining;

                // Get the app-level dispatcher
                DispatcherQueue = global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

                // Register for toast activation. Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
                ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;

                // If we weren't launched by a toast, launch our window like normal.
                // Otherwise if launched by a toast, our OnActivated callback will be triggered
                if (!ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
                {
                    LaunchAndBringToForegroundIfNeeded();
                }

            } else
            {
                foreach (var cur in BackgroundTaskRegistration.AllTasks)
                {

                    if ((cur.Value.Name == "BackgroundPrompt") || (cur.Value.Name == "BackgroundPromptTime"))
                    {
                        cur.Value.Unregister(true);
                    }
                }
                LaunchAndBringToForegroundIfNeeded();
            }
        }

        private void RegisterBackgroundTaskPrompt()
        {
            TimeTrigger timeTrigger = new TimeTrigger(BGTaskPollingFrequency, false);
            SystemTrigger tzTrigger = new SystemTrigger(SystemTriggerType.TimeZoneChange,false);

            RegisterBackgroundTask("BackgroundPrompt.BackgroundTaskPrompt", "BackgroundPrompt", tzTrigger, null);
            var res = RegisterBackgroundTask("BackgroundPrompt.BackgroundTaskPrompt", "BackgroundPromptTime", timeTrigger, null);
            if (res != null)
            {
                IsBgTaskRegistered = true;
            }

        }
        //
        // Register a background task with the specified taskEntryPoint, name, trigger,
        // and condition (optional).
        //
        // taskEntryPoint: Task entry point for the background task.
        // taskName: A name for the background task.
        // trigger: The trigger for the background task.
        // condition: Optional parameter. A conditional event that must be true for the task to fire.
        //
        public static BackgroundTaskRegistration RegisterBackgroundTask(string taskEntryPoint,
                                                                        string taskName,
                                                                        IBackgroundTrigger trigger,
                                                                        IBackgroundCondition condition)
        {
            //
            // Check for existing registrations of this background task.
            //

            foreach (var cur in BackgroundTaskRegistration.AllTasks)
            {

                if (cur.Value.Name == taskName)
                {
                    //
                    // The task is already registered.
                    //

                    cur.Value.Unregister(true);
                }
            }

            //
            // Register the background task.
            //

            var builder = new BackgroundTaskBuilder();

            builder.Name = taskName;
            builder.TaskEntryPoint = taskEntryPoint;
            builder.SetTrigger(trigger);

            if (condition != null)
            {

                builder.AddCondition(condition);
            }

            BackgroundTaskRegistration task = builder.Register();

            return task;
        }

        private async Task<TimeSpan> IsTrial()
        {
            TimeSpan res= new TimeSpan(0);
            var license = await StoreContext.GetDefault().GetAppLicenseAsync();
            if (license.IsTrial)
            {
                var expDate = license.ExpirationDate;
                //var expDate = new DateTimeOffset(2022, 12, 4, 13, 1, 1, 0, new TimeSpan(1, 0, 0));

                var timeRemaining = expDate - DateTimeOffset.UtcNow;
                res = timeRemaining;
            }
            return res;
        }
        private void LaunchAndBringToForegroundIfNeeded()
        {
            if (m_window == null)
            {
                m_window = new MainWindow();
                m_window.Activate();

                WindowHelper.ShowWindow(m_window);
            }
            else
            {
                WindowHelper.ShowWindow(m_window);
            }
        }
        private async void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            // Use the dispatcher from the window if present, otherwise the app dispatcher
            var dispatcherQueue = m_window?.DispatcherQueue ?? App.DispatcherQueue;

            dispatcherQueue.TryEnqueue(async delegate
            {
                var args = ToastArguments.Parse(e.Argument);

                switch (args["action"])
                {
                    // Send a background message
                    case "purchase":
                        // Prompt user to purchase
                        var res = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://pdp/?ProductId=9NKSVZGRD2QW"));

                        // If the UI app isn't open
                        if (m_window == null)
                        {
                            // Close since we're done
                            Process.GetCurrentProcess().Kill();
                        }

                        break;

                }
            });
        }

        private static class WindowHelper
        {
            [DllImport("user32.dll")]
            private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool SetForegroundWindow(IntPtr hWnd);

            public static void ShowWindow(Window window)
            {
                // Bring the window to the foreground... first get the window handle...
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

                // Restore window if minimized... requires DLL import above
                ShowWindow(hwnd, 0x00000009);

                // And call SetForegroundWindow... requires DLL import above
                SetForegroundWindow(hwnd);
            }
        }

        private Window m_window;
    }
}
