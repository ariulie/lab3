namespace PhotoEditor.Core.State
{
    public class PictureDto
    {
        public string Name { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}