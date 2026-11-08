using PhotoEditor.Core.Models;
using System;
namespace PhotoEditor.Core.Codecs
{
    public class BmpCodec : IImageCodec
    {
        // Вычисление размера строки с учётом выравнивания
        private static int GetRowSize(int width)
        {
            return (width * 3 + 3) & ~3; // Кратность 4 в большую сторону
        }

        // Проверка поддержки расширения .bmp
        public bool CanHandle(string extension)
        {
            return extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
        }

        // Загрузка картинки из файла
        public Picture Load(string path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path), "Путь не может быть пустой.");
            using var stream = File.OpenRead(path);
            string name = Path.GetFileNameWithoutExtension(path);
            return Read(stream, name);
        }

        // Сохранение картинки в файл
        public void Save(Picture picture, string path)
        {
            if (picture is null) throw new ArgumentNullException(nameof(picture), "Картинка не может быть пустой.");
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path), "Путь не может быть пустой.");
            using var stream = File.Create(path);
            Write(picture, stream);
        }

        // Запись картинки в поток
        public void Write(Picture picture, Stream stream)
        {
            if (picture is null) throw new ArgumentNullException(nameof(picture), "Картинка не может быть пустой.");
            if (stream is null) throw new ArgumentNullException(nameof(stream), "Поток не может быть пустым.");
            using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

            // Вычисление размеров
            int rowSize = GetRowSize(picture.Width);
            int pixelDataSize = rowSize * picture.Height;
            int fileSize = 54 + pixelDataSize;

            // Запись заголовка файла
            writer.Write((byte)'B');
            writer.Write((byte)'M');
            writer.Write(fileSize);
            writer.Write((short)0);
            writer.Write((short)0);
            writer.Write(54);

            // Запись заголовка информации
            writer.Write(40);
            writer.Write(picture.Width);
            writer.Write(picture.Height);
            writer.Write((short)1);
            writer.Write((short)24);
            writer.Write(0);
            writer.Write(pixelDataSize);
            writer.Write(2835);
            writer.Write(2835);
            writer.Write(0);
            writer.Write(0);

            // Запись пикселей снизу вверх (BGR)
            for (int y = picture.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < picture.Width; x++)
                {
                    Pixel pixel = picture.GetPixel(x, y);
                    writer.Write(pixel.B);
                    writer.Write(pixel.G);
                    writer.Write(pixel.R);
                }
                // Выравнивание строки
                for (int i = 0; i < rowSize - picture.Width * 3; i++)
                {
                    writer.Write((byte)0);
                }
            }
        }

        // Чтение картинки из потока
        public Picture Read(Stream stream, string name)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream), "Поток не может быть пустым.");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Имя не может быть пустым.", nameof(name));
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

            // Проверка сигнатуры
            byte b1 = reader.ReadByte();
            byte b2 = reader.ReadByte();
            if (b1 != 'B' || b2 != 'M') throw new InvalidDataException("Не BMP-файл");

            // Читаем остаток BITMAPFILEHEADER (12 байт)
            reader.ReadInt32(); // Размер файла пропускаем
            reader.ReadInt32(); // Резерв пропускаем
            int pixelOffset = reader.ReadInt32(); // Смещение до пикселей

            // Читаем BITMAPINFOHEADER (40 байт)
            reader.ReadInt32(); // Размер этого заголовка 

            int width = reader.ReadInt32();
            if (width <= 0) throw new InvalidDataException("Отрицательное или нулевое значение ширины. Файл повреждён.");
            int height = reader.ReadInt32();
            if (height == 0) throw new InvalidDataException("Нулевое значение высоты. Файл повреждён.");
            bool isTopDown = height < 0;
            height = Math.Abs(height);

            reader.ReadInt16(); // Плоскости (пропускаем)

            short bitsPerPixel = reader.ReadInt16(); // Бит на пиксель
            if (bitsPerPixel != 24) throw new NotSupportedException($"Поддерживаются только 24-битные BMP. Получено: {bitsPerPixel} бит.");
            int compression = reader.ReadInt32(); // Сжатие
            if (compression != 0) throw new NotSupportedException($"Сжатые BMP (метод {compression}) не поддерживаются.");

            reader.ReadInt32(); // Размер пиксельных данных (пропускаем)
            reader.ReadInt32(); // X DPI (пропускаем)
            reader.ReadInt32(); // Y DPI (пропускаем)
            reader.ReadInt32(); // Цветов в палитре (пропускаем)
            reader.ReadInt32(); // Важных цветов (пропускаем)

            reader.BaseStream.Seek(pixelOffset, SeekOrigin.Begin);

            // Создание картинки и чтение пикселей
            Picture result = new Picture(name, width, height);
            for (int i = 0; i < height; i++)
            {
                int y = isTopDown ? i : height - 1 - i;
                for (int x = 0; x < width; x++)
                {
                    byte b = reader.ReadByte();
                    byte g = reader.ReadByte();
                    byte r = reader.ReadByte();
                    result.SetPixel(x, y, new Pixel(r, g, b));
                }
                // Пропуск выравнивания
                reader.ReadBytes(GetRowSize(width) - width * 3);
            }
            return result;
        }
    }
}