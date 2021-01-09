using Hardcodet.Wpf.TaskbarNotification;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MMT.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace MMT.UI
{
    public partial class MainWindow : MetroWindow
    {
        private readonly ProfileManager _profileManager;
        private readonly TeamsLauncher _teamsLauncher;
        private readonly RegistryManager _registryManager;
        private TaskbarIcon _tray;

        public MainWindow()
        {
            InitializeComponent();
            _profileManager = new ProfileManager();
            _teamsLauncher = new TeamsLauncher();
            _registryManager = new RegistryManager();
            ChangeTabVisibility();
            CreateTray();
            Silent();
            AutoStartCheck();
        }

        private void Silent()
        {
            string[] parameters = Environment.GetCommandLineArgs();
            if (parameters.Length > 1 && parameters[1].Contains("silent"))
                {
                    Show();
                    WindowState = WindowState.Minimized;
                    _tray.Visibility = Visibility.Visible;
                    Visibility = Visibility.Collapsed;

                try
                {
                    var thread = new Thread(() =>
                    {
                        foreach (var item in lstProfiles.Items)
                            if (!item.ToString().StartsWith("[Deaktiviert]"))
                                _teamsLauncher.Start(item.ToString());
                    });
                    thread.Start();
                }
                catch (Exception ex)
                {
                    MessageHelper.Info(ex.Message);
                    txtProfileName.Focus();
                }

                    this.Close();
                }
            else
            {
                Show();
                WindowState = WindowState.Normal;
                _tray.Visibility = Visibility.Collapsed;
                Visibility = Visibility.Visible;
            }

        }

        private void ChangeTabVisibility()
        {
            if (tbiNewProfile.Visibility == Visibility.Visible)
            {
                tbiProfiles.Visibility = Visibility.Visible;
                tbiNewProfile.Visibility = Visibility.Collapsed;
                tbcMain.SelectedItem = tbiProfiles;
                LoadProfiles();
            }
            else
            {
                tbiProfiles.Visibility = Visibility.Collapsed;
                tbiNewProfile.Visibility = Visibility.Visible;
                tbcMain.SelectedItem = tbiNewProfile;
            }
        }

        private void LoadProfiles()
        {
            lstProfiles.Items.Clear();
            List<string> profiles = _profileManager.GetProfiles();
            profiles.ForEach(p => lstProfiles.Items.Add(p));
        }

        private void CreateTray()
        {
            _tray = new TaskbarIcon();
            _tray.Icon = Resource.Taskbar;
            _tray.TrayMouseDoubleClick += TrayMouseDoubleClick;
            _tray.ToolTipText = StaticResources.AppName;
            _tray.Visibility = Visibility.Collapsed;
        }

        private void TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            MetroWindow_StateChanged(sender, e);
        }

        private void AutoStartCheck()
        {
            chkAutoStart.IsChecked = _registryManager.IsApplicationInStartup(StaticResources.AppName);

        }

        private void MetroWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Visibility = Visibility.Visible;    //Visibility = Visibility.Collapsed;
                _tray.Visibility = Visibility.Collapsed;
                // _tray.ShowBalloonTip(StaticResources.AppName, "Dieses Programm läuft im Hintergrund.", BalloonIcon.Info);
            }
            else
            {
                _tray.Visibility = Visibility.Collapsed;
                Visibility = Visibility.Visible;
            }
        }

        private void ChkAutoStart_Click(object sender, RoutedEventArgs e)
        {
            if (chkAutoStart.IsChecked.HasValue && chkAutoStart.IsChecked.Value)
                _registryManager.AddApplicationInStartup(StaticResources.AppName);
            else if (_registryManager.IsApplicationInStartup(StaticResources.AppName))
                _registryManager.RemoveApplicationFromStartup(StaticResources.AppName);
        }

        private void BtnNewProfile_Click(object sender, RoutedEventArgs e)
        {
            txtProfileName.Clear();
            ChangeTabVisibility();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _profileManager.Save(txtProfileName.Text);
                ChangeTabVisibility();
            }
            catch (Exception ex)
            {
                MessageHelper.Info(ex.Message);
                txtProfileName.Focus();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ChangeTabVisibility();
        }

        private void BtnLaunchTeams_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lstProfiles.SelectedItem != null && !lstProfiles.SelectedItem.ToString().StartsWith("[Deaktiviert]"))
                    _teamsLauncher.Start(lstProfiles.SelectedItem.ToString());
            }
            catch (Exception ex)
            {
                MessageHelper.Info(ex.Message);
                txtProfileName.Focus();
            }
        }

        private void BtnLaunchAllTeams_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var thread = new Thread(() =>
                {
                    foreach (var item in lstProfiles.Items)
                        if (!item.ToString().StartsWith("[Deaktiviert]"))
                            _teamsLauncher.Start(item.ToString());
                });
                thread.Start();
            }
            catch (Exception ex)
            {
                MessageHelper.Info(ex.Message);
                txtProfileName.Focus();
            }
        }

        private async void LstProfiles_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                string selectedProfile = lstProfiles.SelectedItem.ToString();
                if (await MessageHelper.Confirm(string.Format("Profil löschen?\nProfilname: {0}", selectedProfile)) == MessageDialogResult.Affirmative)
                {
                    _profileManager.Delete(selectedProfile);
                    LoadProfiles();
                }
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }

        private async void LstProfiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string selectedProfile = lstProfiles.SelectedItem.ToString();
            if (selectedProfile.StartsWith("[Deaktiviert]"))
                _profileManager.Enable(selectedProfile);
            else if (await MessageHelper.Confirm(string.Format("Profil deaktivieren?\nProfilname: {0}", selectedProfile)) == MessageDialogResult.Affirmative)
                _profileManager.Disable(selectedProfile);

            LoadProfiles();
        }

     
    }
}
