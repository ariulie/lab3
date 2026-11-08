using PhotoEditor.Core.Models;

namespace PhotoEditor.Core.Operations
{
    public class ChannelFilter : PixelFilter
    {
        // Поле для хранения множителя красного канала
        private readonly double _r;
        // Поле для хранения множителя зеленого канала
        private readonly double _g;
        // Поле для хранения множителя синего канала
        private readonly double _b;

        // Свойство названия операции
        public override string Name => $"Множители каналов: R = {_r}, G = {_g}, B = {_b}";

        // Переопределение метода преобразования пикселя
        protected override Pixel PixelTransform(Pixel p)
        {
            return new Pixel(Clamp(p.R * _r), Clamp(p.G * _g), Clamp(p.B * _b), p.A);
        }

        // Конструктор с параметрами
        public ChannelFilter(double r, double g, double b)
        {
            if (r < 0) throw new ArgumentOutOfRangeException(nameof(r), "Множитель R не может быть отрицательным.");
            if (g < 0) throw new ArgumentOutOfRangeException(nameof(g), "Множитель G не может быть отрицательным.");
            if (b < 0) throw new ArgumentOutOfRangeException(nameof(b), "Множитель B не может быть отрицательным.");
            _r = r;
            _g = g;
            _b = b;
        }
    }
}