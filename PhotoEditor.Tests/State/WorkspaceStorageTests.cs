using System;
using System.IO;
using PhotoEditor.Core.Models;
using PhotoEditor.Core.State;
using Xunit;

namespace PhotoEditor.Tests.State
{
    public class WorkspaceStorageTests
    {
        // Вспомогательный метод для создания пространства
        private Workspace CreateSampleWorkspace()
        {
            var ws = new Workspace();
            var pic1 = new Picture("pic1", 3, 2);
            pic1.SetPixel(0, 0, new Pixel(1, 2, 3, 4));
            var pic2 = new Picture("pic2", 2, 4);
            pic2.SetPixel(1, 1, new Pixel(5, 6, 7, 8));

            ws.Add(pic1);
            ws.Add(pic2);
            ws.Select(0); // Выбрана первая

            var collage = new Collage("col", 10, 10, new Pixel(10, 20, 30, 40));
            collage.Add(pic1, 2, 2);
            collage.Add(pic2, 5, 5);
            ws.AddCollage(collage);

            return ws;
        }

        // Получение пути к временному файлу
        private string GetTempFilePath() => Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");

        // Тест сохранения и загрузки (полный круг)
        [Fact]
        public void SaveAndLoad_RoundTrip_WorksCorrectly()
        {
            var ws = CreateSampleWorkspace();
            string path = GetTempFilePath();
            try
            {
                WorkspaceStorage.Save(ws, path);
                var loaded = WorkspaceStorage.Load(path);

                TestHelpers.AssertWorkspaceEqual(ws, loaded);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // Тест перезаписи файла
        [Fact]
        public void Save_Overwrite_WorksCorrectly()
        {
            var ws1 = new Workspace();
            ws1.Add(new Picture("pic1", 2, 2));

            var ws2 = new Workspace();
            ws2.Add(new Picture("pic2", 3, 3));

            string path = GetTempFilePath();
            try
            {
                WorkspaceStorage.Save(ws1, path);
                WorkspaceStorage.Save(ws2, path); // Перезаписываем

                var loaded = WorkspaceStorage.Load(path);
                Assert.Equal("pic2", loaded.Pictures[0].Name);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // Тест загрузки несуществующего файла
        [Fact]
        public void Load_NonExistentFile_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => WorkspaceStorage.Load(GetTempFilePath()));
        }

        // Тест невалидного JSON
        [Theory]
        [InlineData("это не json")]
        [InlineData("")]
        [InlineData("{ \"Pictures\": [")] // Обрезанный JSON
        [InlineData("null")]              // Файл с null
        public void Load_InvalidJsonContent_ThrowsInvalidDataException(string content)
        {
            string path = GetTempFilePath();
            try
            {
                File.WriteAllText(path, content);
                Assert.Throws<InvalidDataException>(() => WorkspaceStorage.Load(path));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // Тест повреждённых данных в валидном JSON
        [Fact]
        public void Load_CorruptedDataInValidJson_ThrowsInvalidDataException()
        {
            var ws = new Workspace();
            ws.Add(new Picture("pic", 2, 2));

            string path = GetTempFilePath();
            try
            {
                WorkspaceStorage.Save(ws, path);
                // Меняем ширину, чтобы она не совпадала с длиной массива
                string originalJson = File.ReadAllText(path);
                string corruptedJson = originalJson.Replace("\"Width\": 2", "\"Width\": 5");
                Assert.NotEqual(originalJson, corruptedJson);
                File.WriteAllText(path, corruptedJson);

                Assert.Throws<InvalidDataException>(() => WorkspaceStorage.Load(path));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // Тест невалидного CurrentIndex
        [Fact]
        public void Load_CorruptedCurrentIndexInValidJson_ThrowsInvalidDataException()
        {
            var ws = new Workspace();
            ws.Add(new Picture("pic", 2, 2)); // Одна картинка, индекс должен быть 0

            string path = GetTempFilePath();
            try
            {
                WorkspaceStorage.Save(ws, path);
                string originalJson = File.ReadAllText(path);
                // Меняем индекс на несуществующий
                string corruptedJson = originalJson.Replace("\"CurrentIndex\": 0", "\"CurrentIndex\": 99");

                Assert.NotEqual(originalJson, corruptedJson);

                File.WriteAllText(path, corruptedJson);

                // Ожидаем InvalidDataException, так как маппер поймает ошибку Select(99)
                Assert.Throws<InvalidDataException>(() => WorkspaceStorage.Load(path));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // Тест создания директории при сохранении
        [Fact]
        public void Save_DestinationFolderDoesNotExist_CreatesIt()
        {
            // Создаем путь, где папки "nested" еще нет
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string path = Path.Combine(tempDir, "nested", "save.json");
            var ws = new Workspace();

            try
            {
                WorkspaceStorage.Save(ws, path);
                Assert.True(File.Exists(path)); // Файл создан вместе с папками
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        // Тест передачи null рабочего пространства
        [Fact]
        public void Save_NullWorkspace_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => WorkspaceStorage.Save(null!, GetTempFilePath()));
        }

        // Тест пустого пути
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void SaveAndLoad_EmptyPath_ThrowsArgumentException(string path)
        {
            var ws = new Workspace();
            Assert.Throws<ArgumentException>(() => WorkspaceStorage.Save(ws, path));
            Assert.Throws<ArgumentException>(() => WorkspaceStorage.Load(path));
        }
    }
}