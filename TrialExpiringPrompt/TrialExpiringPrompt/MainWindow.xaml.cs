// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TrialExpiringPrompt
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            txtStatusIsTrial.Text = $"{App.IsAppTrial}";
            txtDaysToExpiration.Text = $"{App.DaysToExpiration}";
            txtBGTaskPollingFrequency.Text = $"{App.BGTaskPollingFrequency}";
            txtBGTaskRegistered.Text = $"{App.IsBgTaskRegistered}";
        }

    }
}
