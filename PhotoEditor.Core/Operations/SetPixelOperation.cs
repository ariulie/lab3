using PhotoEditor.Core.Models;

namespace PhotoEditor.Core.Operations
{
    public class SetPixelOperation : IImageOperation
    {
        private readonly int _x;
        private readonly int _y;
        private readonly Pixel _color;

        public string Name => $"Замена пикселя ({_x}, {_y})";

        // Метод применения операции
        public Picture Apply(Picture source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source), "Исходная картинка не может быть пустой.");
            if (_x >= source.Width || _y >= source.Height) throw new ArgumentOutOfRangeException(nameof(_x), "Координаты выходят за пределы исходной картинки.");
            // Создание копии исходной картинки
            Picture result = source.Clone();
            result.SetPixel(_x, _y, _color);
            return result;
        }

        // Конструктор с параметрами
        public SetPixelOperation(int x, int y, Pixel color)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "Координата X не может быть отрицательной.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "Координата Y не может быть отрицательной.");
            _x = x;
            _y = y;
            _color = color;
        }
    } 
}