using PhotoEditor.Core.Models;

namespace PhotoEditor.Core.Codecs
{
    public interface IImageCodec
    {
        // Проверка поддержки расширения
        bool CanHandle(string extension);
        // Загрузка картинки из файла
        Picture Load(string path);
        // Сохранение картинки в файл
        void Save(Picture picture, string path);
    }
}