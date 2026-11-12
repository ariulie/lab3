using System;
using PhotoEditor.Core.Models;
using PhotoEditor.Core.Operations;
using Xunit;

namespace PhotoEditor.Tests.Operations
{
    public class TextOperationTests
    {
        // Тест отрисовки одной буквы I
        [Fact]
        public void Apply_SingleLetterI_DrawsCorrectPixels()
        {
            var original = new Picture("test", 10, 10);
            var black = new Pixel(0, 0, 0, 255);
            var op = new TextOperation("I", 0, 0, black, 1);
            var result = op.Apply(original);

            // Верхняя строка глифа I: .###.
            Assert.Equal(new Pixel(255, 255, 255, 255), result.GetPixel(0, 0)); // .
            Assert.Equal(black, result.GetPixel(1, 0)); // #
            Assert.Equal(black, result.GetPixel(2, 0)); // #
            Assert.Equal(black, result.GetPixel(3, 0)); // #
            Assert.Equal(new Pixel(255, 255, 255, 255), result.GetPixel(4, 0)); // .
            
            // Вторая строка: ..#..
            Assert.Equal(black, result.GetPixel(2, 1)); 
            
            // Последняя строка (y=6): .###.
            Assert.Equal(black, result.GetPixel(2, 6));
        }

        // Тест масштабирования текста
        [Fact]
        public void Apply_Scale2_DrawsBlocksOfPixels()
        {
            var original = new Picture("test", 15, 15);
            var black = new Pixel(0, 0, 0, 255);
            var op = new TextOperation("I", 0, 0, black, 2);
            var result = op.Apply(original);

            // Пиксель глифа (col=1, row=0) при scale=2 даёт блок 2x2 начиная с X=2, Y=0
            Assert.Equal(black, result.GetPixel(2, 0));
            Assert.Equal(black, result.GetPixel(3, 0));
            Assert.Equal(black, result.GetPixel(2, 1));
            Assert.Equal(black, result.GetPixel(3, 1));
            
            // Пиксель (col=0, row=0) это точка, там должно быть пусто
            Assert.Equal(new Pixel(255, 255, 255, 255), result.GetPixel(0, 0));
        }

        // Тест смещения второго символа
        [Fact]
        public void Apply_TwoLetters_SecondLetterIsOffset()
        {
            var original = new Picture("test", 15, 15);
            var black = new Pixel(0, 0, 0, 255);
            var op = new TextOperation("II", 0, 0, black, 1);
            var result = op.Apply(original);

            // Вторая I начинается с X=6, её первая решётка на col=1, то есть в (7,0)
            Assert.Equal(new Pixel(255, 255, 255, 255), result.GetPixel(6, 0));
            Assert.Equal(black, result.GetPixel(7, 0));
        }

        // Тест сдвига текста по координатам
        [Fact]
        public void Apply_OffsetPosition_ShiftsText()
        {
            var original = new Picture("test", 10, 10);
            var black = new Pixel(0, 0, 0, 255);
            var op = new TextOperation("I", 2, 3, black, 1);
            var result = op.Apply(original);

            // Верхний левый угол текста теперь в (2,3). Там должна быть точка (col=0)
            Assert.Equal(new Pixel(255, 255, 255, 255), result.GetPixel(2, 3));
            // Первая решетка на col=1, значит в (3,3)
            Assert.Equal(black, result.GetPixel(3, 3));
            
            // В самом левом верхнем углу картинки (0,0) должно остаться пусто
            Assert.Equal(new Pixel(255, 255, 255, 255), result.GetPixel(0, 0));
        }

        // Тест обрезки текста за границами
        [Fact]
        public void Apply_TextOutOfBounds_TruncatesWithoutException()
        {
            var original = new Picture("test", 5, 5); // Маленькая картинка
            var black = new Pixel(0, 0, 0, 255);
            var op = new TextOperation("IIIII", 0, 0, black, 1); 
            var result = op.Apply(original);
            
            // Верхняя строка (y=0) первой буквы I: .###.
            Assert.Equal(new Pixel(255, 255, 255, 255), result.GetPixel(0, 0)); // .
            Assert.Equal(black, result.GetPixel(1, 0)); // #
            Assert.Equal(black, result.GetPixel(2, 0)); // #
            Assert.Equal(black, result.GetPixel(3, 0)); // #
            Assert.Equal(new Pixel(255, 255, 255, 255), result.GetPixel(4, 0)); // .
            
            // Строка 4 (y=4) первой буквы: ..#..
            Assert.Equal(black, result.GetPixel(2, 4)); 
        }

        // Тест установки точного цвета с альфа-каналом
        [Fact]
        public void Apply_SetsExactColorWithAlpha()
        {
            var original = new Picture("test", 10, 10);
            var customColor = new Pixel(10, 20, 30, 100); // Непрозрачность 100
            var op = new TextOperation("I", 0, 0, customColor, 1);
            var result = op.Apply(original);

            Assert.Equal(customColor, result.GetPixel(1, 0));
            Assert.Equal(100, result.GetPixel(1, 0).A);
        }

        // Тест заглушки для неизвестного символа
                [Fact]
        public void Apply_UnknownSymbol_DrawsQuestionMark()
        {
            var original = new Picture("test", 10, 10);
            var black = new Pixel(0, 0, 0, 255);
            var opUnknown = new TextOperation("@", 0, 0, black, 1); 
            var opQuestion = new TextOperation("?", 0, 0, black, 1);
            var resultUnknown = opUnknown.Apply(original);
            var resultQuestion = opQuestion.Apply(original);

            // Результат для неизвестного символа должен быть идентичен знаку вопроса
            Assert.Equal(resultQuestion.Data, resultUnknown.Data);
        }

        // Тест регистронезависимости
        [Fact]
        public void Apply_LowercaseLetter_EqualsUppercase()
        {
            var original = new Picture("test", 10, 10);
            var black = new Pixel(0, 0, 0, 255);
            var opLower = new TextOperation("i", 0, 0, black, 1);
            var opUpper = new TextOperation("I", 0, 0, black, 1);
            var resultLower = opLower.Apply(original);
            var resultUpper = opUpper.Apply(original);

            Assert.Equal(resultUpper.Data, resultLower.Data);
        }

        // Тест неизменности оригинала и создания новой копии
        [Fact]
        public void Apply_DoesNotChangeOriginalAndReturnsNewObject()
        {
            var original = new Picture("test", 10, 10);
            var pristine = new Picture("test", 10, 10);
            var op = new TextOperation("I", 0, 0, new Pixel(0,0,0,255), 1);
            var result = op.Apply(original);

            Assert.Equal(pristine.Data, original.Data);
            Assert.NotSame(original, result);
            Assert.NotSame(original.Data, result.Data);
            Assert.Equal(original.Width, result.Width);
            Assert.Equal(original.Height, result.Height);
            Assert.Equal(original.Name, result.Name);
        }
        // Тест на имя операции
          [Fact]
        public void Name_ReturnsExpectedValue()
        {
            var op = new TextOperation("Hi", 0, 0, new Pixel(0, 0, 0), 1);
            Assert.Equal("Надпись 'Hi'", op.Name);
        }
        // Тест невалидного текста
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Constructor_InvalidText_ThrowsArgumentException(string? invalidText)
        {
            Assert.Throws<ArgumentException>(() => new TextOperation(invalidText!, 0, 0, new Pixel(0,0,0), 1));
        }

        // Тест отрицательных координат
        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        public void Constructor_NegativeCoordinates_ThrowsArgumentOutOfRangeException(int x, int y)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TextOperation("I", x, y, new Pixel(0,0,0), 1));
        }

        // Тест неверного масштаба
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Constructor_InvalidScale_ThrowsArgumentOutOfRangeException(int scale)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TextOperation("I", 0, 0, new Pixel(0,0,0), scale));
        }

        // Тест передачи null
        [Fact]
        public void Apply_NullSource_ThrowsArgumentNullException()
        {
            var op = new TextOperation("I", 0, 0, new Pixel(0,0,0), 1);
            Assert.Throws<ArgumentNullException>(() => op.Apply(null!));
        }
    }
}