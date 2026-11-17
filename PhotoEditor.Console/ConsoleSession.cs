using System;
using System.Data;
using System.Diagnostics.Tracing;
using System.IO;
using PhotoEditor.Core.Codecs;
using PhotoEditor.Core.State;

namespace PhotoEditor.ConsoleApp
{
    public class ConsoleSession
    {
        // Переименование повреждённого файла состояния
        private static void TryRenameBadFile(string statePath, TextWriter output)
        {
            try
            {
                string badPath = statePath + ".bad";
                if (File.Exists(badPath)) File.Delete(badPath);
                File.Move(statePath, badPath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                output.WriteLine($"Не удалось переименовать повреждённый файл: {ex.Message}");
            }
        }

        // Безопасное сохранение состояния
        private static void TrySave(Workspace workspace, string statePath, TextWriter output)
        {
            try
            {
                WorkspaceStorage.Save(workspace, statePath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                output.WriteLine($"Не удалось сохранить состояние: {ex.Message}");
            }
        }

        // Загрузка рабочего пространства из файла
        private static Workspace LoadWorkspace(string statePath, TextWriter output)
        {
            if (!File.Exists(statePath)) return new Workspace();
            try
            {
                Workspace ws = WorkspaceStorage.Load(statePath);
                output.WriteLine($"Состояние восстановлено: картинок {ws.Pictures.Count}, коллажей {ws.Collages.Count}.");
                return ws;
            }   
            catch (InvalidDataException)
            {
                // Файл битый — переименовываем, чтобы потом не затереть его пустым сохранением
                output.WriteLine("Файл состояния повреждён, начато с пустого пространства.");
                TryRenameBadFile(statePath, output);
                return new Workspace();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                output.WriteLine("Файл состояния повреждён, начато с пустого пространства.");
                return new Workspace();
            }
        }
        
        // Запуск сессии
        public void Run(TextReader input, TextWriter output, string statePath)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (output is null) throw new ArgumentNullException(nameof(output));
            if (statePath is null) throw new ArgumentNullException(nameof(statePath));
            
            // Загрузка состояния
            Workspace workspace = LoadWorkspace(statePath, output);
            var codecs = new CodecRegistry();
            var processor = new CommandProcessor(workspace, codecs);
            output.WriteLine("Фоторедактор. Введите help для вывода перечня команд.");
            
            // Основной цикл ввода команд
            while (!processor.ShouldExit)
            {
                output.Write("> ");
                string? line = input.ReadLine();
                if (line is null) break;
                string result = processor.Execute(line);
                if (!string.IsNullOrEmpty(result)) output.WriteLine(result);
            }
            
            // Сохранение состояния при выходе
            TrySave(workspace, statePath, output);
        }
    }
}