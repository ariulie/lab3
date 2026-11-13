using System;
using System.Collections.Generic;
using System.Text.Json;
using PhotoEditor.Core.State;
using Xunit;

namespace PhotoEditor.Tests.State
{
    public class WorkspaceDtoTests
    {
        // Тест полной сериализации и десериализации
        [Fact]
        public void SerializeAndDeserialize_FullWorkspace_SavesAllDataAndOrder()
        {
            // Первая картинка
            var picDto1 = new PictureDto
            {
                Name = "pic1",
                Width = 2,
                Height = 1,
                Data = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80 }
            };
            
            // Вторая картинка
            var picDto2 = new PictureDto
            {
                Name = "pic2",
                Width = 1,
                Height = 2,
                Data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
            };

            // Элементы коллажа
            var itemDto1 = new CollageItemDto { Picture = picDto1, X = 15, Y = 25 };
            var itemDto2 = new CollageItemDto { Picture = picDto2, X = 5, Y = 10 };

            // Коллаж
            var collageDto = new CollageDto
            {
                Name = "my_collage",
                Width = 100, Height = 200,
                BackgroundR = 1, BackgroundG = 2, BackgroundB = 3, BackgroundA = 4,
                Items = new List<CollageItemDto> { itemDto1, itemDto2 } 
            };

            // Рабочее пространство
            var workspaceDto = new WorkspaceDto
            {
                Pictures = new List<PictureDto> { picDto1, picDto2 },
                Collages = new List<CollageDto> { collageDto },
                CurrentIndex = 1
            };

            // Сериализация и десериализация
            string json = JsonSerializer.Serialize(workspaceDto);
            WorkspaceDto? deserialized = JsonSerializer.Deserialize<WorkspaceDto>(json);

            // Проверка основных данных
            Assert.NotNull(deserialized);
            Assert.Equal(1, deserialized.CurrentIndex);
            Assert.Equal(2, deserialized.Pictures.Count);
            Assert.Equal(2, deserialized.Collages[0].Items.Count);

            // Проверка порядка элементов
            Assert.Equal(15, deserialized.Collages[0].Items[0].X);
            Assert.Equal(5, deserialized.Collages[0].Items[1].X);

            // Проверка вложенной картинки
            var restoredInnerPic = deserialized.Collages[0].Items[0].Picture;
            Assert.Equal("pic1", restoredInnerPic.Name);
            Assert.Equal(2, restoredInnerPic.Width);
            Assert.Equal(1, restoredInnerPic.Height);
            Assert.Equal(new byte[] { 10, 20, 30, 40, 50, 60, 70, 80 }, restoredInnerPic.Data);
        }

        // Тест сериализации пустого рабочего пространства
        [Fact]
        public void SerializeAndDeserialize_EmptyWorkspace_DoesNotCreateNulls()
        {
            var emptyDto = new WorkspaceDto { CurrentIndex = -1 };
            string json = JsonSerializer.Serialize(emptyDto);
            WorkspaceDto? deserialized = JsonSerializer.Deserialize<WorkspaceDto>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(-1, deserialized.CurrentIndex);
            
            // Проверка, что списки не null
            Assert.NotNull(deserialized.Pictures);
            Assert.NotNull(deserialized.Collages);
            Assert.Empty(deserialized.Pictures);
            Assert.Empty(deserialized.Collages);
        }
    }
}