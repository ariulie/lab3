using PhotoEditor.Core.Models;

namespace PhotoEditor.Core.Codecs
{
    public class CodecRegistry
    {
        // Список зарегистрированных кодеков
        private readonly List<IImageCodec> _codecs;

        // Конструктор по умолчанию
        public CodecRegistry()
        {
            _codecs = new List<IImageCodec>();
            // Регистрация кодеков по умолчанию: BMP (собственный) и PNG/JPEG
            Register(new BmpCodec());
            Register(new ImageSharpCodec());
        }

        // Регистрация нового кодека
        public void Register(IImageCodec codec)
        {
            if (codec is null) throw new ArgumentNullException(nameof(codec), "Кодек не может быть пустым.");
            _codecs.Add(codec);
        }

        // Поиск подходящего кодека по расширению файла
        private IImageCodec GetCodec(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Путь не может быть пустым.", nameof(path));
            string extension = Path.GetExtension(path);
            foreach (var codec in _codecs)
            {
                if (codec.CanHandle(extension)) return codec;
            }
            throw new NotSupportedException($"Формат '{extension}' не поддерживается.");
        }

        // Загрузка картинки с использованием подходящего кодека
        public Picture Load(string path)
        {
            IImageCodec codec = GetCodec(path);
            return codec.Load(path);
        }

        // Сохранение картинки с использованием подходящего кодека
        public void Save(Picture picture, string path)
        {
            IImageCodec codec = GetCodec(path);
            codec.Save(picture, path);
        }
    }
}