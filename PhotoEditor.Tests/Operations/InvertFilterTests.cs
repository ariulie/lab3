using PhotoEditor.Core.Models;
using PhotoEditor.Core.Operations;
using Xunit;

namespace PhotoEditor.Tests.Operations
{
    public class InvertFilterTests
    {
        // Тест инверсии пикселя с сохранением альфа-канала
        [Fact]
        public void Apply_SpecificPixel_InvertsAndKeepsAlpha()
        {
            var original = new Picture("test", 1, 1);
            original.SetPixel(0, 0, new Pixel(10, 20, 30, 200));
            var result = new InvertFilter().Apply(original);
            var inverted = result.GetPixel(0, 0);

            Assert.Equal(245, inverted.R);
            Assert.Equal(235, inverted.G);
            Assert.Equal(225, inverted.B);
            Assert.Equal(200, inverted.A);
        }

        // Тест инверсии крайних значений
        [Fact]
        public void Apply_ExtremeValues_InvertsCorrectly()
        {
            var original = new Picture("test", 2, 1);
            original.SetPixel(0, 0, new Pixel(0, 0, 0, 255));
            original.SetPixel(1, 0, new Pixel(255, 255, 255, 255));
            var result = new InvertFilter().Apply(original);

            Assert.Equal(new Pixel(255, 255, 255, 255), result.GetPixel(0, 0));
            Assert.Equal(new Pixel(0, 0, 0, 255), result.GetPixel(1, 0));
        }

        // Тест инверсии всех пикселей картинки 3x2
        [Fact]
        public void Apply_3x2Picture_InvertsAllPixels()
        {
            var original = new Picture("test", 3, 2);
            original.SetPixel(0, 0, new Pixel(0, 0, 0, 255));
            original.SetPixel(1, 0, new Pixel(10, 20, 30, 255));
            original.SetPixel(2, 0, new Pixel(40, 50, 60, 255));
            original.SetPixel(0, 1, new Pixel(70, 80, 90, 255));
            original.SetPixel(1, 1, new Pixel(100, 110, 120, 255));
            original.SetPixel(2, 1, new Pixel(130, 140, 150, 255));
            var result = new InvertFilter().Apply(original);

            Assert.Equal(new Pixel(255, 255, 255, 255), result.GetPixel(0, 0));
            Assert.Equal(new Pixel(245, 235, 225, 255), result.GetPixel(1, 0));
            Assert.Equal(new Pixel(215, 205, 195, 255), result.GetPixel(2, 0));
            Assert.Equal(new Pixel(185, 175, 165, 255), result.GetPixel(0, 1));
            Assert.Equal(new Pixel(155, 145, 135, 255), result.GetPixel(1, 1));
            Assert.Equal(new Pixel(125, 115, 105, 255), result.GetPixel(2, 1));
        }

        // Тест двойной инверсии
        [Fact]
        public void Apply_DoubleInversion_ReturnsOriginalData()
        {
            var original = new Picture("test", 3, 2);
            original.SetPixel(0, 0, new Pixel(10, 20, 30, 255));
            original.SetPixel(1, 0, new Pixel(40, 50, 60, 200));
            original.SetPixel(2, 0, new Pixel(70, 80, 90, 255));
            original.SetPixel(0, 1, new Pixel(100, 110, 120, 255));
            original.SetPixel(1, 1, new Pixel(130, 140, 150, 255));
            original.SetPixel(2, 1, new Pixel(160, 170, 180, 255));
            var result = new InvertFilter().Apply(new InvertFilter().Apply(original));

            Assert.Equal(original.Data, result.Data);
        }

        // Тест сохранения размеров и имени
        [Fact]
        public void Apply_ResultHasSameSizeAndName()
        {
            var original = new Picture("test", 3, 2);
            var result = new InvertFilter().Apply(original);

            Assert.Equal(original.Width, result.Width);
            Assert.Equal(original.Height, result.Height);
            Assert.Equal(original.Name, result.Name);
        }

        // Тест неизменности оригинала и создания новой копии
        [Fact]
        public void Apply_DoesNotChangeOriginalAndReturnsNewObject()
        {
            var original = new Picture("test", 3, 2);
            var pristine = new Picture("test", 3, 2);
            original.SetPixel(0, 0, new Pixel(10, 20, 30, 40));
            pristine.SetPixel(0, 0, new Pixel(10, 20, 30, 40));
            var result = new InvertFilter().Apply(original);

            Assert.Equal(pristine.Data, original.Data);
            Assert.NotSame(original, result);
            Assert.NotSame(original.Data, result.Data);
        }

        // Тест передачи null
        [Fact]
        public void Apply_NullSource_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new InvertFilter().Apply(null!));
        }

        // Тест корректности названия операции
        [Fact]
        public void Name_ReturnsExpectedValue()
        {
            Assert.Equal("Негатив", new InvertFilter().Name);
        }
    }
}