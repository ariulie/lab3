using System;
using System.Globalization;
using System.Text;
using PhotoEditor.Core.Operations;
using PhotoEditor.Core.Codecs;
using PhotoEditor.Core.Models;
using PhotoEditor.Core.State;

namespace PhotoEditor.ConsoleApp
{
    public class CommandException : Exception
    {
        // Конструктор с параметрами
        public CommandException(string message) : base(message) { }
    }

    public class CommandProcessor
    {
        // Флаг завершения работы
        public bool ShouldExit { get; private set; }
        // Поле для хранения рабочего пространства
        private readonly Workspace _workspace;
        // Поле для хранения реестра кодеков
        private readonly CodecRegistry _codecs;

        // Конструктор с параметрами
        public CommandProcessor(Workspace workspace, CodecRegistry codecs)
        {
            if (workspace is null) throw new ArgumentNullException(nameof(workspace));
            if (codecs is null) throw new ArgumentNullException(nameof(codecs));
            _workspace = workspace;
            _codecs = codecs;
        }

        // Текст справки
        private const string HelpText = """
            Доступные команды:
              help - показать это сообщение
              exit - сохранить состояние и выйти
              open <путь> - загрузить файл
              save <путь> - записать текущую картинку
              list - список картинок
              select <номер> - выбрать картинку
              remove <номер> - удалить картинку
              info - информация о текущей картинке
              rotate <угол> - поворот
              crop <x> <y> <ширина> <высота> - обрезка
              setpixel <x> <y> <r> <g> <b> [a] - замена пикселя
              text <x> <y> <r> <g> <b> <масштаб> <текст> - надпись
              filter invert/gray/brightness/channels <r> <g> <b> - фильтры
              collage new <имя> <W> <H> <r> <g> <b> [a] (r, g, b, [a] - цвет фона) - создать коллаж
              collage add <№коллажа> <№картинки> <x> <y> - добавить картинку в коллаж
              collage list - список коллажей
              collage render <№> - отрисовать коллаж
            """;

        // Проверка количества аргументов
        private void CheckArgsCount(string[] args, int expectedCount, string usageHint)
        {
            if (args.Length != expectedCount) throw new CommandException($"Использование: {usageHint}");
        }

        // Парсинг целого числа
        private int ParseInt(string token, string paramName)
        {
            if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                throw new CommandException($"{paramName} должен быть целым числом.");
            return value;
        }

        // Парсинг числа с плавающей точкой
        private double ParseDouble(string token, string paramName)
        {
            string normalizedToken = token.Replace(',', '.');
            if (!double.TryParse(normalizedToken, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                throw new CommandException($"{paramName} должен быть числом.");
            if (!double.IsFinite(value))
                throw new CommandException($"{paramName} не может быть NaN или бесконечностью.");
            return value;
        }

        // Парсинг индекса (1-based)
        private int ParseIndex(string token, int totalCount, string emptyMessage)
        {
            if (totalCount == 0)
                throw new CommandException(emptyMessage);
            int number = ParseInt(token, "Номер");
            if (number < 1 || number > totalCount)
                throw new CommandException($"Номер должен быть от 1 до {totalCount}.");
            return number - 1;
        }

        // Парсинг байта (0-255)
        private byte ParseByte(string token, string paramName)
        {
            int value = ParseInt(token, paramName);
            if (value < 0 || value > 255)
                throw new CommandException($"{paramName} должен быть от 0 до 255.");
            return (byte)value;
        }

        // Применение операции к текущей картинке
        private string ApplyOperation(IImageOperation operation)
        {
            if (_workspace.Current is null)
                throw new CommandException("Нет открытого изображения.");
            _workspace.Apply(operation);
            return $"{operation.Name}: готово. Размер: {_workspace.Current.Width}×{_workspace.Current.Height}.";
        }

        // Обработка команды open
        private string HandleOpen(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
                throw new CommandException("Использование: open <путь>");
            string path = args.Trim('"'); // Убираем кавычки, если пользователь ввел "C:\my file.bmp"
            Picture pic = _codecs.Load(path);
            _workspace.Add(pic);
            int number = _workspace.Pictures.Count;
            return $"Открыто: {pic.Name} ({pic.Width}×{pic.Height}), номер {number}.";
        }

        // Обработка команды save
        private string HandleSave(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
                throw new CommandException("Использование: save <путь>");
            if (_workspace.Current is null)
                throw new CommandException("Нет открытого изображения для сохранения.");
            string path = args.Trim('"');
            _codecs.Save(_workspace.Current, path);
            return $"Сохранено: {path}";
        }

        // Обработка команды list
        private string HandleList()
        {
            if (_workspace.Pictures.Count == 0)
                return "Нет открытых изображений.";
            var sb = new StringBuilder();
            for (int i = 0; i < _workspace.Pictures.Count; i++)
            {
                var pic = _workspace.Pictures[i];
                string marker = (i == _workspace.CurrentIndex) ? "*" : " "; // Если это текущая картинка, ставим звездочку, иначе пробел
                sb.AppendLine($"{marker} {i + 1}. {pic.Name} ({pic.Width}×{pic.Height})");
            }
            return sb.ToString().TrimEnd(); // Убираем лишний перенос строки в конце
        }

        // Обработка команды select
        private string HandleSelect(string args)
        {
            string[] parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            CheckArgsCount(parts, 1, "select <номер>");
            int index = ParseIndex(parts[0], _workspace.Pictures.Count, "Нет открытых изображений.");
            _workspace.Select(index);
            return $"Выбрано: {index + 1}. {_workspace.Current!.Name}";
        }

        // Обработка команды remove
        private string HandleRemove(string args)
        {
            string[] parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            CheckArgsCount(parts, 1, "remove <номер>");
            int index = ParseIndex(parts[0], _workspace.Pictures.Count, "Нет открытых изображений.");
            string name = _workspace.Pictures[index].Name;
            _workspace.Remove(index);
            return $"Удалено: {name}";
        }

        // Обработка команды info
        private string HandleInfo()
        {
            if (_workspace.Current is null)
                throw new CommandException("Нет открытого изображения.");
            var pic = _workspace.Current;
            return $"Имя: {pic.Name}, размер: {pic.Width}×{pic.Height}, номер: {_workspace.CurrentIndex + 1} из {_workspace.Pictures.Count}";
        }

        // Обработка команды rotate
        private string HandleRotate(string args)
        {
            string[] parts = args.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            CheckArgsCount(parts, 1, "rotate <угол>");
            int angle = ParseInt(parts[0], "Угол");
            return ApplyOperation(new RotateOperation(angle));
        }

        // Обработка команды crop
        private string HandleCrop(string args)
        {
            string[] parts = args.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            CheckArgsCount(parts, 4, "crop <x> <y> <ширина> <высота>");
            int x = ParseInt(parts[0], "X");
            int y = ParseInt(parts[1], "Y");
            int w = ParseInt(parts[2], "Ширина");
            int h = ParseInt(parts[3], "Высота");
            return ApplyOperation(new CropOperation(x, y, w, h));
        }

        // Обработка команды setpixel
        private string HandleSetPixel(string args)
        {
            string[] parts = args.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5 && parts.Length != 6)
                throw new CommandException("Использование: setpixel <x> <y> <r> <g> <b> [a]");
            int x = ParseInt(parts[0], "X");
            int y = ParseInt(parts[1], "Y");
            byte r = ParseByte(parts[2], "R");
            byte g = ParseByte(parts[3], "G");
            byte b = ParseByte(parts[4], "B");
            byte a = parts.Length == 6 ? ParseByte(parts[5], "A") : (byte)255;
            Pixel pixel = new Pixel(r, g, b, a);
            return ApplyOperation(new SetPixelOperation(x, y, pixel));
        }

        // Обработка команды text
        private string HandleText(string args)
        {
            // Ограничиваем Split семью частями, чтобы текст с пробелами остался одним куском
            string[] parts = args.Split(new[] { ' ', '\t' }, 7, StringSplitOptions.RemoveEmptyEntries);
            CheckArgsCount(parts, 7, "text <x> <y> <r> <g> <b> <масштаб> <текст>");
            int x = ParseInt(parts[0], "X");
            int y = ParseInt(parts[1], "Y");
            byte r = ParseByte(parts[2], "R");
            byte g = ParseByte(parts[3], "G");
            byte b = ParseByte(parts[4], "B");
            Pixel pixel = new Pixel(r, g, b);
            int scale = ParseInt(parts[5], "Масштаб");
            string text = parts[6]; // Весь оставшийся текст
            return ApplyOperation(new TextOperation(text, x, y, pixel, scale));
        }

        // Обработка команды filter
        private string HandleFilter(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
                throw new CommandException("Использование: filter invert | filter gray | filter brightness <N> | filter channels <r> <g> <b>");
            string[] parts = args.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
            string type = parts[0].ToLowerInvariant();
            string filterArgs = parts.Length > 1 ? parts[1] : "";
            switch (type)
            {
                case "invert":
                    if (!string.IsNullOrEmpty(filterArgs)) throw new CommandException("Использование: filter invert");
                    return ApplyOperation(new InvertFilter());
                case "gray":
                    if (!string.IsNullOrEmpty(filterArgs)) throw new CommandException("Использование: filter gray");
                    return ApplyOperation(new GrayscaleFilter());
                case "brightness":
                    string[] bArgs = filterArgs.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    CheckArgsCount(bArgs, 1, "filter brightness <N>");
                    int delta = ParseInt(bArgs[0], "Яркость");
                    return ApplyOperation(new BrightnessFilter(delta));
                case "channels":
                    string[] cArgs = filterArgs.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    CheckArgsCount(cArgs, 3, "filter channels <r> <g> <b>");
                    double r = ParseDouble(cArgs[0], "Множитель R");
                    double g = ParseDouble(cArgs[1], "Множитель G");
                    double b = ParseDouble(cArgs[2], "Множитель B");
                    return ApplyOperation(new ChannelFilter(r, g, b));
                default:
                    throw new CommandException("Неизвестный фильтр. Доступно: invert, gray, brightness <N>, channels <r> <g> <b>");
            }
        }

        // Обработка команды collage
        private string HandleCollage(string args)
        {
            string[] parts = args.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                throw new CommandException("Использование: collage new <имя> <W> <H> <r> <g> <b> (r,g,b - цвет фона)| collage add <№> <№> <x> <y> | collage list | collage render <№>");
            string subCommand = parts[0].ToLowerInvariant();
            string subArgs = parts.Length > 1 ? parts[1] : "";
            switch (subCommand)
            {
                case "new": return HandleCollageNew(subArgs);
                case "add": return HandleCollageAdd(subArgs);
                case "list": return HandleCollageList();
                case "render": return HandleCollageRender(subArgs);
                default:
                    throw new CommandException("Неизвестная подкоманда. Использование: collage new | add | list | render");
            }
        }

        // Создание коллажа
        private string HandleCollageNew(string args)
        {
            string[] parts = args.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 6 && parts.Length != 7)
                throw new CommandException("Использование: collage new <имя> <ширина> <высота> <r> <g> <b> [a] (r,g,b - цвет фона)");
            string name = parts[0];
            int width = ParseInt(parts[1], "Ширина");
            int height = ParseInt(parts[2], "Высота");
            byte r = ParseByte(parts[3], "R");
            byte g = ParseByte(parts[4], "G");
            byte b = ParseByte(parts[5], "B");
            byte a = parts.Length == 7 ? ParseByte(parts[6], "A") : (byte)255;
            var collage = new Collage(name, width, height, new Pixel(r, g, b, a));
            _workspace.AddCollage(collage);
            return $"Создан коллаж: {name} ({width}×{height}), номер {_workspace.Collages.Count}.";
        }

        // Добавление картинки в коллаж
        private string HandleCollageAdd(string args)
        {
            string[] parts = args.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            CheckArgsCount(parts, 4, "collage add <№коллажа> <№картинки> <x> <y>");
            int collageIndex = ParseIndex(parts[0], _workspace.Collages.Count, "Нет созданных коллажей.");
            int pictureIndex = ParseIndex(parts[1], _workspace.Pictures.Count, "Нет открытых изображений.");
            int x = ParseInt(parts[2], "X");
            int y = ParseInt(parts[3], "Y");
            Collage collage = _workspace.Collages[collageIndex];
            Picture pic = _workspace.Pictures[pictureIndex];
            collage.Add(pic, x, y);
            return $"Добавлено: {pic.Name} в коллаж {collage.Name} (элементов: {collage.Items.Count}).";
        }

        // Список коллажей
        private string HandleCollageList()
        {
            if (_workspace.Collages.Count == 0)
                return "Нет созданных коллажей.";
            var sb = new StringBuilder();
            for (int i = 0; i < _workspace.Collages.Count; i++)
            {
                var c = _workspace.Collages[i];
                sb.AppendLine($"{i + 1}. {c.Name} ({c.Width}×{c.Height}), элементов: {c.Items.Count}");
            }
            return sb.ToString().TrimEnd();
        }

        // Отрисовка коллажа
        private string HandleCollageRender(string args)
        {
            string[] parts = args.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            CheckArgsCount(parts, 1, "collage render <№коллажа>");
            int index = ParseIndex(parts[0], _workspace.Collages.Count, "Нет созданных коллажей.");
            Collage collage = _workspace.Collages[index];
            _workspace.RenderCollage(index); // Отрисовывает и добавляет как новую картинку
            return $"Коллаж {collage.Name} отрисован как картинка номер {_workspace.Pictures.Count} ({collage.Width}×{collage.Height}).";
        }

        // Диспетчер команд
        private string Dispatch(string line)
        {
            string[] parts = line.Trim().Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
            string command = parts[0].ToLowerInvariant();
            string args = parts.Length > 1 ? parts[1] : "";
            switch (command)
            {
                case "help": return HelpText;
                case "exit":
                    ShouldExit = true;
                    return "Сохранение и выход...";
                case "open": return HandleOpen(args);
                case "save": return HandleSave(args);
                case "list": return HandleList();
                case "select": return HandleSelect(args);
                case "remove": return HandleRemove(args);
                case "info": return HandleInfo();
                case "rotate": return HandleRotate(args);
                case "crop": return HandleCrop(args);
                case "setpixel": return HandleSetPixel(args);
                case "text": return HandleText(args);
                case "filter": return HandleFilter(args);
                case "collage": return HandleCollage(args);
                default:
                    return "Неизвестная команда. Введите help для вывода перечня команд.";
            }
        }

        // Выполнение команды с обработкой ошибок
        public string Execute(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return string.Empty;

            try
            {
                return Dispatch(line.Trim());
            }
            catch (CommandException ex)
            {
                return $"Ошибка: {ex.Message}";
            }
            catch (FileNotFoundException)
            {
                return "Ошибка: Файл не найден.";
            }
            catch (DirectoryNotFoundException)
            {
                return "Ошибка: Папка не найдена.";
            }
            catch (OutOfMemoryException)
            {
                return "Ошибка: Недостаточно памяти для такого размера.";
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException ||
                                       ex is NotSupportedException || ex is InvalidDataException ||
                                       ex is IOException || ex is UnauthorizedAccessException)
            {
                string msg = ex.Message;
                int paramIndex = msg.IndexOf(" (Parameter '");
                if (paramIndex > 0) msg = msg.Substring(0, paramIndex);
                return $"Ошибка: {msg}";
            }
        }
    }
}