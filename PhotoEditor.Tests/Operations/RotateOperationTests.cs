using PhotoEditor.Core.Operations;
using PhotoEditor.Core.Models;
using Xunit;

namespace PhotoEditor.Tests.Operations
{
    public class RotateOperationTests
    {
        private Picture CreateSample()
        {
            var pic = new Picture("test", 2, 3);
            pic.SetPixel(0, 0, new Pixel(255, 0, 0)); // Красный
            pic.SetPixel(1, 0, new Pixel(0, 255, 0)); // Зеленый
            pic.SetPixel(0, 1, new Pixel(0, 0, 255)); // Синий
            pic.SetPixel(1, 1, new Pixel(255, 255, 0)); // Желтый
            pic.SetPixel(0, 2, new Pixel(255, 0, 255)); // Розовый
            pic.SetPixel(1, 2, new Pixel(0, 255, 255)); // Голубой
            return pic;
        }

        [Fact]
        public void RotateOperation_90Degrees_RotatesCorrectly()
        {
            var original = CreateSample();
            var pristine = CreateSample();
            var rotated = new RotateOperation(90).Apply(original);

            Assert.Equal(3, rotated.Width);
            Assert.Equal(2, rotated.Height);
            Assert.Equal("test", rotated.Name);

            Assert.Equal(new Pixel(255, 0, 0), rotated.GetPixel(2, 0));
            Assert.Equal(new Pixel(0, 255, 0), rotated.GetPixel(2, 1));
            Assert.Equal(new Pixel(0, 0, 255), rotated.GetPixel(1, 0));
            Assert.Equal(new Pixel(255, 255, 0), rotated.GetPixel(1, 1));
            Assert.Equal(new Pixel(255, 0, 255), rotated.GetPixel(0, 0));
            Assert.Equal(new Pixel(0, 255, 255), rotated.GetPixel(0, 1));
            Assert.Equal(pristine.Data, original.Data);
        }

        [Fact]
        public void RotateOperation_180Degrees_RotatesCorrectly()
        {
            var original = CreateSample();
            var pristine = CreateSample();
            var rotated = new RotateOperation(180).Apply(original);

            Assert.Equal(2, rotated.Width);
            Assert.Equal(3, rotated.Height);
            Assert.Equal("test", rotated.Name);

            Assert.Equal(new Pixel(255, 0, 0), rotated.GetPixel(1, 2));
            Assert.Equal(new Pixel(0, 255, 0), rotated.GetPixel(0, 2));
            Assert.Equal(new Pixel(0, 0, 255), rotated.GetPixel(1, 1));
            Assert.Equal(new Pixel(255, 255, 0), rotated.GetPixel(0, 1));
            Assert.Equal(new Pixel(255, 0, 255), rotated.GetPixel(1, 0));
            Assert.Equal(new Pixel(0, 255, 255), rotated.GetPixel(0, 0));
            Assert.Equal(pristine.Data, original.Data);
        }

        [Fact]
        public void RotateOperation_270Degrees_RotatesCorrectly()
        {
            var original = CreateSample();
            var rotated = new RotateOperation(270).Apply(original);

            Assert.Equal(3, rotated.Width);
            Assert.Equal(2, rotated.Height);

            Assert.Equal(new Pixel(255, 0, 0), rotated.GetPixel(0, 1));
            Assert.Equal(new Pixel(0, 255, 0), rotated.GetPixel(0, 0));
            Assert.Equal(new Pixel(0, 0, 255), rotated.GetPixel(1, 1));
            Assert.Equal(new Pixel(255, 255, 0), rotated.GetPixel(1, 0));
            Assert.Equal(new Pixel(255, 0, 255), rotated.GetPixel(2, 1));
            Assert.Equal(new Pixel(0, 255, 255), rotated.GetPixel(2, 0));
        }
        [Fact]
        public void RotateOperation_Minus90_Equals270()
        {
            var original = CreateSample();
            var rotated270 = new RotateOperation(270).Apply(original);
            var rotatedMinus90 = new RotateOperation(-90).Apply(original);

            Assert.Equal(rotated270.Data, rotatedMinus90.Data);
            Assert.Equal(rotated270.Name, rotatedMinus90.Name);
        }
        [Fact]
        public void RotateOperation_360Degrees_EqualsOriginal()
        {
            var original = CreateSample();
            var rotated360 = new RotateOperation(360).Apply(original);

            Assert.Equal(original.Data, rotated360.Data);
            Assert.Equal(original.Name, rotated360.Name);
            Assert.NotSame(original, rotated360);
            Assert.NotSame(original.Data, rotated360.Data);
        }
        [Fact]
        public void RotateOperation_0Degrees_EqualsOriginalButIsNewObject()
        {
            var original = CreateSample();
            var rotated0 = new RotateOperation(0).Apply(original);

            Assert.Equal(original.Data, rotated0.Data);
            Assert.Equal(original.Name, rotated0.Name);
            Assert.NotSame(original, rotated0);
            Assert.NotSame(original.Data, rotated0.Data);
        }
        [Fact]
        public void RotateOperation_Four90DegreeRotations_EqualsOriginal()
        {
            var original = CreateSample();
            var rotated = original;

            for (int i = 0; i < 4; i++)
            {
                rotated = new RotateOperation(90).Apply(rotated);
            }

            Assert.Equal(original.Data, rotated.Data);
            Assert.NotSame(original, rotated);
            Assert.NotSame(original.Data, rotated.Data);
        }
        [Theory]
        [InlineData(45)]
        [InlineData(-30)]
        [InlineData(100)]
        public void RotateOperation_InvalidAngle_ThrowsException(int invalidAngle)
        {
            Assert.Throws<ArgumentException>(() => new RotateOperation(invalidAngle));
        }
        [Fact]
        public void RotateOperation_NullPicture_ThrowsException()
        {
            var rotateOperation = new RotateOperation(90);
            Assert.Throws<ArgumentNullException>(() => rotateOperation.Apply(null!));
        }
        [Fact]
        public void RotateOperation_CorrectNames()
        {
            Assert.Equal("Поворот на 90°", new RotateOperation(90).Name);
            Assert.Equal("Поворот на 270°", new RotateOperation(-90).Name);
        }
        [Theory]
        [InlineData(450)]  // 450 = 360 + 90
        [InlineData(-270)] // -270 = 90 - 360
        public void RotateOperation_LargeAngles_BehaveLike90(int angle)
        {
            var original = CreateSample();
            var expected = new RotateOperation(90).Apply(original);
            var actual = new RotateOperation(angle).Apply(original);

            Assert.Equal(expected.Data, actual.Data);
            Assert.Equal(expected.Name, actual.Name);
        }
    }
}