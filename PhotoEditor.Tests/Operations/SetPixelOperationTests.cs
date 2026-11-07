using PhotoEditor.Core.Models;
using PhotoEditor.Core.Operations;
using Xunit;

namespace PhotoEditor.Tests.Operations
{
    public class SetPixelOperationTests
    {
        // Тест изменения пикселя и сохранности соседей
        [Fact]
        public void Apply_ChangesTargetPixel_AndKeepsNeighbors()
        {
            var original = new Picture("test", 3, 2); 
            var newColor = new Pixel(255, 0, 0); // Красный
            var op = new SetPixelOperation(1, 1, newColor);
            var result = op.Apply(original);

            Assert.Equal("test", result.Name);
            Assert.Equal(newColor, result.GetPixel(1, 1));
            Assert.Equal(new Pixel(255, 255, 255, 255), result.GetPixel(0, 1)); // Слева
            Assert.Equal(new Pixel(255, 255, 255, 255), result.GetPixel(2, 1)); // Справа
            Assert.Equal(new Pixel(255, 255, 255, 255), result.GetPixel(1, 0)); // Сверху
        }

        // Тест неизменности оригинала
        [Fact]
        public void Apply_DoesNotChangeOriginal()
        {
            var original = new Picture("test", 3, 2); 
            var pristine = new Picture("test", 3, 2);  // Эталон для сравнения содержимого
            var op = new SetPixelOperation(0, 0, new Pixel(0, 0, 0));
            var result = op.Apply(original);

            Assert.Equal(pristine.Data, original.Data);
            Assert.NotSame(original, result);
            Assert.NotSame(original.Data, result.Data);
        }

        // Тест выхода за границы
        [Theory]
        [InlineData(3, 0)] // x == Width (для картинки 3x2)
        [InlineData(0, 2)] // y == Height (для картинки 3x2)
        public void Apply_OutOfBounds_ThrowsException(int x, int y)
        {
            var original = new Picture("test", 3, 2); 
            var op = new SetPixelOperation(x, y, new Pixel(1, 2, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => op.Apply(original));
        }

        // Тест отрицательных координат
        [Theory]
        [InlineData(-1, 0)] // Отрицательный X
        [InlineData(0, -1)] // Отрицательный Y
        public void Constructor_NegativeCoordinates_ThrowsException(int x, int y)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SetPixelOperation(x, y, new Pixel(1, 2, 3)));
        }

        // Тест передачи null
        [Fact]
        public void Apply_NullSource_ThrowsException()
        {
            var op = new SetPixelOperation(0, 0, new Pixel(1, 2, 3));
            Assert.Throws<ArgumentNullException>(() => op.Apply(null!));
        }

        // Тест корректности названия операции
        [Fact]
        public void SetPixelOperation_CorrectName()
        {
            var op = new SetPixelOperation(5, 10, new Pixel(0, 0, 0));
            Assert.Equal("Замена пикселя (5, 10)", op.Name);
        }
    }
}