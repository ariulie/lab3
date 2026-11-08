using PhotoEditor.Core.Models;
using PhotoEditor.Core.Operations;
using Xunit;

namespace PhotoEditor.Tests.Operations
{
    public class BrightnessFilterTests
    {
        // Тест увеличения яркости с сохранением альфа-канала
        [Fact]
        public void Apply_PositiveDelta_IncreasesChannelsAndKeepsAlpha()
        {
            var original = new Picture("test", 1, 1);
            original.SetPixel(0, 0, new Pixel(10, 100, 200, 120));
            var result = new BrightnessFilter(50).Apply(original);

            Assert.Equal(new Pixel(60, 150, 250, 120), result.GetPixel(0, 0));
        }

        // Тест ограничения значения сверху
        [Fact]
        public void Apply_HighDelta_ClampsToUpperLimit()
        {
            var original = new Picture("test", 1, 1);
            original.SetPixel(0, 0, new Pixel(200, 200, 200, 255));
            var result = new BrightnessFilter(100).Apply(original);

            Assert.Equal(new Pixel(255, 255, 255, 255), result.GetPixel(0, 0));
        }

        // Тест ограничения значения снизу
        [Fact]
        public void Apply_NegativeDelta_ClampsToLowerLimit()
        {
            var original = new Picture("test", 1, 1);
            original.SetPixel(0, 0, new Pixel(50, 50, 50, 255));
            var result = new BrightnessFilter(-100).Apply(original);

            Assert.Equal(new Pixel(0, 0, 0, 255), result.GetPixel(0, 0));
        }

        // Тест нулевого изменения яркости
        [Fact]
        public void Apply_ZeroDelta_DoesNotChangePicture()
        {
            var original = new Picture("test", 2, 2);
            original.SetPixel(0, 0, new Pixel(10, 20, 30, 40));
            original.SetPixel(1, 1, new Pixel(100, 150, 200, 250));
            var result = new BrightnessFilter(0).Apply(original);

            Assert.Equal(original.Data, result.Data);
        }

        // Тест выброса исключения при недопустимом значении
        [Theory]
        [InlineData(256)]
        [InlineData(-256)]
        public void Constructor_InvalidDelta_ThrowsException(int invalidDelta)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BrightnessFilter(invalidDelta));
        }

        // Тест неизменности оригинала и создания новой копии
        [Fact]
        public void Apply_DoesNotChangeOriginalAndReturnsNewObject()
        {
            var original = new Picture("test", 2, 2);
            original.SetPixel(0, 0, new Pixel(10, 20, 30, 40));
            var pristine = new Picture("test", 2, 2);
            pristine.SetPixel(0, 0, new Pixel(10, 20, 30, 40));
            var result = new BrightnessFilter(50).Apply(original);

            Assert.Equal(pristine.Data, original.Data);
            Assert.NotSame(original, result);
            Assert.NotSame(original.Data, result.Data);
        }
        // Тест границ
        [Theory]
        [InlineData(255)]
        [InlineData(-255)]
        public void Constructor_BoundaryValues_DoesNotThrow(int delta)
        {
            var ex = Record.Exception(() => new BrightnessFilter(delta));
            Assert.Null(ex);
        }

        // Тест передачи null
        [Fact]
        public void Apply_NullSource_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BrightnessFilter(10).Apply(null!));
        }

        // Тест корректности названия операции
        [Fact]
        public void Name_ReturnsExpectedValue()
        {
            Assert.Equal("Яркость +50", new BrightnessFilter(50).Name);
            Assert.Equal("Яркость -50", new BrightnessFilter(-50).Name);
        }
    }
}