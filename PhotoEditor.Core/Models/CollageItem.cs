namespace PhotoEditor.Core.Models
{
    public class CollageItem
    {
        public Picture Pic { get; }
        public int X { get; }
        public int Y { get; }
        public CollageItem(Picture picture, int x, int y)
        {
            if (picture is null) throw new ArgumentNullException(nameof(picture), "Картинка не может быть пустой.");
            Pic = picture;
            X = x;
            Y = y;
        }
    }
}