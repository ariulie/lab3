namespace PhotoEditor.Core.State
{
    public class CollageDto
    {
        public string Name { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public byte BackgroundR { get; set; }
        public byte BackgroundG { get; set; }
        public byte BackgroundB { get; set; }
        public byte BackgroundA { get; set; }
        public List<CollageItemDto> Items { get; set; } = new List<CollageItemDto>();
    }
}