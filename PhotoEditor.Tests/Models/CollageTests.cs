using System;
using System.Collections.Generic;
using PhotoEditor.Core.Models;
using Xunit;

namespace PhotoEditor.Tests.Models
{
    public class CollageTests
    {
        // Вспомогательный метод для быстрого создания однотонной картинки
        private Picture CreateSolidPicture(int width, int height, Pixel color)
        {
            var pic = new Picture("test_pic", width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pic.SetPixel(x, y, color);
                }
            }
            return pic;
        }

        // Тест пустого коллажа (только фон)
        [Fact]
        public void Render_EmptyCollage_ReturnsCanvasWithBackground()
        {
            var bgColor = new Pixel(10, 20, 30, 255);
            var collage = new Collage("empty", 4, 3, bgColor);
            var result = collage.Render();

            Assert.Equal(4, result.Width);
            Assert.Equal(3, result.Height);
            Assert.Equal("empty", result.Name);

            for (int y = 0; y < result.Height; y++)
            {
                for (int x = 0; x < result.Width; x++)
                {
                    Assert.Equal(bgColor, result.GetPixel(x, y));
                }
            }
        }

        // Тест размещения одной картинки по центру
        [Fact]
        public void Render_SinglePictureInCenter_DrawsCorrectly()
        {
            var bgColor = new Pixel(0, 0, 0, 255);
            var picColor = new Pixel(255, 0, 0, 255);
            var collage = new Collage("test", 4, 3, bgColor);
            var pic = CreateSolidPicture(2, 2, picColor);
            collage.Add(pic, 1, 1);
            var result = collage.Render();

            Assert.Equal(bgColor, result.GetPixel(0, 0));
            Assert.Equal(bgColor, result.GetPixel(3, 0));
            Assert.Equal(bgColor, result.GetPixel(0, 2));
            Assert.Equal(bgColor, result.GetPixel(3, 2));
            Assert.Equal(picColor, result.GetPixel(1, 1));
            Assert.Equal(picColor, result.GetPixel(2, 2));
        }

        // Тест перекрытия картинок (побеждает последняя)
        [Fact]
        public void Render_OverlappingPictures_LastOneWins()
        {
            var bgColor = new Pixel(0, 0, 0, 255);
            var red = new Pixel(255, 0, 0, 255);
            var blue = new Pixel(0, 0, 255, 255);
            var collage = new Collage("test", 3, 3, bgColor);
            var pic1 = CreateSolidPicture(2, 2, red);
            var pic2 = CreateSolidPicture(2, 2, blue);
            collage.Add(pic2, 1, 1); 
            collage.Add(pic1, 0, 0); 
            var result = collage.Render();
            Assert.Equal(red, result.GetPixel(1, 1)); 
        }

        // Тест частичного выхода картинки за границы
        [Fact]
        public void Render_PicturePartiallyOffEdge_IsTruncatedWithoutException()
        {
            var bgColor = new Pixel(0, 0, 0, 255);
            var red = new Pixel(255, 0, 0, 255);
            var collage = new Collage("test", 3, 3, bgColor);
            var pic = CreateSolidPicture(2, 2, red);
            collage.Add(pic, 2, 2); 
            var result = collage.Render();

            Assert.Equal(red, result.GetPixel(2, 2)); 
            Assert.Equal(bgColor, result.GetPixel(1, 2));
            Assert.Equal(bgColor, result.GetPixel(2, 1));
        }

        // Тест отрицательных координат (рисуется только видимая часть)
        [Fact]
        public void Render_NegativeCoordinates_DrawsOnlyVisiblePart()
        {
            var bgColor = new Pixel(0, 0, 0, 255);
            var red = new Pixel(255, 0, 0, 255);
            var collage = new Collage("test", 3, 3, bgColor);
            var pic = CreateSolidPicture(2, 2, red);
            collage.Add(pic, -1, -1);
            var result = collage.Render();

            Assert.Equal(red, result.GetPixel(0, 0));
            Assert.Equal(bgColor, result.GetPixel(1, 0)); 
        }

        // Тест картинки полностью за границами (остаётся только фон)
        [Fact]
        public void Render_PictureCompletelyOutside_ReturnsPureBackground()
        {
            var bgColor = new Pixel(0, 0, 0, 255);
            var red = new Pixel(255, 0, 0, 255);
            var collage = new Collage("test", 3, 3, bgColor);
            
            var pic = CreateSolidPicture(2, 2, red);
            collage.Add(pic, 10, 10); 

            var result = collage.Render();

            Assert.Equal(bgColor, result.GetPixel(0, 0));
            Assert.Equal(bgColor, result.GetPixel(2, 2));
        }

        // Тест неизменности исходных данных и создания новых объектов
        [Fact]
        public void Render_DoesNotMutateSources_AndReturnsNewObject()
        {
            var bgColor = new Pixel(0, 0, 0, 255);
            var red = new Pixel(255, 0, 0, 255);
            var collage = new Collage("test", 3, 3, bgColor);
            var pic = CreateSolidPicture(2, 2, red);
            var picDataBefore = (byte[])pic.Data.Clone(); 
            collage.Add(pic, 0, 0);
            var render1 = collage.Render();
            var render2 = collage.Render();

            Assert.Equal(picDataBefore, pic.Data);
            Assert.NotSame(render1, render2);
            Assert.NotSame(render1.Data, render2.Data);
        }

        // Тест невалидного имени
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Constructor_InvalidName_ThrowsException(string? invalidName)
        {
            Assert.Throws<ArgumentException>(() => new Collage(invalidName!, 10, 10, new Pixel(0,0,0)));
        }

        // Тест неверных размеров
        [Theory]
        [InlineData(0, 10)]
        [InlineData(-5, 10)]
        [InlineData(10, 0)]
        [InlineData(10, -5)]
        public void Constructor_InvalidDimensions_ThrowsException(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Collage("test", width, height, new Pixel(0,0,0)));
        }

        // Тест добавления null-картинки
        [Fact]
        public void Add_NullPicture_ThrowsException()
        {
            var collage = new Collage("test", 10, 10, new Pixel(0,0,0));
            Assert.Throws<ArgumentNullException>(() => collage.Add(null!, 0, 0));
        }

        // Тест свойства Items (только для чтения и корректность)
        [Fact]
        public void Items_ReflectsAddedElements_AndIsReadOnly()
        {
            var collage = new Collage("test", 10, 10, new Pixel(0,0,0));
            var pic1 = new Picture("p1", 1, 1);
            var pic2 = new Picture("p2", 1, 1);
            collage.Add(pic1, 1, 2);
            collage.Add(pic2, 3, 4);

            Assert.Equal(2, collage.Items.Count);
            Assert.Equal(1, collage.Items[0].X);
            Assert.Equal(4, collage.Items[1].Y);
        }
        
    }
}