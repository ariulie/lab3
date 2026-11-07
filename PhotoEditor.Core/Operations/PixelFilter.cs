using PhotoEditor.Core.Models;
using System;

namespace PhotoEditor.Core.Operations
{
    public abstract class PixelFilter : IImageOperation
    {
        // Абстрактное свойство названия операции
        public abstract string Name { get; }
        // Абстрактный метод преобразования пикселя
        protected abstract Pixel PixelTransform(Pixel p);
        // Метод применения операции
        public Picture Apply(Picture source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source), "Исходная картинка не может быть пустой.");
            Picture result = source.Clone();
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    result.SetPixel(x, y, PixelTransform(source.GetPixel(x, y)));
                }
            }
            return result;
        }

        // Вспомогательный метод ограничения значения
        protected static byte Clamp(double value)
        {
            if (value < 0) return 0;
            if (value > 255) return 255;
            return (byte)Math.Round(value);
        }
    }
}