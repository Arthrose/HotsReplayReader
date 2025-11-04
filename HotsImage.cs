using System;
using System.Diagnostics;
using System.Drawing;
using System.Resources;

namespace HotsReplayReader
{
    internal class HotsImage
    {
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
                case "heroesicon":
                    resourceManager = Resources.HeroesIcon.ResourceManager;
                    break;
                case "hotsresources":
                    resourceManager = Resources.HotsResources.ResourceManager;
                    break;
                case "abilitytalents":
                    resourceManager = Resources.AbilityTalents.ResourceManager;
                    break;
                case "emoticons":
                    resourceManager = Resources.HotsEmoticons.ResourceManager;
                    ResxObjectName = $@"{ResxObjectName}{Extension}";
                    ResxObjectName = ResxObjectName.Replace("_0.gif", ".gif");
                    break;
                case "minimapicons":
                    resourceManager = Resources.MinimapIcons.ResourceManager;
                    break;
                case "matchawards":
                    resourceManager = Resources.MatchAwardsImg.ResourceManager;
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
