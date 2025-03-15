namespace HotsReplayReader
{
    internal class hotsImage
    {
        public Bitmap Bitmap { get; set; }
        public string Name { get; set; }
        public string ResourceName { get; set; }
        public hotsImage(string resourceName = "heroesicon", string imageName = "_Null")
        {
            this.Name = imageName;
            this.ResourceName = resourceName;
            setBitmap();
        }
        public void setBitmap()
        {
            if (ResourceName == "heroesicon")
            {
                var resourceManager = heroesIcon.ResourceManager;
                object image = resourceManager.GetObject(Name);
                Bitmap = ByteToImage((byte[])image);
            }
            else if (ResourceName == "hotsresources")
            {
                var resourceManager = hotsResources.ResourceManager;
                object image = resourceManager.GetObject(Name);
                Bitmap = ByteToImage((byte[])image);
            }
            else if (ResourceName == "abilitytalents")
            {
                var resourceManager = abilityTalents.ResourceManager;
                object image = resourceManager.GetObject(Name);
                Bitmap = ByteToImage((byte[])image);
            }
        }

        public Bitmap getBitmap(string name)
        {
            Name = name;
            setBitmap();
            return Bitmap;
        }

        private static Bitmap ByteToImage(byte[] blob)
        {
            MemoryStream mStream = new MemoryStream();
            byte[] pData = blob;
            mStream.Write(pData, 0, Convert.ToInt32(pData.Length));
            Bitmap bm = new Bitmap(mStream, false);
            mStream.Dispose();
            return bm;
        }
    }
}
