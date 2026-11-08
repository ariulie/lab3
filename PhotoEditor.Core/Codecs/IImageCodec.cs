using PhotoEditor.Core.Models;

namespace PhotoEditor.Core.Codecs
{
    public interface IImageCodec
    {
        bool CanHandle(string extension);
        Picture Load(string path);
        void Save(Picture picture, string path);
    }
}