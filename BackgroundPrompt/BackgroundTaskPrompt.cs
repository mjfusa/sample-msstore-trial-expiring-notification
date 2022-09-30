// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Notifications;
using System;
using System.Diagnostics;
using System.IO;
using Windows.ApplicationModel.Background;
using Windows.Services.Store;
using Windows.UI.Notifications;

namespace BackgroundPrompt
{
    public sealed class BackgroundTaskPrompt : IBackgroundTask
    {
        BackgroundTaskDeferral? _deferral;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            Debug.WriteLine("Background " + taskInstance.Task.Name + " Starting...");

            // Perform the background task.
            SendToast();


            _deferral.Complete();
        }

        private static async void SendToast()
        {
            TimeSpan res = new TimeSpan(0);
            var license = await StoreContext.GetDefault().GetAppLicenseAsync();
#if DEBUG
            if (true)
#else
            if (license.IsTrial)
#endif
            {
#if DEBUG
                DateTime sourceDate = new DateTime(2022, 12, 24, 13, 1, 1);
                DateTime utcTime = DateTime.SpecifyKind(sourceDate, DateTimeKind.Utc);
                var expDate = new DateTimeOffset(utcTime, TimeSpan.Zero);
#else
                var expDate = license.ExpirationDate;
#endif
                var timeRemaining = expDate - DateTimeOffset.UtcNow;
                int daysToExpire = ((TimeSpan)timeRemaining).Days;

                var imageUri = Path.GetFullPath(@"Images\contoso.jpeg");
                var tcb = new ToastContentBuilder()
                .AddAppLogoOverride(new Uri(imageUri), ToastGenericAppLogoCrop.Default, "logo", null)
                .AddText("Purchase Contoso EP 2023")
                .AddText($"Thanks for using Contoso EP 2023! Your trial expires in {daysToExpire} days.")
                .AddButton(new ToastButton()
                .SetContent("Click to purchase Contoso EP 2023")
                .AddArgument("action", "purchase")
                .SetBackgroundActivation());
                var toastXml = tcb.GetXml();
                var toast = new ToastNotification(toastXml);
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
        }
    }

}