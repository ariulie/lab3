using PhotoEditor.Core.Models;

namespace PhotoEditor.Core.Operations
{
    public class TextOperation : IImageOperation
    {
        // Поле для хранения текста
        private readonly string _text;
        // Поле для хранения координаты X
        private readonly int _x;
        // Поле для хранения координаты Y
        private readonly int _y;
        // Поле для хранения масштаба
        private readonly int _scale;
        // Поле для хранения цвета текста
        private readonly Pixel _color;

        // Свойство названия операции
        public string Name => $"Надпись '{_text}'";

        // Конструктор с параметрами
        public TextOperation(string text, int x, int y, Pixel color, int scale)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Текст не может быть пустым.", nameof(text));
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "Координата X не может быть отрицательной.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "Координата Y не может быть отрицательной.");
            if (scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale), "Масштаб должен быть больше нуля.");
        
            _text = text;
            _x = x;
            _y = y;
            _color = color;
            _scale = scale;
        }

        // Метод применения операции
        public Picture Apply(Picture source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source), "Исходная картинка не может быть пустой.");
            // Создание копии исходной картинки
            Picture result = source.Clone();
            // Проход по каждому символу текста
            for (int i = 0; i < _text.Length; i++)
            {
                char c = _text[i];
                // Получение глифа символа
                string[] glyph = PixelFont.GetGlyph(c);
                // Вычисление стартовой координаты символа с учётом масштаба
                int startX = _x + i * (PixelFont.Width + 1) * _scale;
                for (int row = 0; row < PixelFont.Height; row++)
                {
                    for (int col = 0; col < PixelFont.Width; col++)
                    {
                        if (glyph[row][col] == '#')
                        {
                            // Отрисовка закрашенного пикселя с учётом масштаба
                            for (int dy = 0; dy < _scale; dy++)
                            {
                                for (int dx = 0; dx < _scale; dx++)
                                {
                                    int targetX = startX + col * _scale + dx;
                                    int targetY = _y + row * _scale + dy;
                                    if (result.InBounds(targetX, targetY))
                                    {
                                        result.SetPixel(targetX, targetY, _color);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}