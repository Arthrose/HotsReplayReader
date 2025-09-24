using System.Resources;

namespace HotsReplayReader
{
    internal class HotsImage
    {
        public Bitmap? Bitmap { get; set; }
        public string Name { get; set; }
        public string? Extension { get; set; }
        public string ResourceName { get; set; }
        public HotsImage(string resourceName = "heroesicon", string imageName = "_Null", string? extension = null)
        {
            Name = imageName;
            ResourceName = resourceName;
            Extension = extension ?? ".png";
            SetBitmap();
        }
        public void SetBitmap()
        {
            object? image = null;
            ResourceManager? resourceManager = null;
            string ResxObjectName = Name;
            switch (ResourceName)
            {
                case "heroesicon":
                    resourceManager = HeroesIcon.ResourceManager;
                    break;
                case "hotsresources":
                    resourceManager = HotsResources.ResourceManager;
                    break;
                case "abilitytalents":
                    resourceManager = AbilityTalents.ResourceManager;
                    break;
                case "emoticons":
                    resourceManager = HotsEmoticons.ResourceManager;
                    ResxObjectName = $@"{ResxObjectName}{Extension}";
                    ResxObjectName = ResxObjectName.Replace("_0.gif", ".gif");
                    break;
                case "minimapicons":
                    resourceManager = MinimapIcons.ResourceManager;
                    break;
                case "matchawards":
                    resourceManager = MatchAwardsImg.ResourceManager;
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
