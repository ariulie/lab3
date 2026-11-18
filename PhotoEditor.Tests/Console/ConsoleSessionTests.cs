using System;
using System.IO;
using PhotoEditor.ConsoleApp;
using PhotoEditor.Core.Codecs;
using PhotoEditor.Core.Models;
using PhotoEditor.Core.State;
using Xunit;

namespace PhotoEditor.Tests.ConsoleApp
{
    public class ConsoleSessionTests
    {
        // Создание временного BMP-файла
        private string CreateTempBmp(string picName = "test_pic")
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bmp");
            var pic = new Picture(picName, 3, 2);
            pic.SetPixel(0, 0, new Pixel(255, 0, 0));
            new BmpCodec().Save(pic, path);
            return path;
        }

        // Получение пути к временному файлу состояния
        private string TempStatePath() =>
            Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");

        // Запуск сессии с заданными командами
        private static (string output, Exception? ex) RunSession(
            string[] lines, string statePath)
        {
            var input = new StringReader(string.Join("\n", lines) + "\n");
            var output = new StringWriter();
            Exception? ex = null;
            try
            {
                new ConsoleSession().Run(input, output, statePath);
            }
            catch (Exception e)
            {
                ex = e;
            }
            return (output.ToString(), ex);
        }

        // Тест: команда exit сохраняет файл и выводит приветствие
        [Fact]
        public void Exit_SavesStateFile_AndPrintsGreeting()
        {
            string statePath = TempStatePath();
            try
            {
                var (text, ex) = RunSession(new[] { "exit" }, statePath);

                Assert.Null(ex);
                Assert.Contains("Фоторедактор", text);
                Assert.Contains("Сохранение и выход", text);
                Assert.True(File.Exists(statePath));
            }
            finally { if (File.Exists(statePath)) File.Delete(statePath); }
        }

        // Тест: конец ввода сохраняет файл
        [Fact]
        public void EndOfInput_SavesStateFile()
        {
            string statePath = TempStatePath();
            try
            {
                // Пустой StringReader — ReadLine сразу вернёт null
                var (text, ex) = RunSession(Array.Empty<string>(), statePath);

                Assert.Null(ex);
                Assert.True(File.Exists(statePath));
            }
            finally { if (File.Exists(statePath)) File.Delete(statePath); }
        }

        // Тест: две сессии восстанавливают картинки и коллажи
        [Fact]
        public void TwoSessions_RestorePicturesAndCollages()
        {
            string bmpPath = CreateTempBmp();
            string statePath = TempStatePath();
            try
            {
                // Сессия 1: open → rotate 90 → collage new → exit
                RunSession(new[]
                { $"open {bmpPath}", "rotate 90", "collage new c 5 4 0 0 0", "exit"}, statePath);
                // Сессия 2: читаем, что сохранилось
                var (text2, ex2) = RunSession(new[]
                { "list", "collage list", "info", "exit" }, statePath);

                Assert.Null(ex2);
                Assert.Contains("Состояние восстановлено", text2);
                Assert.Contains("2×3", text2); // картинка повёрнута (3×2 → 2×3)
                Assert.Contains("c (5×4)", text2); // коллаж на месте
                Assert.Contains("номер: 1 из 1", text2); // текущая — единственная
            }
            finally
            {
                if (File.Exists(bmpPath)) File.Delete(bmpPath);
                if (File.Exists(statePath)) File.Delete(statePath);
            }
        }

        // Тест: выбранная картинка сохраняется между сессиями
        [Fact]
        public void SelectedPicture_PersistsAcrossSessions()
        {
            string bmp1 = CreateTempBmp("a");
            string bmp2 = CreateTempBmp("b");
            string statePath = TempStatePath();
            try
            {
                RunSession(new[]
                { $"open {bmp1}", $"open {bmp2}", "select 1", "exit" }, statePath);

                var (text2, _) = RunSession(new[] { "info", "exit" }, statePath);

                Assert.Contains("номер: 1 из 2", text2);
            }
            finally
            {
                if (File.Exists(bmp1)) File.Delete(bmp1);
                if (File.Exists(bmp2)) File.Delete(bmp2);
                if (File.Exists(statePath)) File.Delete(statePath);
            }
        }

        // Тест: нет файла состояния — старт с пустого без предупреждений
        [Fact]
        public void NoStateFile_StartsEmpty_WithoutWarning()
        {
            string statePath = TempStatePath();
            // намеренно НЕ создаём файл
            try
            {
                var (text, _) = RunSession(new[] { "list", "exit" }, statePath);

                Assert.Contains("Нет открытых изображений", text);
                Assert.DoesNotContain("повреждён", text);
            }
            finally { if (File.Exists(statePath)) File.Delete(statePath); }
        }
        [Fact]
        public void RestoredState_ContainsRotatedPixelData()
        {
            string bmpPath = CreateTempBmp(); // Красный пиксель (255,0,0) в (0,0)
            string statePath = TempStatePath();
            try
            {
                RunSession(new[] { $"open {bmpPath}", "rotate 90", "exit" }, statePath);

                // Загружаем сохраненный JSON напрямую, минуя консоль
                var ws = WorkspaceStorage.Load(statePath);

                // Исходная картинка 3x2 после поворота на 90 стала 2x3.
                // Пиксель из (0,0) переехал в (1,0).
                var p = ws.Pictures[0].GetPixel(1, 0);
                Assert.Equal(255, p.R);
                Assert.Equal(0, p.G);
            }
            finally
            {
                if (File.Exists(bmpPath)) File.Delete(bmpPath);
                if (File.Exists(statePath)) File.Delete(statePath);
            }
        }

        // Текст после exit не выполняется
        [Fact]
        public void CommandsAfterExit_AreNotExecuted()
        {
            string bmpPath = CreateTempBmp();
            string statePath = TempStatePath();
            try
            {
                // Пытаемся сделать open после exit
                RunSession(new[] { "exit", $"open {bmpPath}" }, statePath);

                var ws = WorkspaceStorage.Load(statePath);
                Assert.Empty(ws.Pictures); // Картинка не должна была загрузиться
            }
            finally
            {
                if (File.Exists(bmpPath)) File.Delete(bmpPath);
                if (File.Exists(statePath)) File.Delete(statePath);
            }
        }

        //  Проверка ArgumentNullException
        [Fact]
        public void Run_WithNullArguments_ThrowsArgumentNullException()
        {
            var session = new ConsoleSession();
            var reader = new StringReader("");
            var writer = new StringWriter();
            string path = "test.json";

            Assert.Throws<ArgumentNullException>(() => session.Run(null!, writer, path));
            Assert.Throws<ArgumentNullException>(() => session.Run(reader, null!, path));
            Assert.Throws<ArgumentNullException>(() => session.Run(reader, writer, null!));
        }

        // Проверка, что при битом файле новый workspace.json корректно читается
        [Fact]
        public void CorruptedFile_ReplacedWithValidState()
        {
            string statePath = TempStatePath();
            File.WriteAllText(statePath, "это не json");
            string badPath = statePath + ".bad";
            try
            {
                RunSession(new[] { "exit" }, statePath);

                // Проверяем, что новый файл не просто "не мусор", а валидный JSON,
                // который WorkspaceStorage может прочитать без исключений.
                var ws = WorkspaceStorage.Load(statePath);
                Assert.NotNull(ws);
                Assert.Empty(ws.Pictures);
            }
            finally
            {
                if (File.Exists(statePath)) File.Delete(statePath);
                if (File.Exists(badPath)) File.Delete(badPath);
            }
        }

        // Приглашение > 
        [Fact]
        public void Run_PrintsPromptSymbol()
        {
            string statePath = TempStatePath();
            try
            {
                var (text, _) = RunSession(new[] { "exit" }, statePath);

                // Проверяем, что приглашение вообще есть в выводе
                Assert.Contains("> ", text);
            }
            finally
            {
                if (File.Exists(statePath)) File.Delete(statePath);
            }
        }

        // Тест: повреждённый файл переименовывается в .bad и создаётся новый
        [Fact]
        public void CorruptedStateFile_IsRenamedToBad_AndNewOneCreated()
        {
            string statePath = TempStatePath();
            string badPath = statePath + ".bad";
            string garbage = "это не json, а мусор";
            File.WriteAllText(statePath, garbage);
            try
            {
                var (text, ex) = RunSession(new[] { "exit" }, statePath);

                Assert.Null(ex);
                Assert.Contains("повреждён", text);

                // старый битый файл переехал в .bad с тем же содержимым
                Assert.True(File.Exists(badPath));
                Assert.Equal(garbage, File.ReadAllText(badPath));

                // а по statePath теперь новый, корректный файл 
                Assert.True(File.Exists(statePath));
                Assert.NotEqual(garbage, File.ReadAllText(statePath));
            }
            finally
            {
                if (File.Exists(statePath)) File.Delete(statePath);
                if (File.Exists(badPath)) File.Delete(badPath);
            }
        }

        // Тест: ошибка сохранения выводит сообщение, но не роняет программу
        [Fact]
        public void SaveError_PrintsMessage_DoesNotCrash()
        {
            // Делаем файл и пытаемся сохранить "внутрь" него, как в подкаталог.
            string fakeDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bmp");
            File.WriteAllText(fakeDir, "not a dir");
            string badStatePath = Path.Combine(fakeDir, "x.json");
            try
            {
                var (text, ex) = RunSession(new[] { "exit" }, badStatePath);

                Assert.Null(ex);
                Assert.Contains("Не удалось сохранить состояние", text);
            }
            finally { if (File.Exists(fakeDir)) File.Delete(fakeDir); }
        }

        // Тест: ошибка команды не останавливает сессию
        [Fact]
        public void CommandError_DoesNotStopSession()
        {
            string statePath = TempStatePath();
            try
            {
                var (text, ex) = RunSession(new[]
                { "open нет_такого.bmp", "list", "exit" }, statePath);

                Assert.Null(ex);
                Assert.Contains("Ошибка:", text);
                Assert.Contains("Нет открытых изображений", text);
            }
            finally { if (File.Exists(statePath)) File.Delete(statePath); }
        }
    }
}