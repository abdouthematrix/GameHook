using EasyHook;
using System;
using System.IO;
using System.Runtime.Remoting;
using System.Windows;
using System.Windows.Forms; // FolderBrowserDialog
using System.Windows.Media;

namespace GameHook.Launcher
{    
    public partial class MainWindow : Window
    {
        private bool _isLaunching;
        // IPC Variables
        private string _channelName = null;
        private HookInterface _ipcInterface;
        public MainWindow()
        {
            InitializeComponent();

            // Initialize IPC Server
            _ipcInterface = new HookInterface();
            _ipcInterface.MessageReceived += OnIpcLog;
            RemoteHooking.IpcCreateServer(
                ref _channelName,
                WellKnownObjectMode.Singleton,
                _ipcInterface);

            MsgServerDirectoryTextBox.Text = "C:\\Users\\AbdouMatrix\\Downloads\\Conquer_Online Source_v6159\\Source\\";//AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            MsgServerExecutableTextBox.Text = "MsgServer.exe";
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the MsgServer directory";
                if (Directory.Exists(MsgServerDirectoryTextBox.Text))
                {
                    dialog.SelectedPath = MsgServerDirectoryTextBox.Text;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    MsgServerDirectoryTextBox.Text = dialog.SelectedPath;
                }
            }
        }
        private async void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLaunching)
            {
                return;
            }

            string MsgServerDirectory = MsgServerDirectoryTextBox.Text.Trim();
            string MsgServerExecutable = MsgServerExecutableTextBox.Text.Trim();
            string arguments = ArgumentsTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(MsgServerDirectory) || string.IsNullOrWhiteSpace(MsgServerExecutable))
            {
                SetStatus("MsgServer directory and executable are required.", isError: true);
                return;
            }

            SetLaunching(true);
            LogTextBox.Clear();
            SetStatus("Launching...", isError: false);

            string exePath = Path.Combine(MsgServerDirectory, MsgServerExecutable);

            if (!File.Exists(exePath))
            {
                SetStatus($"MsgServer executable not found: {exePath}", isError: true);
                return;
            }
                        
            string dllPath = "GameHook.dll";

            Log($"Loading {dllPath} module");

            if (!File.Exists(dllPath))
            {
                SetStatus($"Could not find {dllPath} -- build GameHook first.", isError: true);
                return;
            }
            try
            {
                RemoteHooking.CreateAndInject(
                    InEXEPath: exePath,
                    InCommandLine: arguments,
                    InProcessCreationFlags: 0,
                    InjectionOptions.DoNotRequireStrongName,
                    InLibraryPath_x86: dllPath,
                    InLibraryPath_x64: dllPath,
                    OutProcessId: out int pid,
                    InPassThruArgs: _channelName);

                Log($"Started {Path.GetFileName(exePath)} (PID {pid}) and injected {Path.GetFileName(dllPath)}.");
                SetStatus($"Launched successfully (PID {pid}).", isError: false);
            }
            catch (Exception ex)
            {               
                SetStatus($"Injection failed: {ex.Message}", isError: true);
                return;
            }            

            SetLaunching(false);
        }

        // Add this method to handle IPC logs safely on the UI thread
        private void OnIpcLog(string message)
        {
            Log("[DLL] " + message);  
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText(message + Environment.NewLine);
                LogScrollViewer.ScrollToEnd();
            });
        }

        private void SetLaunching(bool launching)
        {
            _isLaunching = launching;
            LaunchButton.IsEnabled = !launching;
            BrowseButton.IsEnabled = !launching;
        }

        private void SetStatus(string text, bool isError)
        {
            StatusTextBlock.Text = text;
            StatusTextBlock.Foreground = isError
                ? new SolidColorBrush(Color.FromRgb(0xE0, 0x6C, 0x5A))
                : (Brush)FindResource("TextSecondaryBrush");
        }
    }
}
