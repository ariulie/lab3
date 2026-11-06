namespace PhotoEditor.Core.Models
{
    public readonly struct Pixel
    {
        // Компонент красного цвета
        public byte R { get; }
        // Компонент зеленого цвета
        public byte G { get; }
        // Компонент синего цвета
        public byte B { get; }
        // Компонент прозрачности (альфа-канал)
        public byte A { get; }

        // Конструктор с параметрами
        public Pixel(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    } 
}