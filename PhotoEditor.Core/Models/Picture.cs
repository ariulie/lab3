namespace PhotoEditor.Core.Models
{
    public class Picture
    {
        // Поле для хранения названия
        string _name = "";

        // Свойство названия с проверкой
        public string Name
        {
            get { return _name; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Название не должно быть пустым.", nameof(value));
                else _name = value;
            }
        }

        // Свойство ширины
        public int Width { get; }
        // Свойство высоты
        public int Height { get; }
        // Свойство данных пикселей
        public byte[] Data { get; }

        // Конструктор с параметрами
        public Picture(string name, int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Ширина не может быть меньше или равна нулю.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Высота не может быть меньше или равна нулю.");
            Width = width;
            Height = height;
            Name = name;

            Data = new byte[width * height * 4];
            Array.Fill(Data, (byte)255);
        }

        // Метод проверки границ
        public bool InBounds(int x, int y)
        {
            return (0 <= x && x < Width && 0 <= y && y < Height);
        }

        // Метод получения пикселя
        public Pixel GetPixel(int x, int y)
        {
            if (!InBounds(x, y)) throw new ArgumentOutOfRangeException(nameof(x), "Координаты вне картинки.");
            int index = (y * Width + x) * 4;
            return new Pixel(Data[index], Data[index + 1], Data[index + 2], Data[index + 3]);
        }

        // Метод установки пикселя
        public void SetPixel(int x, int y, Pixel pix)
        {
            if (!InBounds(x, y)) throw new ArgumentOutOfRangeException(nameof(x), "Координаты вне картинки.");
            int index = (y * Width + x) * 4;
            Data[index] = pix.R;
            Data[index + 1] = pix.G;
            Data[index + 2] = pix.B;
            Data[index + 3] = pix.A;
        }

        // Метод клонирования
        public Picture Clone()
        {
            Picture ClonePic = new Picture(this.Name, this.Width, this.Height);
            Array.Copy(this.Data, ClonePic.Data, this.Data.Length);
            return ClonePic;
        }
    }
}