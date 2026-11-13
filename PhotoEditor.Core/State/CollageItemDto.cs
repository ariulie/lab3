namespace PhotoEditor.Core.State
{
    public class CollageItemDto 
    {
        public PictureDto Picture { get; set; } = new PictureDto();
        public int X { get; set; }
        public int Y { get; set; }
    }
}