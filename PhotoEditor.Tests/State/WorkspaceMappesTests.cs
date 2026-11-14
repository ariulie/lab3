using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PhotoEditor.Core.Models;
using PhotoEditor.Core.State;
using Xunit;

namespace PhotoEditor.Tests.State
{
    public class WorkspaceMapperTests
    {
        // Тест полного круга преобразований
        [Fact]
        public void FullRoundTrip_PreservesAllData()
        {
            var ws = new Workspace();
            var pic1 = new Picture("pic1", 3, 2);
            var pic2 = new Picture("pic2", 2, 4);
            pic1.SetPixel(0, 0, new Pixel(1, 2, 3, 4));
            pic2.SetPixel(1, 1, new Pixel(5, 6, 7, 8));
            ws.Add(pic1);
            ws.Add(pic2);
            ws.Select(0); // Выбираем первую для проверки восстановления индекса
            var collage = new Collage("col", 10, 10, new Pixel(10, 20, 30, 40));
            collage.Add(pic1, 2, 2);
            collage.Add(pic2, 5, 5);
            ws.AddCollage(collage);

            var dto = WorkspaceMapper.ToDto(ws);
            var restoredWs = WorkspaceMapper.FromDto(dto);

            TestHelpers.AssertWorkspaceEqual(ws, restoredWs);
            Assert.Equal(3, restoredWs.Current!.Width); // Текущая должна быть pic1 (3x2)
            Assert.Equal(2, restoredWs.Current!.Height);
        }

        // Тест пустого пространства
        [Fact]
        public void EmptyWorkspace_RoundTrip_PreservesEmptyState()
        {
            var ws = new Workspace();
            var dto = WorkspaceMapper.ToDto(ws);
            var restoredWs = WorkspaceMapper.FromDto(dto);

            Assert.Empty(restoredWs.Pictures);
            Assert.Empty(restoredWs.Collages);
            Assert.Equal(-1, restoredWs.CurrentIndex);
        }

        // Тест глубокого копирования в ToDto
        [Fact]
        public void ToDto_CreatesDeepCopy()
        {
            var ws = new Workspace();
            var pic = new Picture("pic", 2, 2);
            pic.Data[0] = 10;
            ws.Add(pic);

            var dto = WorkspaceMapper.ToDto(ws);

            // Меняем данные в DTO
            dto.Pictures[0].Data[0] = 99;

            // В оригинале должно остаться 10
            Assert.Equal(10, ws.Pictures[0].Data[0]);
        }

        // Тест глубокого копирования в FromDto
        [Fact]
        public void FromDto_CreatesDeepCopy()
        {
            var dto = new WorkspaceDto();
            var picDto = new PictureDto { Name = "pic", Width = 2, Height = 2, Data = new byte[16] };
            picDto.Data[0] = 10;
            dto.Pictures.Add(picDto);
            dto.CurrentIndex = 0;

            var ws = WorkspaceMapper.FromDto(dto);

            // Меняем данные в DTO после загрузки
            picDto.Data[0] = 99;

            // В новой картинке должно остаться 10 (конструктор скопировал массив)
            Assert.Equal(10, ws.Pictures[0].Data[0]);
        }

        // Тест передачи null DTO
        [Fact]
        public void FromDto_NullDto_ThrowsInvalidDataException()
        {
            Assert.Throws<InvalidDataException>(() => WorkspaceMapper.FromDto(null!));
        }

        // Тест null-списков
        [Fact]
        public void FromDto_NullLists_ThrowsInvalidDataException()
        {
            var dto1 = new WorkspaceDto { Pictures = null! };
            var dto2 = new WorkspaceDto { Collages = null! };

            Assert.Throws<InvalidDataException>(() => WorkspaceMapper.FromDto(dto1));
            Assert.Throws<InvalidDataException>(() => WorkspaceMapper.FromDto(dto2));
        }

        // Тест некорректных данных картинки
        [Fact]
        public void FromDto_InvalidPictureData_ThrowsInvalidDataException()
        {
            // Неверная длина массива
            var dto1 = new WorkspaceDto();
            dto1.Pictures.Add(new PictureDto { Name = "pic", Width = 2, Height = 2, Data = new byte[5] }); // Должно быть 16

            // Пустое имя
            var dto2 = new WorkspaceDto();
            dto2.Pictures.Add(new PictureDto { Name = "", Width = 2, Height = 2, Data = new byte[16] });

            // Нулевая ширина
            var dto3 = new WorkspaceDto();
            dto3.Pictures.Add(new PictureDto { Name = "pic", Width = 0, Height = 2, Data = new byte[16] });

            Assert.Throws<InvalidDataException>(() => WorkspaceMapper.FromDto(dto1));
            Assert.Throws<InvalidDataException>(() => WorkspaceMapper.FromDto(dto2));
            Assert.Throws<InvalidDataException>(() => WorkspaceMapper.FromDto(dto3));
        }
        // Тест некорректных данных коллажа
        [Fact]
        public void FromDto_InvalidCollageData_ThrowsInvalidDataException()
        {
            // Пустое имя коллажа
            var dto1 = new WorkspaceDto();
            dto1.Collages.Add(new CollageDto { Name = "", Width = 10, Height = 10 });

            // Нулевая ширина
            var dto2 = new WorkspaceDto();
            dto2.Collages.Add(new CollageDto { Name = "col", Width = 0, Height = 10 });

            // Элемент с Picture = null
            var dto3 = new WorkspaceDto();
            var badCollage = new CollageDto { Name = "col", Width = 10, Height = 10 };
            badCollage.Items.Add(new CollageItemDto { Picture = null!, X = 0, Y = 0 });
            dto3.Collages.Add(badCollage);

            // null внутри списков
            var dto4 = new WorkspaceDto();
            dto4.Pictures.Add(null!); // null вместо картинки

            var dto5 = new WorkspaceDto();
            dto5.Collages.Add(null!); // null вместо коллажа

            // Все эти случаи превращаются в InvalidDataException
            Assert.Throws<InvalidDataException>(() => WorkspaceMapper.FromDto(dto1));
            Assert.Throws<InvalidDataException>(() => WorkspaceMapper.FromDto(dto2));
            Assert.Throws<InvalidDataException>(() => WorkspaceMapper.FromDto(dto3));
            Assert.Throws<InvalidDataException>(() => WorkspaceMapper.FromDto(dto4));
            Assert.Throws<InvalidDataException>(() => WorkspaceMapper.FromDto(dto5));
        }

        // Тест невалидного CurrentIndex
        [Fact]
        public void FromDto_InvalidCurrentIndex_ThrowsInvalidDataException()
        {
            // Индекс 5 при одной картинке
            var dto1 = new WorkspaceDto();
            dto1.Pictures.Add(new PictureDto { Name = "pic", Width = 1, Height = 1, Data = new byte[4] });
            dto1.CurrentIndex = 5;

            // Индекс 0 при пустом списке картинок
            var dto2 = new WorkspaceDto { CurrentIndex = 0 };

            Assert.Throws<InvalidDataException>(() => WorkspaceMapper.FromDto(dto1));
            Assert.Throws<InvalidDataException>(() => WorkspaceMapper.FromDto(dto2));
        }

        // Тест полного круга через JSON
        [Fact]
        public void FullRoundTrip_ThroughJson_WorksCorrectly()
        {
            var ws = new Workspace();
            var pic1 = new Picture("pic1", 3, 2);
            pic1.SetPixel(0, 0, new Pixel(255, 0, 0, 255));
            var pic2 = new Picture("pic2", 2, 4);
            pic2.SetPixel(1, 1, new Pixel(0, 255, 0, 255));
            ws.Add(pic1);
            ws.Add(pic2);
            ws.Select(0);
            var collage = new Collage("col", 5, 5, new Pixel(1, 2, 3, 4));
            collage.Add(pic1, 0, 0);
            ws.AddCollage(collage);

            var dto = WorkspaceMapper.ToDto(ws);
            string json = JsonSerializer.Serialize(dto);
            var deserializedDto = JsonSerializer.Deserialize<WorkspaceDto>(json);
            var restoredWs = WorkspaceMapper.FromDto(deserializedDto!);

            TestHelpers.AssertWorkspaceEqual(ws, restoredWs);
            Assert.Equal(0, restoredWs.CurrentIndex);
        }
    }
}