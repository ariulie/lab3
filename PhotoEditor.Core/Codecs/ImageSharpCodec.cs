using PhotoEditor.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PhotoEditor.Core.Codecs
{
    public class ImageSharpCodec : IImageCodec
    {
        // Поддерживаемые расширения
        private static readonly string[] PngExtensions = { ".png" };
        private static readonly string[] JpegExtensions = { ".jpg", ".jpeg" };

        // Качество сохранения JPEG
        private const int JpegQuality = 90;

        // Проверка принадлежности расширения к списку
        private static bool Contains(string[] list, string extension)
        {
            foreach (string item in list)
            {
                if (item.Equals(extension, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        // Проверка поддержки расширения (PNG, JPG, JPEG)
        public bool CanHandle(string extension)
        {
            if (extension is null) return false;
            return Contains(PngExtensions, extension) || Contains(JpegExtensions, extension);
        }

        // Загрузка картинки из файла
        public Picture Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Путь не может быть пустым.", nameof(path));
            string name = Path.GetFileNameWithoutExtension(path);
            try
            {
                using Image<Rgba32> image = Image.Load<Rgba32>(path);
                // Учёт поворота, записанного камерой в метаданных (EXIF)
                image.Mutate(ctx => ctx.AutoOrient());

                byte[] data = new byte[image.Width * image.Height * 4];
                image.CopyPixelDataTo(data);
                return new Picture(name, image.Width, image.Height, data);
            }
            catch (ImageFormatException ex)
            {
                throw new InvalidDataException("Файл повреждён или не является изображением.", ex);
            }
        }

        // Сохранение картинки в файл
        public void Save(Picture picture, string path)
        {
            if (picture is null) throw new ArgumentNullException(nameof(picture), "Картинка не может быть пустой.");
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Путь не может быть пустым.", nameof(path));
            string extension = Path.GetExtension(path);

            using Image<Rgba32> image = Image.LoadPixelData<Rgba32>(picture.Data, picture.Width, picture.Height);
            if (Contains(PngExtensions, extension))
            {
                image.SaveAsPng(path);
            }
            else if (Contains(JpegExtensions, extension))
            {
                // JPEG не хранит прозрачность: полупрозрачные пиксели накладываются на белый фон
                image.Mutate(ctx => ctx.BackgroundColor(Color.White));
                image.SaveAsJpeg(path, new JpegEncoder { Quality = JpegQuality });
            }
            else
            {
                throw new NotSupportedException($"Формат '{extension}' не поддерживается.");
            }
        }
    }
}