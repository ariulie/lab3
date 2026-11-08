using PhotoEditor.Core.Models;

namespace PhotoEditor.Core.Operations
{
    public class InvertFilter : PixelFilter
    {
        // Свойство названия операции
        public override string Name => "Негатив";

        // Переопределение метода преобразования пикселя
        protected override Pixel PixelTransform(Pixel p)
        {
            // Инверсия цветовых каналов
            Pixel resultPix = new Pixel((byte)(255 - p.R), (byte)(255 - p.G), (byte)(255 - p.B), p.A);
            return resultPix;
        }
    }
}