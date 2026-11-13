using System;
using System.Collections.Generic;
using PhotoEditor.Core.Models;
using PhotoEditor.Core.Operations;
using PhotoEditor.Core.State;
using Xunit;

namespace PhotoEditor.Tests.State
{
    public class WorkspaceTests
    {
        // Тест пустого рабочего пространства
        [Fact]
        public void NewWorkspace_IsEmpty()
        {
            var ws = new Workspace();
            Assert.Empty(ws.Pictures);
            Assert.Equal(-1, ws.CurrentIndex);
            Assert.Null(ws.Current);
            Assert.Empty(ws.Collages);
        }

        // Тест добавления картинки и выбора её текущей
        [Fact]
        public void Add_AddsPictureAndMakesItCurrent()
        {
            var ws = new Workspace();
            var pic1 = new Picture("pic1", 2, 3);
            var pic2 = new Picture("pic2", 4, 5);
            ws.Add(pic1);
            Assert.Same(pic1, ws.Current);
            Assert.Equal(0, ws.CurrentIndex);
            ws.Add(pic2);
            Assert.Same(pic2, ws.Current);
            Assert.Equal(1, ws.CurrentIndex);
        }

        // Тест смены текущей картинки
        [Fact]
        public void Select_ChangesCurrentPicture()
        {
            var ws = new Workspace();
            var pic1 = new Picture("pic1", 2, 3);
            var pic2 = new Picture("pic2", 4, 5);
            ws.Add(pic1);
            ws.Add(pic2);
            ws.Select(0);

            Assert.Same(pic1, ws.Current);
            Assert.Equal(0, ws.CurrentIndex);
        }

        // Тест невалидного индекса
        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void Select_InvalidIndex_ThrowsException(int invalidIndex)
        {
            var ws = new Workspace();
            ws.Add(new Picture("pic1", 2, 3));
            ws.Add(new Picture("pic2", 4, 5));

            Assert.Throws<ArgumentOutOfRangeException>(() => ws.Select(invalidIndex));
        }

        // Тест удаления текущей картинки
        [Fact]
        public void Remove_CurrentPicture_SelectsAnother()
        {
            var ws = new Workspace();
            var pic1 = new Picture("pic1", 2, 3);
            ws.Add(pic1);
            ws.Add(new Picture("pic2", 4, 5)); // Текущая — pic2
            ws.Remove(1); // Удаляем текущую

            Assert.Single(ws.Pictures);
            Assert.Same(pic1, ws.Current); 
            Assert.Equal(0, ws.CurrentIndex);
        }

        // Тест удаления картинки справа от текущей
        [Fact]
        public void Remove_PictureRightOfCurrent_IndexRemains()
        {
            var ws = new Workspace();
            var pic1 = new Picture("pic1", 2, 3);
            ws.Add(pic1);
            ws.Add(new Picture("pic2", 4, 5));
            ws.Add(new Picture("pic3", 6, 7));
            ws.Select(0); // Текущая — pic1
            ws.Remove(2); // Удаляем pic3

            Assert.Equal(2, ws.Pictures.Count);
            Assert.Same(pic1, ws.Current);
            Assert.Equal(0, ws.CurrentIndex); // Индекс не изменился
        }

        // Тест удаления последней картинки
        [Fact]
        public void Remove_LastPicture_ResetsState()
        {
            var ws = new Workspace();
            ws.Add(new Picture("pic1", 2, 3));
            ws.Remove(0);

            Assert.Empty(ws.Pictures);
            Assert.Null(ws.Current);
            Assert.Equal(-1, ws.CurrentIndex);
        }

        // Тест применения операции только к текущей картинке
        [Fact]
        public void Apply_ReplacesCurrentPictureOnly()
        {
            var ws = new Workspace();
            // Неквадратные картинки
            var pic1 = new Picture("pic1", 2, 3); 
            var pic2 = new Picture("pic2", 4, 5);
            var pic3 = new Picture("pic3", 6, 7);
            
            ws.Add(pic1);
            ws.Add(pic2);
            ws.Add(pic3);
            
            // Делаем текущей первую картинку
            ws.Select(0); 

            var op = new RotateOperation(90);
            var result = ws.Apply(op);

            // Результат вернулся и стал текущим
            Assert.Same(result, ws.Current);
            
            // Размеры изменились с 2x3 на 3x2
            Assert.Equal(3, ws.Current!.Width); 
            Assert.Equal(2, ws.Current!.Height);

            // Количество не изменилось
            Assert.Equal(3, ws.Pictures.Count);

            // Заменилась только первая, остальные остались теми же
            Assert.Same(result, ws.Pictures[0]);
            Assert.Same(pic2, ws.Pictures[1]);
            Assert.Same(pic3, ws.Pictures[2]);

            // Исходный pic1 не изменился (остался 2x3)
            Assert.Equal(2, pic1.Width);
            Assert.Equal(3, pic1.Height);
        }

        // Тест применения операции без текущей картинки
        [Fact]
        public void Apply_NoCurrentPicture_ThrowsInvalidOperationException()
        {
            var ws = new Workspace();
            var op = new RotateOperation(90);

            Assert.Throws<InvalidOperationException>(() => ws.Apply(op));
        }

        // Тест передачи null-операции
        [Fact]
        public void Apply_NullOperation_ThrowsArgumentNullException()
        {
            var ws = new Workspace();
            ws.Add(new Picture("pic", 2, 3));

            Assert.Throws<ArgumentNullException>(() => ws.Apply(null!));
        }

        // Тест добавления и отрисовки коллажа
        [Fact]
        public void AddCollage_And_RenderCollage_WorksCorrectly()
        {
            var ws = new Workspace();
            var collage = new Collage("collage", 5, 4, new Pixel(0, 0, 0));
            ws.AddCollage(collage);
            Assert.Single(ws.Collages);
            ws.Add(new Picture("existing", 2, 3));
            ws.RenderCollage(0);

            Assert.Single(ws.Collages); // Коллаж остался в списке
            Assert.Equal(2, ws.Pictures.Count); // Добавилась новая картинка
            Assert.Equal(5, ws.Current!.Width);
            Assert.Equal(4, ws.Current!.Height);
            Assert.Equal(1, ws.CurrentIndex); // Индекс указывает на новую картинку
        }
    }
}