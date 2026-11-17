using PhotoEditor.Core.Models;

namespace PhotoEditor.Core.Operations
{
    public class CropOperation : IImageOperation
    {
        // Поле для координаты X
        private readonly int _x;
        // Поле для координаты Y
        private readonly int _y;
        // Поле для ширины
        private readonly int _width;
        // Поле для высоты
        private readonly int _height;

        // Свойство названия операции
        public string Name => $"Обрезка {_width}×{_height}";

        // Метод применения операции
        public Picture Apply(Picture source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source), "Исходная картинка не может быть пустой.");
            if (_x < 0 || _y < 0 || _width <= 0 || _height <= 0 || _width > source.Width - _x || _height > source.Height - _y)
                throw new ArgumentException("Область обрезки выходит за границы исходной картинки.");

            // Создание новой картинки нужного размера
            Picture result = new Picture(source.Name, _width, _height);

            // Двойной цикл копирования пикселей
            for (int i = 0; i < _width; i++)
            {
                for (int j = 0; j < _height; j++)
                {
                    Pixel pix = source.GetPixel(_x + i, _y + j);
                    result.SetPixel(i, j, pix);
                }
            }
            return result;
        }

        // Конструктор с параметрами
        public CropOperation(int x, int y, int width, int height)
        {
            // Проверка корректности входных параметров
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "Координата X не может быть отрицательной.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "Координата Y не может быть отрицательной.");
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Ширина обрезки должна быть больше нуля.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Высота обрезки должна быть больше нуля.");

            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }
    }
}