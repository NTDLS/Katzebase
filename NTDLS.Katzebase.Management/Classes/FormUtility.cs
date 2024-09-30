namespace NTDLS.Katzebase.Management.Classes
{
    internal static class FormUtility
    {
        private static int nextNewFileName = 1;

        public static Image TransparentImage(Image image)
        {
            Bitmap toolBitmap = new(image);
            toolBitmap.MakeTransparent(Color.Magenta);
            return toolBitmap;
        }

        public static Image ImageFromBytes(byte[] imageBytes)
        {
            using (var ms = new MemoryStream(imageBytes))
            {
                return Image.FromStream(ms);
            }
        }

        public static string GetNextNewFileName()
        {
            return $"Untitled {(nextNewFileName++):n0}.kbs";
        }
    }
}
