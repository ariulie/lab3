using PhotoEditor.Core.Models;
using PhotoEditor.Core.Operations;
using Xunit;

namespace PhotoEditor.Tests.Operations
{
    public class ChannelFilterTests
    {
        // Тест обнуления синего канала с сохранением альфа-канала
        [Fact]
        public void Apply_ZeroBlueMultiplier_RemovesBlueAndKeepsAlpha()
        {
            var original = new Picture("test", 1, 1);
            original.SetPixel(0, 0, new Pixel(100, 150, 200, 128));
            var result = new ChannelFilter(1, 1, 0).Apply(original);
            var pixel = result.GetPixel(0, 0);

            Assert.Equal(100, pixel.R);
            Assert.Equal(150, pixel.G);
            Assert.Equal(0, pixel.B);
            Assert.Equal(128, pixel.A);
        }

        // Тест уменьшения красного канала вдвое
        [Fact]
        public void Apply_HalfRedMultiplier_ReducesRedByHalf()
        {
            var original = new Picture("test", 1, 1);
            original.SetPixel(0, 0, new Pixel(200, 100, 100));
            var result = new ChannelFilter(0.5, 1, 1).Apply(original);

            Assert.Equal(100, result.GetPixel(0, 0).R);
        }

        // Тест ограничения красного канала сверху
        [Fact]
        public void Apply_DoubleRedMultiplier_ClampsToUpperLimit()
        {
            var original = new Picture("test", 1, 1);
            original.SetPixel(0, 0, new Pixel(200, 100, 100));
            var result = new ChannelFilter(2, 1, 1).Apply(original);

            Assert.Equal(255, result.GetPixel(0, 0).R);
        }

        // Тест единичных множителей (без изменений)
        [Fact]
        public void Apply_IdentityMultipliers_DoesNotChangePicture()
        {
            var original = new Picture("test", 2, 2);
            original.SetPixel(0, 0, new Pixel(10, 20, 30));
            original.SetPixel(1, 1, new Pixel(100, 150, 200));
            var result = new ChannelFilter(1, 1, 1).Apply(original);

            Assert.Equal(original.Data, result.Data);
        }

        // Тест выброса исключения при отрицательном множителе
        [Theory]
        [InlineData(-0.1, 1, 1)]
        [InlineData(1, -1, 1)]
        [InlineData(1, 1, -2)]
        public void Constructor_NegativeMultiplier_ThrowsException(double r, double g, double b)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChannelFilter(r, g, b));
        }

        // Тест независимого умножения разных каналов
        [Fact]
        public void Apply_AllDifferentMultipliers_MultipliesCorrectChannels()
        {
            var original = new Picture("test", 1, 1);
            original.SetPixel(0, 0, new Pixel(100, 100, 100));

            var result = new ChannelFilter(0.5, 2, 0).Apply(original);
            var pixel = result.GetPixel(0, 0);

            Assert.Equal(50, pixel.R);  // 100 * 0.5
            Assert.Equal(200, pixel.G); // 100 * 2
            Assert.Equal(0, pixel.B);   // 100 * 0
        }

        // Тест неизменности оригинала и создания новой копии
        [Fact]
        public void Apply_DoesNotChangeOriginalAndReturnsNewObject()
        {
            var original = new Picture("test", 1, 1);
            original.SetPixel(0, 0, new Pixel(10, 20, 30));
            
            var pristine = new Picture("test", 1, 1);
            pristine.SetPixel(0, 0, new Pixel(10, 20, 30));

            var result = new ChannelFilter(2, 2, 2).Apply(original);

            Assert.Equal(pristine.Data, original.Data);
            Assert.NotSame(original, result);
            Assert.NotSame(original.Data, result.Data);
        }

        // Тест сохранения размеров и имени
        [Fact]
        public void Apply_ResultHasSameSizeAndName()
        {
            var original = new Picture("test", 3, 2);
            var result = new ChannelFilter(1, 1, 1).Apply(original);

            Assert.Equal(original.Width, result.Width);
            Assert.Equal(original.Height, result.Height);
            Assert.Equal(original.Name, result.Name);
        }

        // Тест передачи null
        [Fact]
        public void Apply_NullSource_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ChannelFilter(1, 1, 1).Apply(null!));
        }

        // Тест корректности названия операции
        [Fact]
        public void Name_ReturnsExpectedValue()
        {
            Assert.Equal("Множители каналов: R = 1, G = 2, B = 3", new ChannelFilter(1, 2, 3).Name);
        }
    }
}