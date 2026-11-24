using System;
using System.IO;
using PhotoEditor.ConsoleApp;
using PhotoEditor.Core.Codecs;
using PhotoEditor.Core.Models;
using PhotoEditor.Core.State;
using Xunit;

namespace PhotoEditor.Tests.ConsoleApp
{
    public class CommandProcessorTests
    {
        // Создание процессора с тестовой картинкой
        private (CommandProcessor proc, Workspace ws) CreateProcessorWithPicture()
        {
            var ws = new Workspace();
            var codecs = new CodecRegistry();
            var proc = new CommandProcessor(ws, codecs);

            var pic = new Picture("test", 3, 2);
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    // Уникальный цвет для каждого пикселя
                    pic.SetPixel(x, y, new Pixel((byte)(x * 40 + 10), (byte)(y * 40 + 10), (byte)(x + y + 10)));
                }
            }
            ws.Add(pic);

            return (proc, ws);
        }

        // Создание пустого процессора
        private (CommandProcessor proc, Workspace ws) CreateProcessor()
        {
            var ws = new Workspace();
            var codecs = new CodecRegistry();
            return (new CommandProcessor(ws, codecs), ws);
        }

        // Создание временного BMP-файла
        private string CreateTempBmp(string picName = "test_pic")
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bmp");
            var pic = new Picture(picName, 3, 2); // Неквадратная
            pic.SetPixel(0, 0, new Pixel(255, 0, 0)); // Оставляем след
            new BmpCodec().Save(pic, path);
            return path;
        }

        // Тест сообщений при пустых коллажах
        [Fact]
        public void ParseIndex_EmptyCollages_ReturnsCorrectMessage()
        {
            var (proc, _) = CreateProcessor();
            // коллажей нет — должно быть про коллажи
            Assert.Equal("Ошибка: Нет созданных коллажей.",
                proc.Execute("collage render 1"));
            // есть коллаж, но нет картинок — сообщение про картинки
            proc.Execute("collage new c 5 4 0 0 0");
            Assert.Equal("Ошибка: Нет открытых изображений.",
                proc.Execute("collage add 1 1 0 0"));
        }

        // Тест пустого или null-ввода
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void Execute_EmptyOrNullInput_ReturnsEmptyString(string? input)
        {
            var (processor, _) = CreateProcessor();

            string result = processor.Execute(input!);

            Assert.Equal(string.Empty, result);
            Assert.False(processor.ShouldExit);
        }

        // Тест команды help
        [Theory]
        [InlineData("help")]
        [InlineData("  help  ")]
        [InlineData("HELP")]
        [InlineData("HeLp")]
        [InlineData("help abc")]
        public void Execute_HelpCommand_ReturnsHelpText_AndDoesNotExit(string input)
        {
            var (processor, _) = CreateProcessor();

            string result = processor.Execute(input);

            Assert.Contains("Доступные команды", result);
            Assert.False(processor.ShouldExit);
        }

        // Тест команды exit
        [Theory]
        [InlineData("exit")]
        [InlineData("EXIT")]
        [InlineData("ExIt")]
        [InlineData("  exit  ")]
        public void Execute_ExitCommand_SetsShouldExitAndReturnsMessage(string input)
        {
            var (processor, _) = CreateProcessor();
            Assert.False(processor.ShouldExit);

            string result = processor.Execute(input);

            Assert.True(processor.ShouldExit);
            Assert.Contains("Сохранение и выход", result);
        }

        // Тест неизвестной команды
        [Fact]
        public void Execute_UnknownCommand_ReturnsErrorMessage_AndDoesNotExit()
        {
            var (processor, _) = CreateProcessor();

            string result = processor.Execute("какая-то_фигня");

            Assert.Contains("Неизвестная команда", result);
            Assert.Contains("help", result);
            Assert.False(processor.ShouldExit);
        }

        // Тест загрузки файла и выбора текущей картинки
        [Fact]
        public void Open_LoadsFileAndMakesItCurrent()
        {
            var (proc, ws) = CreateProcessor();
            string path = CreateTempBmp();
            string expectedName = Path.GetFileNameWithoutExtension(path);

            try
            {
                string result = proc.Execute($"open {path}");

                // Проверяем, что имя совпадает с именем файла
                Assert.Contains($"Открыто: {expectedName} (3×2)", result);
                Assert.Single(ws.Pictures);
                Assert.NotNull(ws.Current);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // Тест открытия файла с пробелами и кавычками
        [Fact]
        public void Open_WithQuotesAndSpaces_Works()
        {
            var (proc, ws) = CreateProcessor();
            string dir = Path.Combine(Path.GetTempPath(), "folder with space");
            Directory.CreateDirectory(dir);

            // Называем файл "file.bmp", значит при загрузке имя картинки станет "file"
            string path = Path.Combine(dir, "file.bmp");
            new BmpCodec().Save(new Picture("space_pic", 3, 2), path);

            try
            {
                string result = proc.Execute($"open \"{path}\"");

                Assert.Contains("Открыто: file", result);
                Assert.Single(ws.Pictures);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (Directory.Exists(dir)) Directory.Delete(dir);
            }
        }

        // Тест open без аргументов
        [Fact]
        public void Open_WithoutArgs_ReturnsError()
        {
            var (proc, _) = CreateProcessor();
            string result = proc.Execute("open");
            Assert.StartsWith("Ошибка: Использование:", result);
        }

        // Тест open с некорректным файлом
        [Fact]
        public void Open_InvalidFile_ReturnsErrorAndKeepsWorkspaceEmpty()
        {
            var (proc, ws) = CreateProcessor();

            string result1 = proc.Execute("open missing_file.bmp");
            string result2 = proc.Execute("open image.png");

            Assert.StartsWith("Ошибка:", result1);
            Assert.StartsWith("Ошибка:", result2);
            Assert.Empty(ws.Pictures);
        }

        // Тест open с повреждённым BMP
        [Fact]
        public void Open_CorruptedBmpFile_ReturnsError()
        {
            var (proc, ws) = CreateProcessor();
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bmp");
            File.WriteAllText(path, "Это не BMP, а просто текст"); // Пишем мусор

            try
            {
                string result = proc.Execute($"open {path}");
                Assert.StartsWith("Ошибка:", result);
                Assert.Empty(ws.Pictures);
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        // Тест открытия второго файла
        [Fact]
        public void Open_SecondFile_ReturnsNumberTwo()
        {
            var (proc, ws) = CreateProcessor();
            string path1 = CreateTempBmp();
            string path2 = CreateTempBmp();

            try
            {
                proc.Execute($"open {path1}");
                string result = proc.Execute($"open {path2}");

                Assert.Contains("номер 2", result);
                Assert.Equal(2, ws.Pictures.Count);
            }
            finally
            {
                if (File.Exists(path1)) File.Delete(path1);
                if (File.Exists(path2)) File.Delete(path2);
            }
        }

        // Тест save без картинки
        [Fact]
        public void Save_NoPicture_ReturnsError()
        {
            var (proc, _) = CreateProcessor();
            string result = proc.Execute("save test.bmp");
            Assert.Equal("Ошибка: Нет открытого изображения для сохранения.", result);
        }

        // Тест save после open
        [Fact]
        public void Save_AfterOpen_CreatesFileWithSameData()
        {
            var (proc, ws) = CreateProcessor();
            string path = CreateTempBmp("pic1");
            string savePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bmp");

            try
            {
                proc.Execute($"open {path}");
                string result = proc.Execute($"save {savePath}");

                Assert.Equal($"Сохранено: {savePath}", result);
                Assert.True(File.Exists(savePath));

                var loadedPic = new BmpCodec().Load(savePath);
                Assert.Equal(ws.Current!.Data, loadedPic.Data);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(savePath)) File.Delete(savePath);
            }
        }

        // Тест save с неподдерживаемым расширением
        [Fact]
        public void Save_UnsupportedExtension_ReturnsError()
        {
            var (proc, ws) = CreateProcessor();
            ws.Add(new Picture("pic", 2, 2));
            string savePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".gif");

            string result = proc.Execute($"save {savePath}");

            Assert.StartsWith("Ошибка:", result); // CodecRegistry не знает .png
        }

        // Тест команды list
        [Fact]
        public void List_EmptyAndWithPictures_ReturnsCorrectFormat()
        {
            var (proc, ws) = CreateProcessor();

            Assert.Equal("Нет открытых изображений.", proc.Execute("list"));

            ws.Add(new Picture("pic1", 3, 2));
            ws.Add(new Picture("pic2", 4, 5));

            string list1 = proc.Execute("list");
            Assert.Contains(" 1. pic1 (3×2)", list1);
            Assert.Contains("* 2. pic2 (4×5)", list1);

            proc.Execute("select 1");
            string list2 = proc.Execute("list");
            Assert.Contains("* 1. pic1 (3×2)", list2);
            Assert.Contains(" 2. pic2 (4×5)", list2);
        }

        // Тест невалидного ввода select
        [Theory]
        [InlineData("select 0")]
        [InlineData("select -1")]
        [InlineData("select 3")]
        [InlineData("select abc")]
        [InlineData("select")]
        public void Select_InvalidInput_ReturnsErrorAndDoesNotChangeIndex(string command)
        {
            var (proc, ws) = CreateProcessor();
            ws.Add(new Picture("pic1", 3, 2));
            ws.Add(new Picture("pic2", 4, 5));

            string result = proc.Execute(command);

            Assert.StartsWith("Ошибка:", result);
            Assert.Equal(1, ws.CurrentIndex); // Индекс не изменился
        }

        // Тест select и remove на пустом пространстве
        [Fact]
        public void SelectAndRemove_OnEmptyWorkspace_ReturnsError()
        {
            var (proc, _) = CreateProcessor();

            string selectResult = proc.Execute("select 1");
            string removeResult = proc.Execute("remove 1");

            Assert.Equal("Ошибка: Нет открытых изображений.", selectResult);
            Assert.Equal("Ошибка: Нет открытых изображений.", removeResult);
        }

        // Тест select с лишними аргументами
        [Fact]
        public void Select_ExtraArguments_ReturnsError()
        {
            var (proc, ws) = CreateProcessor();
            ws.Add(new Picture("pic1", 2, 2));

            string result = proc.Execute("select 1 2");

            Assert.StartsWith("Ошибка: Использование:", result);
        }

        // Тест удаления картинки
        [Fact]
        public void Remove_DeletesPicture()
        {
            var (proc, ws) = CreateProcessor();
            ws.Add(new Picture("pic1", 3, 2));
            ws.Add(new Picture("pic2", 4, 5));

            string result = proc.Execute("remove 1");

            Assert.Equal("Удалено: pic1", result);
            Assert.Single(ws.Pictures);
            Assert.Equal("pic2", ws.Pictures[0].Name);
        }

        // Тест удаления текущей картинки
        [Fact]
        public void Remove_CurrentPicture_ShiftsIndex()
        {
            var (proc, ws) = CreateProcessor();
            ws.Add(new Picture("pic1", 2, 2));
            ws.Add(new Picture("pic2", 2, 2));
            ws.Add(new Picture("pic3", 2, 2));
            ws.Select(1); // Текущая - pic2 (индекс 1)

            proc.Execute("remove 2"); // Удаляем pic2

            Assert.Equal(2, ws.Pictures.Count);
            Assert.Equal(1, ws.CurrentIndex); // Индекс остался 1, но теперь там pic3
            Assert.Equal("pic3", ws.Current!.Name);
        }

        // Тест команды info
        [Fact]
        public void Info_ReturnsDetailsOrError()
        {
            var (proc, ws) = CreateProcessor();

            Assert.Equal("Ошибка: Нет открытого изображения.", proc.Execute("info"));

            ws.Add(new Picture("my_pic", 3, 4));
            string result = proc.Execute("info");

            Assert.Equal("Имя: my_pic, размер: 3×4, номер: 1 из 1", result);
        }

        // Тест поворота на 90 градусов
        [Fact]
        public void Rotate_90_Works_AndMovesPixels()
        {
            var (proc, ws) = CreateProcessorWithPicture();

            proc.Execute("rotate 90");

            Assert.Equal(2, ws.Current!.Width); // 3x2 -> 2x3
            Assert.Equal(3, ws.Current!.Height);

            // Пиксель из (0,0) должен переехать в (1,0)
            // Исходный (0,0) был: R=10, G=10, B=10
            Assert.Equal(10, ws.Current.GetPixel(1, 0).R);
            Assert.Equal(10, ws.Current.GetPixel(1, 0).G);
            Assert.Equal(10, ws.Current.GetPixel(1, 0).B);
        }

        // Тест эквивалентности -90 и 270
        [Fact]
        public void Rotate_Minus90_Equals270()
        {
            var (proc1, ws1) = CreateProcessorWithPicture();
            var (proc2, ws2) = CreateProcessorWithPicture();

            proc1.Execute("rotate -90");
            proc2.Execute("rotate 270");

            Assert.Equal(ws2.Current!.Data, ws1.Current!.Data); // Результаты идентичны
        }

        // Тест невалидного rotate
        [Theory]
        [InlineData("rotate 45")]
        [InlineData("rotate abc")]
        [InlineData("rotate")]
        [InlineData("rotate 1 2")]
        public void Rotate_Invalid_ReturnsError(string command)
        {
            var (proc, ws) = CreateProcessorWithPicture();
            var original = ws.Current;

            string result = proc.Execute(command);

            Assert.StartsWith("Ошибка:", result);
            Assert.Same(original, ws.Current);
        }

        // Тест валидной обрезки
        [Fact]
        public void Crop_Valid_Works_AndCopiesCorrectPixels()
        {
            var (proc, ws) = CreateProcessorWithPicture(); // Исходник 3x2

            // Обрезаем область 2x1 начиная с (1, 0)
            string result = proc.Execute("crop 1 0 2 1");

            Assert.Contains("Обрезка 2×1", result);
            Assert.Equal(2, ws.Current!.Width);
            Assert.Equal(1, ws.Current!.Height);

            // В новой картинке (0,0) должен быть цвет исходного (1,0)
            var p = ws.Current.GetPixel(0, 0);
            Assert.Equal(50, p.R); // x=1 => 1*40+10
            Assert.Equal(10, p.G); // y=0 => 0*40+10
        }

        // Тест обрезки последнего пикселя
        [Fact]
        public void Crop_ValidLastPixel_Works()
        {
            var (proc, ws) = CreateProcessorWithPicture(); // 3x2
            // Обрезаем самый последний пиксель (2, 1)
            string result = proc.Execute("crop 2 1 1 1");

            Assert.Equal(1, ws.Current!.Width);
            Assert.Equal(1, ws.Current!.Height);
            Assert.Equal(90, ws.Current.GetPixel(0, 0).R); // x=2 => 2*40+10
        }

        // Тест невалидной обрезки
        [Theory]
        [InlineData("crop 2 0 2 1")]     // За краем
        [InlineData("crop 0 0 0 1")]     // Нулевая ширина
        [InlineData("crop -1 0 1 1")]    // Отрицательный X
        [InlineData("crop 1 2 3")]       // Мало аргументов
        [InlineData("crop 1 2 3 4 5")]   // Много аргументов
        [InlineData("crop a b c d")]     // Не числа
        public void Crop_Invalid_ReturnsError(string command)
        {
            var (proc, ws) = CreateProcessorWithPicture();
            var original = ws.Current;

            string result = proc.Execute(command);

            Assert.StartsWith("Ошибка:", result);
            Assert.Same(original, ws.Current);
        }

        // Тест setpixel с RGB
        [Fact]
        public void SetPixel_RGB_ChangesOnlyTarget()
        {
            var (proc, ws) = CreateProcessorWithPicture();
            var neighborBefore = ws.Current!.GetPixel(0, 1); // Соседний (не трогаем)

            proc.Execute("setpixel 1 1 10 20 30");

            var p = ws.Current!.GetPixel(1, 1);
            Assert.Equal(new Pixel(10, 20, 30, 255), p);
            Assert.Equal(neighborBefore, ws.Current!.GetPixel(0, 1)); // Сосед не изменился
        }

        // Тест невалидного setpixel
        [Theory]
        [InlineData("setpixel 1 1 256 0 0")]   // Цвет > 255
        [InlineData("setpixel 1 1 10 20 30 -1")] // Альфа < 0
        [InlineData("setpixel 1 1 10 20 30 256")] // Альфа > 255
        [InlineData("setpixel 3 0 10 20 30")]  // За границей (x=3)
        [InlineData("setpixel 1 1 a b c")]     // Буквы
        [InlineData("setpixel 1 1 10 20")]     // Мало аргументов
        public void SetPixel_Invalid_ReturnsError(string command)
        {
            var (proc, ws) = CreateProcessorWithPicture();
            var original = ws.Current;

            string result = proc.Execute(command);

            Assert.StartsWith("Ошибка:", result);
            Assert.Same(original, ws.Current);
        }

        // Тест валидной надписи
        [Fact]
        public void Text_Valid_DrawsPixels()
        {
            var (proc, ws) = CreateProcessorWithPicture();

            string result = proc.Execute("text 0 0 0 0 0 1 I");

            Assert.Contains("Надпись", result);
            Assert.DoesNotContain("Ошибка", result);

            // Буква I в первой строке имеет точку на позиции 1: .###.
            Assert.Equal(new Pixel(0, 0, 0, 255), ws.Current!.GetPixel(1, 0));
        }

        // Тест надписи с пробелами
        [Fact]
        public void Text_WithSpaces_PreservesText()
        {
            var (proc, ws) = CreateProcessorWithPicture();

            string result = proc.Execute("text 0 0 0 0 0 1 Hello World");

            Assert.Contains("Hello World", result); // Весь текст сохранился
            Assert.DoesNotContain("Ошибка", result);
        }

        // Тест невалидной надписи
        [Theory]
        [InlineData("text 0 0 0 0 0")]       // Нет текста
        [InlineData("text 0 0 0 0 0 0 I")]   // Масштаб 0
        [InlineData("text 0 0 300 0 0 1 I")] // Цвет > 255
        [InlineData("text -1 0 0 0 0 1 I")]  // Отрицательные координаты
        public void Text_Invalid_ReturnsError(string command)
        {
            var (proc, ws) = CreateProcessorWithPicture();
            var original = ws.Current;

            string result = proc.Execute(command);

            Assert.StartsWith("Ошибка:", result);
            Assert.Same(original, ws.Current); // Картинка не изменилась
        }

        // Тест фильтра grayscale
        [Fact]
        public void Filter_Gray_MakesRgbEqual()
        {
            var (proc, ws) = CreateProcessorWithPicture();
            proc.Execute("filter gray");

            var p = ws.Current!.GetPixel(0, 0);
            Assert.Equal(p.R, p.G);
            Assert.Equal(p.G, p.B);
        }

        // Тест ограничения яркости
        [Fact]
        public void Filter_Brightness_ClampsTo255()
        {
            var (proc, ws) = CreateProcessorWithPicture();
            // Исходный (0,0) = (10, 10, 10). Прибавляем 250 -> должно стать 255, а не 260.
            proc.Execute("filter brightness 250");

            var p = ws.Current!.GetPixel(0, 0);
            Assert.Equal(255, p.R);
            Assert.Equal(255, p.G);
            Assert.Equal(255, p.B);
        }

        // Тест фильтра channels с запятой и точкой
        [Fact]
        public void Filter_Channels_WorksWithCommaAndDot()
        {
            var (proc1, ws1) = CreateProcessorWithPicture();
            var (proc2, ws2) = CreateProcessorWithPicture();

            proc1.Execute("filter channels 0.5 1 1");
            proc2.Execute("filter channels 0,5 1 1"); // Работает благодаря Replace в ParseDouble

            // Исходный красный был 10. 10 * 0.5 = 5.
            Assert.Equal(5, ws1.Current!.GetPixel(0, 0).R);
            Assert.Equal(5, ws2.Current!.GetPixel(0, 0).R);
        }

        // Тест невалидного filter
        [Theory]
        [InlineData("filter invert abc")]         // Лишний аргумент
        [InlineData("filter brightness -300")]    // Ошибка из конструктора BrightnessFilter
        [InlineData("filter channels -1 1 1")]    // Отрицательный множитель
        [InlineData("filter channels NaN 1 1")]   // NaN
        [InlineData("filter channels 1 1")]       // Мало аргументов
        [InlineData("filter unknown")]            // Неизвестный фильтр
        [InlineData("filter")]                    // Нет подкоманды
        public void Filter_Invalid_ReturnsError(string command)
        {
            var (proc, ws) = CreateProcessorWithPicture();
            var original = ws.Current;

            string result = proc.Execute(command);

            Assert.StartsWith("Ошибка:", result);
            Assert.Same(original, ws.Current);
        }

        // Тест операций без картинки
        [Theory]
        [InlineData("rotate 90")]
        [InlineData("crop 0 0 1 1")]
        [InlineData("setpixel 0 0 0 0 0")]
        [InlineData("text 0 0 0 0 0 1 A")]
        [InlineData("filter invert")]
        public void AllOperations_NoPicture_ReturnsError(string command)
        {
            var (proc, _) = CreateProcessor(); // Пустое пространство
            string result = proc.Execute(command);
            Assert.Equal("Ошибка: Нет открытого изображения.", result);
        }

        // Тест создания коллажа
        [Fact]
        public void CollageNew_AddsCollage()
        {
            var (proc, ws) = CreateProcessor();

            string result1 = proc.Execute("collage new c1 5 4 0 0 0");
            Assert.Contains("Создан коллаж: c1 (5×4), номер 1", result1);
            Assert.Single(ws.Collages);

            string result2 = proc.Execute("collage new c2 10 10 0 0 0");
            Assert.Contains("номер 2", result2);
            Assert.Equal(2, ws.Collages.Count);
        }

        // Тест альфа-канала фона коллажа
        [Fact]
        public void CollageNew_BackgroundAlpha()
        {
            var (proc, ws) = CreateProcessor();
            proc.Execute("collage new c 5 4 0 0 0 128");
            Assert.Equal(128, ws.Collages[0].Background.A);
            Assert.StartsWith("Ошибка:", proc.Execute("collage new c2 5 4 0 0 0 256"));
        }

        // Тест невалидного создания коллажа
        [Theory]
        [InlineData("collage new")] // Пустой
        [InlineData("collage new c 5 4 0 0")] // Мало аргументов
        [InlineData("collage new c 5 4 0 0 0 0 0")] // Много аргументов
        [InlineData("collage new c 0 4 0 0 0")] // Нулевая ширина
        [InlineData("collage new c 5 -1 0 0 0")] // Отрицательная высота
        [InlineData("collage new c 5 4 300 0 0")] // Цвет > 255
        [InlineData("collage new c a 4 0 0 0")] // Буквы
        public void CollageNew_Invalid_ReturnsErrorAndKeepsEmpty(string command)
        {
            var (proc, ws) = CreateProcessor();
            string result = proc.Execute(command);
            Assert.StartsWith("Ошибка:", result);
            Assert.Empty(ws.Collages);
        }

        // Тест добавления картинки в коллаж
        [Fact]
        public void CollageAdd_AddsPictureToCollage()
        {
            var (proc, ws) = CreateProcessor();
            var pic1 = new Picture("pic1", 3, 2);
            var pic2 = new Picture("pic2", 3, 2);
            ws.Add(pic1);
            ws.Add(pic2);
            proc.Execute("collage new col 10 10 0 0 0");

            string result = proc.Execute("collage add 1 2 5 5");

            Assert.Contains("Добавлено: pic2 в коллаж col (элементов: 1)", result);
            Assert.Single(ws.Collages[0].Items);
            Assert.Equal(5, ws.Collages[0].Items[0].X);
            Assert.Equal(5, ws.Collages[0].Items[0].Y);
            Assert.Same(pic2, ws.Collages[0].Items[0].Picture); // Картинка из списка не изменилась
        }

        // Тест сохранения снимка в коллаже
        [Fact]
        public void CollageAdd_StoresSnapshot_NotLiveReference()
        {
            var (proc, ws) = CreateProcessor();
            var pic = new Picture("p", 3, 2);
            pic.SetPixel(0, 0, new Pixel(255, 0, 0));
            ws.Add(pic);
            proc.Execute("collage new c 5 4 0 0 0");
            proc.Execute("collage add 1 1 0 0");

            proc.Execute("rotate 90");
            proc.Execute("collage render 1");

            // На холсте должна быть старая версия (не повёрнутая)
            Assert.NotSame(ws.Current, ws.Collages[0].Items[0].Picture);
        }

        // Тест невалидного добавления в коллаж
        [Theory]
        [InlineData("collage add 0 1 0 0")] // Номер коллажа 0
        [InlineData("collage add 2 1 0 0")] // Номер коллажа вне диапазона (всего 1)
        [InlineData("collage add 1 0 0 0")] // Номер картинки 0
        [InlineData("collage add 1 2 0 0")] // Номер картинки вне диапазона (всего 1)
        [InlineData("collage add 1 1 a b")] // Нечисловые координаты
        [InlineData("collage add 1 1 0")]   // Мало аргументов
        [InlineData("collage add 1 1 0 0 0")] // Много аргументов
        public void CollageAdd_Invalid_ReturnsError(string command)
        {
            var (proc, ws) = CreateProcessor();
            // Даем и картинку, и коллаж, чтобы проверять именно валидацию аргументов
            ws.Add(new Picture("pic1", 3, 2));
            proc.Execute("collage new col 10 10 0 0 0");

            string result = proc.Execute(command);

            Assert.StartsWith("Ошибка:", result);
            Assert.Empty(ws.Collages[0].Items);
        }

        // Тест списка коллажей
        [Fact]
        public void CollageList_EmptyAndWithCollages_ReturnsCorrectFormat()
        {
            var (proc, ws) = CreateProcessor();
            Assert.Equal("Нет созданных коллажей.", proc.Execute("collage list"));

            proc.Execute("collage new c1 5 4 0 0 0");
            proc.Execute("collage new c2 10 10 0 0 0");
            ws.Add(new Picture("pic1", 3, 2));
            proc.Execute("collage add 1 1 0 0");

            string result = proc.Execute("collage list");
            Assert.Contains("1. c1 (5×4), элементов: 1", result);
            Assert.Contains("2. c2 (10×10), элементов: 0", result);
        }

        // Тест отрисовки коллажа
        [Fact]
        public void CollageRender_CreatesNewPicture()
        {
            var (proc, ws) = CreateProcessor();
            // Картинка 3x2 с красным пикселем в (0,0) и синим в (1,0)
            var pic = new Picture("pic", 3, 2);
            pic.SetPixel(0, 0, new Pixel(255, 0, 0));
            pic.SetPixel(1, 0, new Pixel(0, 0, 255));
            ws.Add(pic);

            proc.Execute("collage new col 5 4 0 255 0"); // Зеленый фон
            proc.Execute("collage add 1 1 1 1"); // Картинку на координату (1,1)

            string result = proc.Execute("collage render 1");

            Assert.Contains("отрисован как картинка номер 2", result);
            Assert.Equal(2, ws.Pictures.Count); // Стало на одну больше

            var rendered = ws.Current!;
            Assert.Equal(5, rendered.Width);
            Assert.Equal(4, rendered.Height);

            // Внутри вставленной картинки (1,1)
            Assert.Equal(255, rendered.GetPixel(1, 1).R); // Красный
            Assert.Equal(0, rendered.GetPixel(1, 1).G);

            // Снаружи (0,0) должен быть зеленый фон
            Assert.Equal(0, rendered.GetPixel(0, 0).R);
            Assert.Equal(255, rendered.GetPixel(0, 0).G);
        }

        // Тест невалидного индекса отрисовки
        [Fact]
        public void CollageRender_InvalidIndex_ReturnsError()
        {
            var (proc, _) = CreateProcessor();
            Assert.StartsWith("Ошибка:", proc.Execute("collage render 1"));
            Assert.StartsWith("Ошибка:", proc.Execute("collage render abc"));
        }

        // Тест применения операции только к текущей картинке
        [Fact]
        public void ApplyOperation_ChangesOnlyCurrentPicture()
        {
            var (proc, ws) = CreateProcessor();
            var pic1 = new Picture("pic1", 3, 2);
            var pic2 = new Picture("pic2", 4, 4);
            ws.Add(pic1);
            ws.Add(pic2);
            ws.Select(0); // Делаем текущей первую

            proc.Execute("rotate 90");

            Assert.NotSame(pic1, ws.Pictures[0]); // Первая изменилась
            Assert.Same(pic2, ws.Pictures[1]);    // Вторая осталась той же
        }

        // Тест отсутствия исключений на любом вводе
        [Fact]
        public void Execute_NeverThrowsExceptions()
        {
            var (proc, _) = CreateProcessorWithPicture();
            string[] badInputs =
            {
                "open",
                "select x",
                "rotate",
                "\t",
                new string('a', 10000),
                "rotate 99999999999",
                "crop -1 -1 -1 -1",
                "filter",
                "text",
                "setpixel 1 2",
                "filter channels NaN 1 1",
                "collage",
                "collage new",
                "collage new c 536870913 536870913 0 0 0",
                "collage add 1",
                "collage render x"
            };

            foreach (var input in badInputs)
            {
                var ex = Record.Exception(() => proc.Execute(input));
                Assert.Null(ex); // Программа не должна падать ни на одной из этих строк
            }
        }
    }
}