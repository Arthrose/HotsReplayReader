using System.Resources;

namespace HotsReplayReader
{
    internal class hotsImage
    {
        public Bitmap Bitmap { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public string ResourceName { get; set; }
        public hotsImage(string resourceName = "heroesicon", string imageName = "_Null", string? extension = null)
        {
            this.Name = imageName;
            this.ResourceName = resourceName;
            this.Extension = extension;
            setBitmap();
        }
        public void setBitmap()
        {
            object image = null;
            ResourceManager resourceManager = null;
            string ResxObjectName = Name;
            switch (ResourceName)
            {
                case "heroesicon":
                    resourceManager = heroesIcon.ResourceManager;
                    break;
                case "hotsresources":
                    resourceManager = hotsResources.ResourceManager;
                    break;
                case "abilitytalents":
                    resourceManager = abilityTalents.ResourceManager;
                    break;
                case "emoticons":
                    resourceManager = hotsEmoticons.ResourceManager;
                    ResxObjectName = $@"{ResxObjectName}{Extension}";
                    ResxObjectName = ResxObjectName.Replace("_0.gif", ".gif");
                    break;
                case "minimapicons":
                    resourceManager = minimapIcons.ResourceManager;
                    break;
            }
            if (resourceManager != null)
            {
                image = resourceManager.GetObject(ResxObjectName);
            }
            Bitmap = ByteToImage(image as byte[]);
        }
        private static Bitmap ByteToImage(byte[] blob)
        {
            if (blob == null)
            {
                return null;
            }
            using (MemoryStream mStream = new MemoryStream(blob))
            {
                return new Bitmap(mStream, false);
            }
        }
    }
}
