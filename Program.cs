using System.Diagnostics;

namespace HotsReplayReader
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Renames itself "HotSReplayReader.exe" to get rid of GitHub version
            string currentExePath = Environment.ProcessPath!;
            string currentDirectory = Path.GetDirectoryName(currentExePath)!;
            string targetName = "HotSReplayReader.exe";
            string targetExePath = Path.Combine(currentDirectory, targetName);
            if (!string.Equals(Path.GetFileName(currentExePath), targetName, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    File.Copy(currentExePath, targetExePath, true);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetExePath,
                        Arguments = $"--delete-old \"{currentExePath}\"",
                        UseShellExecute = true
                    });
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erreur lors de la copie ou du lancement : {ex.Message}");
                }
            }
            else if (args.Length >= 2 && string.Equals(args[0], "--delete-old", StringComparison.OrdinalIgnoreCase))
            {
                string oldExePath = args[1];
                SupprimerAncienFichier(oldExePath);
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new HotsReplayWebReader());
        }

        private static void SupprimerAncienFichier(string filePath)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    break;
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Impossible de supprimer l'ancien fichier : {ex.Message}");
                    break;
                }
            }
        }

        internal static void ExitApp()
        {
            System.Windows.Forms.Application.Exit();
        }
    }
}