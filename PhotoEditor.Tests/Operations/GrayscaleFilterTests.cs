using PhotoEditor.Core.Models;
using PhotoEditor.Core.Operations;
using Xunit;

namespace PhotoEditor.Tests.Operations
{
    public class GrayscaleFilterTests
    {
        // Тест преобразования в оттенки серого с проверкой альфа-канала
        [Theory]
        [InlineData(255, 0, 0, 76)]
        [InlineData(0, 255, 0, 150)]
        [InlineData(0, 0, 255, 29)]
        [InlineData(100, 100, 100, 100)]
        public void PixelTransform_ReturnsExpectedGrayscale(byte r, byte g, byte b, byte expectedGray)
        {
            var original = new Picture("test", 1, 1);
            original.SetPixel(0, 0, new Pixel(r, g, b, 123));
            var result = new GrayscaleFilter().Apply(original);
            var pixel = result.GetPixel(0, 0);

            Assert.Equal(expectedGray, pixel.R);
            Assert.Equal(expectedGray, pixel.G);
            Assert.Equal(expectedGray, pixel.B);
            Assert.Equal(123, pixel.A);
        }

        // Тест равенства каналов RGB
        [Fact]
        public void Apply_ResultHasEqualRgbChannels()
        {
            var original = new Picture("test", 1, 1);
            original.SetPixel(0, 0, new Pixel(12, 34, 56));
            var result = new GrayscaleFilter().Apply(original);
            var pixel = result.GetPixel(0, 0);

            Assert.Equal(pixel.R, pixel.G);
            Assert.Equal(pixel.G, pixel.B);
        }

        // Тест неизменности оригинала и создания новой копии
        [Fact]
        public void Apply_DoesNotChangeOriginalAndReturnsNewObject()
        {
            var original = new Picture("test", 1, 1);
            original.SetPixel(0, 0, new Pixel(10, 20, 30, 40));
            var pristine = new Picture("test", 1, 1);
            pristine.SetPixel(0, 0, new Pixel(10, 20, 30, 40));
            var result = new GrayscaleFilter().Apply(original);

            Assert.Equal(pristine.Data, original.Data);
            Assert.NotSame(original, result);
            Assert.NotSame(original.Data, result.Data);
        }

        // Тест сохранения размеров и имени
        [Fact]
        public void Apply_ResultHasSameSizeAndName()
        {
            var original = new Picture("test", 3, 2);
            var result = new GrayscaleFilter().Apply(original);

            Assert.Equal(original.Width, result.Width);
            Assert.Equal(original.Height, result.Height);
            Assert.Equal(original.Name, result.Name);
        }

        // Тест передачи null
        [Fact]
        public void Apply_NullSource_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new GrayscaleFilter().Apply(null!));
        }

        // Тест корректности названия операции
        [Fact]
        public void Name_ReturnsExpectedValue()
        {
            Assert.Equal("Оттенки серого", new GrayscaleFilter().Name);
        }
    }
}