using System.Collections.Concurrent;
using System.Resources;

namespace HotsReplayReader
{
    internal class HotsImage
    {
        private static readonly string CacheRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HotsReplayReader", "cache");
        private const string BaseCdnUrl = "https://cdn.jsdelivr.net/gh/HeroesToolChest/heroes-images@main/heroesimages/";

        // Évite les téléchargements concurrents multiples pour la même image.
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> DownloadLocks = new();
        public static async Task<string?> GetOrDownloadAsync(HttpClient httpClient, string relativePath)
        {
            relativePath = relativePath.Replace('\\', '/').TrimStart('/');
            string localPath = Path.Combine(CacheRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(localPath))
                return localPath;

            // Un seul téléchargement à la fois par chemin, même si plusieurs requêtes arrivent en parallèle.
            SemaphoreSlim gate = DownloadLocks.GetOrAdd(relativePath, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                // Revérifier après avoir acquis le verrou (un autre thread a peut-être déjà téléchargé entre-temps).
                if (File.Exists(localPath))
                    return localPath;

                string url = BaseCdnUrl + relativePath;

                using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                    return null;

                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                // Téléchargement vers un fichier temporaire puis rename atomique,
                // pour éviter qu'un lecteur concurrent lise un fichier partiellement écrit.
                string tempPath = localPath + ".tmp";
                await using (Stream input = await response.Content.ReadAsStreamAsync())
                await using (FileStream output = File.Create(tempPath))
                {
                    await input.CopyToAsync(output);
                }
                File.Move(tempPath, localPath, overwrite: true);

                return localPath;
            }
            catch { return null; }// échec réseau : pas de cache, pas d'image (le handler gérera le fallback) }
            finally { gate.Release(); }
        }
        public Bitmap? Bitmap { get; set; }
        public string Name { get; set; }
        public string? Extension { get; set; }
        public string ResourceName { get; set; }
        private enum CropDirection { Left, Right, Top, Bottom }
        public HotsImage(string resourceName = "heroesicon", string imageName = "_Null", string? extension = null, string? queryActions = null)
        {
            Name = imageName;
            ResourceName = resourceName;
            Extension = extension ?? ".png";
            SetBitmap();

            if (queryActions != null && Bitmap != null)
            {
                var actions = queryActions.Split(';');
                foreach (var action in actions)
                {
                    var parts = action.Split(':');
                    if (parts.Length < 2) continue;

                    var actionName = parts[0].ToLower();
                    var parameters = parts[1].Split(',');

                    switch (actionName)
                    {
                        case "crop":
                            if (parameters.Length == 2 &&
                                Enum.TryParse<CropDirection>(Capitalize(parameters[0]), out var dir) &&
                                int.TryParse(parameters[1], out int px)
                            )
                            {
                                Bitmap = CropImage(Bitmap, dir, px);
                            }
                            break;
                        case "border":
                            if (parameters.Length == 2 &&
                                int.TryParse(parameters[1], out int borderSize))
                            {
                                Bitmap = AddBorder(Bitmap, parameters[0], borderSize);
                            }
                            break;
                    }
                }
            }
        }
        private static string Capitalize(string s) => char.ToUpperInvariant(s[0]) + s[1..].ToLower();
        private static Bitmap CropImage(Bitmap source, CropDirection direction, int pixels)
        {
            int x = 0, y = 0, width = source.Width, height = source.Height;

            switch (direction)
            {
                case CropDirection.Left:
                    x = pixels;
                    width = source.Width - pixels;
                    break;
                case CropDirection.Right:
                    width = source.Width - pixels;
                    break;
                case CropDirection.Top:
                    y = pixels;
                    height = source.Height - pixels;
                    break;
                case CropDirection.Bottom:
                    height = source.Height - pixels;
                    break;
            }

            if (width <= 0 || height <= 0)
                throw new ArgumentException("Invalid crop parameters.");

            Bitmap cropped = new(width, height, source.PixelFormat);
            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.DrawImage(source,
                    new Rectangle(0, 0, width, height),
                    new Rectangle(x, y, width, height),
                    GraphicsUnit.Pixel);
            }
            return cropped;
        }
        private static Bitmap AddBorder(Bitmap source, string borderColor, int borderSize)
        {
            int newWidth = source.Width + 2 * borderSize;
            int newHeight = source.Height + 2 * borderSize;

            // Convert hex string to Color
            Color color = ColorTranslator.FromHtml(borderColor);

            Bitmap output = new(newWidth, newHeight, source.PixelFormat);

            using (Graphics g = Graphics.FromImage(output))
            {
                // Dessine fond de la couleur de la bordure partout
                using (SolidBrush brush = new(color))
                {
                    g.FillRectangle(brush, 0, 0, newWidth, newHeight);
                }
                // Dessine l'image d'origine au centre
                g.DrawImage(source, borderSize, borderSize, source.Width, source.Height);
            }

            return output;
        }
        public void SetBitmap()
        {
            object? image = null;
            ResourceManager? resourceManager = null;
            string ResxObjectName = Name;
            switch (ResourceName)
            {
                case "hotsresources":
                    resourceManager = Resources.HotsResources.ResourceManager;
                    break;
            }
            if (resourceManager != null)
            {
                image = resourceManager.GetObject(ResxObjectName);
            }

            if (image is byte[])
            {
                Bitmap = ByteToImage(image as byte[]);
            }
            else if (image is Bitmap)
            {
                Bitmap = image as Bitmap;
            }
            else
            {
                Bitmap = null;
            }
        }
        private static Bitmap? ByteToImage(byte[]? blob)
        {
            if (blob == null)
            {
                return null;
            }
            using MemoryStream mStream = new(blob);
            return new Bitmap(mStream, false);
        }
    }
}
