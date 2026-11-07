using PhotoEditor.Core.Models;
using Xunit;

namespace PhotoEditor.Tests.Models
{
    public class PictureTests
    {
        // Тест корректной инициализации свойств
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            var pic = new Picture("test", 3, 2);
            Assert.Equal("test", pic.Name);
            Assert.Equal(3, pic.Width);
            Assert.Equal(2, pic.Height);
            Assert.Equal(24, pic.Data.Length); 
        }

        // Тест выброса исключения при пустом имени
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidName_ThrowsArgumentException(string invalidName)
        {
            Assert.Throws<ArgumentException>(() => new Picture(invalidName, 3, 2));
        }

        // Тест выброса исключения при неверных размерах
        [Theory]
        [InlineData(0, 5)]  
        [InlineData(-2, 5)]
        [InlineData(5, 0)] 
        [InlineData(5, -3)]     
        public void Constructor_InvalidDimensions_ThrowsArgumentOutOfRangeException(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Picture("test", width, height));
        }

        // Тест цвета по умолчанию (белый и непрозрачный)
        [Fact]
        public void Constructor_NewPicture_IsWhiteAndOpaque()
        {
            var pic = new Picture("test", 3, 2);
            Assert.All(pic.Data, b => Assert.Equal(255, b));
        }

        // Тест получения ранее установленного пикселя
        [Fact]
        public void GetPixel_ReturnsWhatWasSet()
        {
            var pic = new Picture("test", 3, 2);
            var expectedPixel = new Pixel(168, 200, 65);
            pic.SetPixel(0, 0, expectedPixel);
            var actualPixel = pic.GetPixel(0, 0);
            Assert.Equal(expectedPixel, actualPixel);
        }

        // Тест независимости соседних пикселей
        [Fact]
        public void SetPixel_DoesNotChangeNeighbor()
        {
            var pic = new Picture("test", 3, 2);
            var pixelBefore = pic.GetPixel(0, 0); 
            pic.SetPixel(0, 1, new Pixel(168, 200, 65)); 
            var pixelAfter = pic.GetPixel(0, 0); 
            Assert.Equal(pixelBefore, pixelAfter);
        }

        // Тест выхода за границы при чтении пикселя
        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(3, 0)] 
        [InlineData(0, 2)] 
        public void GetPixel_OutOfBounds_ThrowsException(int x, int y)
        {
            var pic = new Picture("test", 3, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => pic.GetPixel(x, y));
        }

        // Тест выхода за границы при записи пикселя
        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(3, 0)]
        [InlineData(0, 2)]
        public void SetPixel_OutOfBounds_ThrowsException(int x, int y)
        {
            var pic = new Picture("test", 3, 2);
            var pixel = new Pixel(168, 200, 65);
            Assert.Throws<ArgumentOutOfRangeException>(() => pic.SetPixel(x, y, pixel));
        }

        // Тест проверки границ по углам
        [Theory]
        [InlineData(0, 0, true)]      
        [InlineData(2, 1, true)]      
        [InlineData(3, 2, false)]   
        [InlineData(-1, -1, false)]   
        public void InBounds_ChecksCornersCorrectly(int x, int y, bool expectedResult)
        {
            var pic = new Picture("test", 3, 2);
            bool actualResult = pic.InBounds(x, y);
            Assert.Equal(expectedResult, actualResult);
        }

        // Тест создания идентичной копии
        [Fact]
        public void Clone_CreatesIdenticalCopy()
        {
            var original = new Picture("test", 3, 2);
            original.SetPixel(0, 0, new Pixel(1, 2, 3)); 
            var clone = original.Clone();
            Assert.Equal(original.Name, clone.Name);
            Assert.Equal(original.Width, clone.Width);
            Assert.Equal(original.Height, clone.Height);
            Assert.Equal(original.Data, clone.Data); 
        }

        // Тест независимости клона от оригинала
        [Fact]
        public void Clone_DoesNotChangeOriginal()
        {
            var original = new Picture("test", 3, 2);
            var clone = original.Clone();
            clone.SetPixel(0, 0, new Pixel(120, 150, 180));
            var originalPixel = original.GetPixel(0, 0);
            Assert.Equal(255, originalPixel.R); 
            Assert.NotEqual(clone.GetPixel(0, 0), originalPixel);
        }
    }
}

