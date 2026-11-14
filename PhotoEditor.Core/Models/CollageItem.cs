namespace PhotoEditor.Core.Models
{
    public class CollageItem
    {
        // Свойство картинки
        public Picture Picture { get; }
        // Свойство координаты X
        public int X { get; }
        // Свойство координаты Y
        public int Y { get; }

        // Конструктор с параметрами
        public CollageItem(Picture picture, int x, int y)
        {
            if (picture is null) throw new ArgumentNullException(nameof(picture), "Картинка не может быть пустой.");
            Picture = picture;
            X = x;
            Y = y;
        }
    }
}