using PhotoEditor.Core.Models;

namespace PhotoEditor.Core.Operations
{
    public class GrayscaleFilter : PixelFilter
    {
        // Свойство названия операции
        public override string Name => "Оттенки серого";

        // Переопределение метода преобразования пикселя
        protected override Pixel PixelTransform(Pixel p)
        {
            // Вычисление оттенков серого по формуле
            byte gray = Clamp(p.R * 0.299 + p.G * 0.587 + p.B * 0.114);
            return new Pixel(gray, gray, gray, p.A);
        }
    }
}