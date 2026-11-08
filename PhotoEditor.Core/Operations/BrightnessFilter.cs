using PhotoEditor.Core.Models;

namespace PhotoEditor.Core.Operations
{
    public class BrightnessFilter : PixelFilter
    {
        // Поле для хранения изменения яркости
        private readonly int _delta;
        // Свойство названия операции
        public override string Name => _delta >= 0 ? $"Яркость + {_delta}" : $"Яркость {_delta}";
        // Переопределение метода преобразования пикселя
        protected override Pixel PixelTransform(Pixel p)
        {
            return new Pixel(Clamp(p.R + _delta), Clamp(p.G + _delta), Clamp(p.B + _delta), p.A);
        }
        // Конструктор с параметрами
        public BrightnessFilter(int delta)
        {
            if (delta < -255 || delta > 255) throw new ArgumentOutOfRangeException(nameof(delta), "Значение яркости должно быть от -255 до 255.");
            _delta = delta;
        }
    }
}