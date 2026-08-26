using System.Diagnostics;

namespace HotsReplayReader.Updater
{
    public partial class HotsReplayReaderUpdater : Form
    {
        private readonly string? _exeLocalPath;
        private readonly string? _exeUpdateUrl;

        public HotsReplayReaderUpdater(string[] args)
        {
            InitializeComponent();

            if (args.Length == 2)
            {
                _exeLocalPath = args[0];
                _exeUpdateUrl = args[1];
                //MessageBox.Show($@"ExeLocalPath: {_exeLocalPath}\n_exeUpdateUrl: {_exeUpdateUrl}", "Arguments reçus", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Invalid arguments. Expected: [ExeLocalPath] [ExeUpdateUrl]", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }
        }

        private async void HotsReplayReaderUpdater_Load(object? sender, EventArgs e)
        {
            bool updateSucceeded;
            string? errorMessage = null;

            try
            {
                await RunUpdateAsync(_exeLocalPath!, _exeUpdateUrl!);
                updateSucceeded = true;
            }
            catch (Exception ex)
            {
                updateSucceeded = false;
                errorMessage = ex.Message;
            }

            if (updateSucceeded)
            {
                SetStatus("Update completed.", marquee: false, progressValue: 100);
                MessageBox.Show("The update was installed successfully.", "Update Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                SetStatus("Update failed.", marquee: false, progressValue: 0);
                MessageBox.Show($"The update failed:{Environment.NewLine}{errorMessage}{Environment.NewLine}{Environment.NewLine}The application will be restarted in its current version.",
                    "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            RestartApplication(_exeLocalPath!);
            Close();
        }

        private async Task RunUpdateAsync(string exeLocalPath, string exeUpdateUrl)
        {
            SetStatus("Closing the application...", marquee: true);
            await WaitAndKillProcessAsync(exeLocalPath);

            SetStatus("Downloading the update...", marquee: false, progressValue: 0);
            string tempDir = Path.Combine(Path.GetTempPath(), "HotsReplayReaderUpdater");
            Directory.CreateDirectory(tempDir);
            string downloadedExePath = Path.Combine(tempDir, Path.GetFileName(exeLocalPath));

            var progress = new Progress<int>(percent =>
                SetStatus($"Downloading the update... {percent}%", marquee: false, progressValue: percent));

            bool downloadOk = await DownloadFileAsync(exeUpdateUrl, downloadedExePath, progress);
            if (!downloadOk)
            {
                throw new Exception("The update download failed.");
            }

            SetStatus("Installing the update...", marquee: true);
            ReplaceExecutable(downloadedExePath, exeLocalPath);
        }

        private void SetStatus(string text, bool marquee, int? progressValue = null)
        {
            if (IsDisposed || Disposing)
                return;

            if (InvokeRequired)
            {
                try
                {
                    Invoke(() => SetStatus(text, marquee, progressValue));
                }
                catch (ObjectDisposedException) { /* Form fermée entre-temps, on ignore */ }
                catch (InvalidOperationException) { /* handle pas encore créé, on ignore */ }
                return;
            }

            lblStatus.Text = text;
            progressBar.Style = marquee ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;

            if (!marquee && progressValue.HasValue)
            {
                progressBar.Value = Math.Clamp(progressValue.Value, 0, 100);
            }
        }

        private static async Task WaitAndKillProcessAsync(string exeLocalPath)
        {
            string processName = Path.GetFileNameWithoutExtension(exeLocalPath);

            // Laisse une chance au process de se fermer proprement
            const int maxAttempts = 10; // 10 x 500 ms = 5 secondes
            for (int i = 0; i < maxAttempts; i++)
            {
                if (Process.GetProcessesByName(processName).Length == 0)
                    return;

                await Task.Delay(500);
            }

            // Toujours ouvert après le délai -> on force la fermeture
            foreach (var process in Process.GetProcessesByName(processName))
            {
                try
                {
                    process.Kill();
                    await process.WaitForExitAsync();
                }
                catch
                {
                    // le process a peut-être déjà été fermé entre-temps, on ignore
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        private static async Task<bool> DownloadFileAsync(string url, string destinationPath, IProgress<int> progress)
        {
            try
            {
                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1;

                await using var httpStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;
                int lastReportedPercent = -1;

                while ((bytesRead = await httpStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                    {
                        int percent = (int)(totalRead * 100 / totalBytes);
                        if (percent != lastReportedPercent)
                        {
                            lastReportedPercent = percent;
                            progress.Report(percent);
                        }
                    }
                }

                // Si la taille totale était inconnue, on affiche 100% une fois terminé
                if (totalBytes <= 0)
                {
                    progress.Report(100);
                }

                return File.Exists(destinationPath) && new FileInfo(destinationPath).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static void ReplaceExecutable(string downloadedExePath, string exeLocalPath)
        {
            if (File.Exists(exeLocalPath))
            {
                File.Delete(exeLocalPath);
            }

            File.Move(downloadedExePath, exeLocalPath);
        }

        private static void RestartApplication(string exeLocalPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = exeLocalPath,
                    WorkingDirectory = Path.GetDirectoryName(exeLocalPath) ?? string.Empty,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to restart the application:{Environment.NewLine}{ex.Message}", "Restart Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}