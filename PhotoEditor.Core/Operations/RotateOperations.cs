using PhotoEditor.Core.Models;

namespace PhotoEditor.Core.Operations
{
    public class RotateOperation : IImageOperation
    {
        // Поле для хранения угла поворота
        private readonly int _angle;

        // Конструктор с параметрами
        public RotateOperation(int angle)
        {
            if (angle % 90 != 0) throw new ArgumentException("Угол поворота должен быть кратен 90.", nameof(angle));
            // Нормализация угла в диапазон [0, 360)
            _angle = (angle % 360 + 360) % 360; 
        }

        // Свойство названия операции
        public string Name => $"Поворот на {_angle}°";

        // Метод применения операции
        public Picture Apply(Picture source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source), "Нельзя передавать пустой аргумент.");
            // Создание копии исходной картинки
            Picture result = source.Clone();
            int rotation = _angle / 90;
            for (int i = 0; i < rotation; i++)
            {
                result = Rotate90(result);
            }
            return result;
        }

        // Метод поворота на 90 градусов
        private static Picture Rotate90(Picture source)
        {
            // Создание новой картинки с заменой ширины и высоты
            Picture result = new Picture(source.Name, source.Height, source.Width);;
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    Pixel pixel = source.GetPixel(x, y);
                    int newX = source.Height - 1 - y;
                    int newY = x;
                    result.SetPixel(newX, newY, pixel);
                }
            }
            return result;
        }
    }    
}