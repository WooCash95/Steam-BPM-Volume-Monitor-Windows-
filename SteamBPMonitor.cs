using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

class TrayApp : ApplicationContext
{
    NotifyIcon notifyIcon;
    MenuItem statusMenuItem;
    MenuItem attributionMenuItem;
    MenuItem getBeerMenuItem;
    MenuItem runStartupMenuItem;
    MenuItem exitMenuItem;

    string mainApp = "steamwebhelper.exe";
    string nircmdPath;
    string startupRegName = "SteamBigPictureVolume";
    string startupRegEnableKey = "SteamBigPictureVolumeEnabled";
    string startupRegPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

    string currentVolumeState = null;
    Timer timer;

    public TrayApp()
    {
        nircmdPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(exePath), "nircmd.exe");

        UpdateStartupPathIfEnabled();  // <-- Ensure correct path is registered at startup

        notifyIcon = new NotifyIcon();
        try
        {
            string iconPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(exePath), "SteamBigPictureVolume.ico");
            notifyIcon.Icon = new Icon(iconPath);
        }
        catch
        {
            notifyIcon.Icon = SystemIcons.Application;
        }
        notifyIcon.Text = "Steam Big Picture Volume";
        notifyIcon.Visible = true;

        ContextMenu contextMenu = new ContextMenu();

        var titleMenuItem = new MenuItem("Big Picture Volume Monitor v.1") { Enabled = false };

        statusMenuItem = new MenuItem("State: Unknown") { Enabled = false };

        attributionMenuItem = new MenuItem("Made by @WooCash95") { Enabled = true };
        attributionMenuItem.Click += (s, e) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://x.com/woocash95",
                    UseShellExecute = true
                });
            }
            catch { }
        };

        getBeerMenuItem = new MenuItem("Get dev a beer - Thanks!") { Enabled = true };
        getBeerMenuItem.Click += (s, e) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://paypal.me/lmoczulski",
                    UseShellExecute = true
                });
            }
            catch { }
        };

        runStartupMenuItem = new MenuItem("Run on Startup") { Checked = IsStartupEnabled() };
        exitMenuItem = new MenuItem("Exit");

        contextMenu.MenuItems.Add(titleMenuItem);
        contextMenu.MenuItems.Add(statusMenuItem);
        contextMenu.MenuItems.Add(attributionMenuItem);
        contextMenu.MenuItems.Add(getBeerMenuItem);
        contextMenu.MenuItems.Add("-");
        contextMenu.MenuItems.Add(runStartupMenuItem);
        contextMenu.MenuItems.Add("-");
        contextMenu.MenuItems.Add(exitMenuItem);

        notifyIcon.ContextMenu = contextMenu;

        runStartupMenuItem.Click += (s, e) =>
        {
            if (runStartupMenuItem.Checked)
            {
                DisableStartup();
                runStartupMenuItem.Checked = false;
            }
            else
            {
                EnableStartup();
                runStartupMenuItem.Checked = true;
            }
        };

        exitMenuItem.Click += (s, e) =>
        {
            timer.Stop();
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            Application.Exit();
        };

        timer = new Timer { Interval = 1000 };
        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        bool bigPictureActive = IsBigPictureActive();

        if (bigPictureActive && currentVolumeState != "on")
        {
            RunNirCmdSetAppVolume(1.0);
            currentVolumeState = "on";
            statusMenuItem.Text = "State: ON";
            notifyIcon.Text = "Steam Big Picture Mode Volume - ON";
        }
        else if (!bigPictureActive && currentVolumeState != "off")
        {
            RunNirCmdSetAppVolume(0.0);
            currentVolumeState = "off";
            statusMenuItem.Text = "State: OFF";
            notifyIcon.Text = "Steam Big Picture Mode Volume - OFF";
        }
    }

    private bool IsBigPictureActive()
    {
        foreach (var proc in Process.GetProcesses())
        {
            try
            {
                if (!string.IsNullOrEmpty(proc.MainWindowTitle) &&
                    proc.MainWindowTitle.Contains("Steam Big Picture Mode"))
                    return true;
            }
            catch { }
        }
        return false;
    }

    private void RunNirCmdSetAppVolume(double volume)
    {
        if (!System.IO.File.Exists(nircmdPath)) return;

        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = nircmdPath,
                Arguments = $"setappvolume {mainApp} {volume.ToString("0.0")}",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
        }
        catch { }
    }

    private bool IsStartupEnabled()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(startupRegPath, false))
            {
                if (key == null) return false;
                object enabled = key.GetValue(startupRegEnableKey);
                return enabled != null && enabled.ToString() == "1";
            }
        }
        catch { return false; }
    }

    private void EnableStartup()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(startupRegPath, true))
            {
                if (key == null) return;
                key.SetValue(startupRegName, $"\"{exePath}\"");
                key.SetValue(startupRegEnableKey, 1);
            }
        }
        catch { }
    }

    private void DisableStartup()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(startupRegPath, true))
            {
                if (key == null) return;
                key.DeleteValue(startupRegName, false);
                key.DeleteValue(startupRegEnableKey, false);
            }
        }
        catch { }
    }

    private void UpdateStartupPathIfEnabled()
    {
        try
        {
            if (!IsStartupEnabled()) return;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(startupRegPath, true))
            {
                if (key == null) return;
                string current = key.GetValue(startupRegName) as string;
                string expected = $"\"{exePath}\"";

                if (!string.Equals(current, expected, StringComparison.OrdinalIgnoreCase))
                {
                    key.SetValue(startupRegName, expected); // Update to new path
                }
            }
        }
        catch { }
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApp());
    }
}
