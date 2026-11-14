namespace PhotoEditor.Core.Models
{
    public class Collage
    {
        // Список элементов коллажа
        private readonly List<CollageItem> _items = new List<CollageItem>();
        // Закрытое поле для хранения названия коллажа
        private string _name = "";

        // Свойство для получения списка элементов
        public IReadOnlyList<CollageItem> Items => _items;

        // Свойство для безопасного доступа и изменения названия с проверкой
        public string Name
        {
            get { return _name; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Название не должно быть пустым.", nameof(value));
                _name = value;
            }
        }
        // Свойство ширины
        public int Width { get; }
        // Свойство высоты
        public int Height { get; }
        // Свойство цвета фона
        public Pixel Background { get; }

        // Конструктор с параметрами
        public Collage(string name, int width, int height, Pixel background)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Ширина должна быть положительной.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Высота должна быть положительной.");
            Name = name;
            Width = width;
            Height = height;
            Background = background;
        }

        // Метод добавления картинки в коллаж
        public void Add(Picture picture, int x, int y)
        {
            CollageItem item = new CollageItem(picture, x, y);
            _items.Add(item);
        }

        // Метод отрисовки коллажа
        public Picture Render()
        {
            // Создание шаблона готовой картинки
            var result = new Picture(Name, Width, Height);

            // Заполнение фона
            for (int i = 0; i < result.Data.Length; i += 4)
            {
                result.Data[i] = Background.R;
                result.Data[i + 1] = Background.G;
                result.Data[i + 2] = Background.B;
                result.Data[i + 3] = Background.A;
            }

            // Наложение элементов на фон
            foreach (CollageItem item in _items)
            {
                Picture pic = item.Picture;
                for (int i = 0; i < pic.Width; i++)
                {
                    for (int j = 0; j < pic.Height; j++)
                    {
                        if (!result.InBounds(item.X + i, item.Y + j)) continue;
                        result.SetPixel(item.X + i, item.Y + j, pic.GetPixel(i, j));
                    }
                }
            }
            return result;
        }
    }
}