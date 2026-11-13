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
        // Тест на копирование данных не по ссылке
        [Fact]
        public void Constructor_WithData_CopiesArray()
        {
            byte[] originalData = new byte[24]; 
            originalData[0] = 10; // Задаем начальное значение
            var pic = new Picture("test", 3, 2, originalData);
            // Меняем исходный массив
            originalData[0] = 99;
            // В картинке значение должно остаться прежним 
            Assert.Equal(10, pic.Data[0]);
        }

        // Тест на невалидную длину массива
        [Fact]
        public void Constructor_InvalidDataLength_ThrowsArgumentException()
        {
            byte[] wrongData = new byte[10];
            Assert.Throws<ArgumentException>(() => new Picture("test", 3, 2, wrongData));
        }

        // Тест на нулевой массив
        [Fact]
        public void Constructor_NullData_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Picture("test", 3, 2, null!));
        }

        // Тест на невалидные данные для второго конструктора
        [Theory]
        [InlineData("", 3, 2)]
        [InlineData("test", 0, 2)]
        [InlineData("test", -1, 2)]
        [InlineData("test", 3, 0)]
        [InlineData("test", 3, -1)]
        public void Constructor_WithData_InvalidArguments_ThrowsException(string name, int width, int height)
        {
            int validLength = Math.Max(0, width * height * 4); 
            byte[] data = new byte[validLength];

            Assert.ThrowsAny<ArgumentException>(() => new Picture(name, width, height, data));
        }

        // Тест на правильное чтение пикселей
        [Fact]
        public void Constructor_WithData_ReadsPixelsCorrectly()
        {
            var pic = new Picture("test", 3, 2); 
            pic.SetPixel(1, 0, new Pixel(10, 20, 30, 40));
            byte[] data = pic.Data;
            var newPic = new Picture("test2", 3, 2, data);
            var expectedPixel = new Pixel(10, 20, 30, 40);
            Assert.Equal(expectedPixel, newPic.GetPixel(1, 0));
        }
    }
}

