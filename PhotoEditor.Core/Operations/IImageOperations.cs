using PhotoEditor.Core.Models;

namespace PhotoEditor.Core.Operations
{
    public interface IImageOperation
    {
        // Свойство названия операции
        string Name { get; }
        // Метод применения операции к картинке
        Picture Apply(Picture source);
    }
}