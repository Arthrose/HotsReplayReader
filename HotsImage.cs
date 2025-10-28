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
        private enum CropDirection
        {
            Left,
            Right,
            Top,
            Bottom
        }
        public HotsImage(string resourceName = "heroesicon", string imageName = "_Null", string? extension = null, string? action = null)
        {
            Name = imageName;
            ResourceName = resourceName;
            Extension = extension ?? ".png";
            SetBitmap();

            if (action != null && Bitmap != null)
            {
                if (action.Split('_').Length >= 3)
                {
                    if (action.Split('_')[0] == "crop")
                    {
                        string uriDirection = action.Split("_")[1];
                        CropDirection direction;
                        switch (uriDirection)
                        {
                            case "left":
                                direction = CropDirection.Left;
                                break;
                            case "right":
                                direction = CropDirection.Right;
                                break;
                            case "top":
                                direction = CropDirection.Top;
                                break;
                            case "bottom":
                                direction = CropDirection.Bottom;
                                break;
                            default:
                                direction = CropDirection.Left;
                                break;
                        }

                        int uriPixels;
                        if (int.TryParse(action.Split("_")[2], out uriPixels))
                            Bitmap = CropImage(Bitmap, direction, uriPixels);
                    }
                }
            }
        }
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

            Bitmap cropped = new Bitmap(width, height, source.PixelFormat);
            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.DrawImage(source,
                    new Rectangle(0, 0, width, height),
                    new Rectangle(x, y, width, height),
                    GraphicsUnit.Pixel);
            }
            return cropped;
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
