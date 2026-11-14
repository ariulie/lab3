using System;
using System.IO;
using System.Text.Json;

namespace PhotoEditor.Core.State
{
    public static class WorkspaceStorage
    {
        // Метод сохранения рабочего пространства в файл
        public static void Save(Workspace workspace, string path)
        {
            if (workspace is null) throw new ArgumentNullException(nameof(workspace), "Рабочее пространство не может быть пустым.");
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Путь к файлу не может быть пустым.", nameof(path));
            
            // Преобразование в DTO
            var dto = WorkspaceMapper.ToDto(workspace);
            var options = new JsonSerializerOptions { WriteIndented = true };
            
            // Создание директории при необходимости
            string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            // Сериализация и запись в файл
            string json = JsonSerializer.Serialize(dto, options);
            File.WriteAllText(path, json);
        }

        // Метод загрузки рабочего пространства из файла
        public static Workspace Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Путь к файлу не может быть пустым.", nameof(path));
            if (!File.Exists(path)) throw new FileNotFoundException("Файл сохранения не найден.", path);
            try
            {
                // Чтение и десериализация JSON
                string jsonString = File.ReadAllText(path);
                WorkspaceDto? dto = JsonSerializer.Deserialize<WorkspaceDto>(jsonString);
                if (dto is null) throw new InvalidDataException("Файл сохранения пуст.");
                
                // Преобразование из DTO
                return WorkspaceMapper.FromDto(dto);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("Ошибка разбора JSON. Файл сохранения поврежден или имеет неверный формат.", ex);
            }
        }
    }
}