using PhotoEditor.Core.Models;
using PhotoEditor.Core.Operations;
using Xunit;

namespace PhotoEditor.Tests.Operations
{
    public class CropOperationTests
    {
        // Создание тестовой картинки
        private Picture CreateSample()
        {
            var pic = new Picture("test", 4, 3);
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    pic.SetPixel(x, y, new Pixel((byte)x, (byte)y, 0));
                }
            }
            return pic;
        }

        // Тест обрезки 2x2 и сохранения имени
        [Fact]
        public void Apply_Crop2x2FromPoint_ReturnsCorrectPixelsAndName()
        {
            var original = CreateSample();
            var cropOp = new CropOperation(1, 1, 2, 2);
            var cropped = cropOp.Apply(original);

            Assert.Equal(2, cropped.Width);
            Assert.Equal(2, cropped.Height);
            Assert.Equal("test", cropped.Name);
            Assert.Equal(new Pixel(1, 1, 0), cropped.GetPixel(0, 0));
            Assert.Equal(new Pixel(2, 1, 0), cropped.GetPixel(1, 0));
            Assert.Equal(new Pixel(1, 2, 0), cropped.GetPixel(0, 1));
            Assert.Equal(new Pixel(2, 2, 0), cropped.GetPixel(1, 1));
        }

        // Тест обрезки неквадратной области 3x1
        [Fact]
        public void Apply_CropNonSquare3x1_ReturnsCorrectPixels()
        {
            var original = CreateSample();
            var cropOp = new CropOperation(1, 0, 3, 1);
            var cropped = cropOp.Apply(original);

            Assert.Equal(3, cropped.Width);
            Assert.Equal(1, cropped.Height);
            Assert.Equal(new Pixel(1, 0, 0), cropped.GetPixel(0, 0));
            Assert.Equal(new Pixel(2, 0, 0), cropped.GetPixel(1, 0));
            Assert.Equal(new Pixel(3, 0, 0), cropped.GetPixel(2, 0));
        }

        // Тест обрезки всего изображения
        [Fact]
        public void Apply_CropWholeImage_ReturnsEqualCopy()
        {
            var original = CreateSample();
            var cropOp = new CropOperation(0, 0, 4, 3);
            var cropped = cropOp.Apply(original);

            Assert.Equal(original.Width, cropped.Width);
            Assert.Equal(original.Height, cropped.Height);
            Assert.Equal(original.Data, cropped.Data);
            Assert.NotSame(original, cropped);
            Assert.NotSame(original.Data, cropped.Data);
        }

        // Тест обрезки до одного пикселя
        [Fact]
        public void Apply_Crop1x1_ReturnsSinglePixel()
        {
            var original = CreateSample();
            var cropOp = new CropOperation(2, 1, 1, 1);
            var cropped = cropOp.Apply(original);

            Assert.Equal(1, cropped.Width);
            Assert.Equal(1, cropped.Height);
            Assert.Equal(new Pixel(2, 1, 0), cropped.GetPixel(0, 0));
        }

        // Тест обрезки у правого нижнего края
        [Fact]
        public void Apply_CropAtRightBottomEdge_CopiesCorrectly()
        {
            var original = CreateSample(); // 4x3
            var cropOp = new CropOperation(2, 1, 2, 2); // x+w = 4, y+h = 3 (граница)
            var cropped = cropOp.Apply(original);

            Assert.Equal(2, cropped.Width);
            Assert.Equal(2, cropped.Height);
            Assert.Equal(new Pixel(2, 1, 0), cropped.GetPixel(0, 0));
            Assert.Equal(new Pixel(3, 1, 0), cropped.GetPixel(1, 0));
            Assert.Equal(new Pixel(2, 2, 0), cropped.GetPixel(0, 1));
            Assert.Equal(new Pixel(3, 2, 0), cropped.GetPixel(1, 1));
        }

        // Тест выхода за границы изображения
        [Theory]
        [InlineData(3, 0, 2, 1)]      // x + width = 5 > 4
        [InlineData(0, 2, 1, 2)]      // y + height = 4 > 3
        [InlineData(4, 0, 1, 1)]      // x целиком за границей (x = Width)
        [InlineData(0, 0, 100, 100)]  // сильно большой размер
        public void Apply_OutOfBounds_ThrowsArgumentException(int x, int y, int w, int h)
        {
            var original = CreateSample();
            var cropOp = new CropOperation(x, y, w, h);
            Assert.Throws<ArgumentException>(() => cropOp.Apply(original));
        }

        // Тест неизменности оригинала
        [Fact]
        public void Apply_DoesNotChangeOriginal()
        {
            var original = CreateSample();
            var pristine = CreateSample(); 
            var cropOp = new CropOperation(1, 1, 2, 2);
            cropOp.Apply(original);

            Assert.Equal(pristine.Data, original.Data);
        }

        // Тест неверных аргументов конструктора
        [Theory]
        [InlineData(-1, 0, 1, 1)] // Отрицательный x
        [InlineData(0, -1, 1, 1)] // Отрицательный y
        [InlineData(0, 0, 0, 1)]  // Нулевая ширина
        [InlineData(0, 0, 1, 0)]  // Нулевая высота
        [InlineData(0, 0, -1, 1)] // Отрицательная ширина
        [InlineData(0, 0, 1, -1)] // Отрицательная высота
        public void Constructor_InvalidArguments_ThrowsException(int x, int y, int w, int h)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CropOperation(x, y, w, h));
        }

        // Тест передачи null
        [Fact]
        public void Apply_NullSource_ThrowsException()
        {
            var cropOp = new CropOperation(0, 0, 2, 2);
            Assert.Throws<ArgumentNullException>(() => cropOp.Apply(null!));
        }

        // Тест корректности названия операции
        [Fact]
        public void CropOperation_CorrectName()
        {
            var op = new CropOperation(0, 0, 10, 20);
            Assert.Equal("Обрезка 10×20", op.Name);
        }
    }
}