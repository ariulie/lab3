using System;
using System.Collections.Generic;
using PhotoEditor.Core.Models;
using PhotoEditor.Core.Operations;

namespace PhotoEditor.Core.State
{
    public class Workspace 
    {
        // Список картинок
        private readonly List<Picture> _pictures = new List<Picture>();
        // Список коллажей
        private readonly List<Collage> _collages = new List<Collage>();

        // Индекс текущей картинки
        public int CurrentIndex { get; private set; } = -1;
        // Текущая картинка
        public Picture? Current => CurrentIndex == -1 ? null : _pictures[CurrentIndex];
        
        // Список картинок только для чтения
        public IReadOnlyList<Picture> Pictures => _pictures;
        // Список коллажей только для чтения
        public IReadOnlyList<Collage> Collages => _collages;

        // Метод добавления картинки
        public void Add(Picture picture)
        {
            if (picture is null) throw new ArgumentNullException(nameof(picture), "Невозможно добавить пустую картинку.");
            
            _pictures.Add(picture);
            CurrentIndex = _pictures.Count - 1;
        }

        // Метод выбора текущей картинки по индексу
        public void Select(int index)
        {
            if (index < 0 || index >= Pictures.Count) throw new ArgumentOutOfRangeException(nameof(index), "Индекс выходит за границы списка.");
            CurrentIndex = index;
        }

        // Метод удаления картинки по индексу
        public void Remove(int index)
        {
            if (index < 0 || index >= Pictures.Count) throw new ArgumentOutOfRangeException(nameof(index), "Индекс выходит за границы списка.");
            _pictures.RemoveAt(index);
            
            // Корректировка текущего индекса после удаления
            if (_pictures.Count == 0) 
                CurrentIndex = -1;
            else if (index < CurrentIndex) 
                CurrentIndex--;
            else if (index == CurrentIndex && CurrentIndex >= _pictures.Count) 
                CurrentIndex = _pictures.Count - 1;
        }

        // Метод применения операции к текущей картинке
        public Picture Apply(IImageOperation operation)
        {
            if (operation is null) throw new ArgumentNullException(nameof(operation), "Операция не указана.");
            if (Current is null) throw new InvalidOperationException("Текущее изображение не задано.");
            Picture result = operation.Apply(Current);
            _pictures[CurrentIndex] = result;
            return result;
        }

        // Метод добавления коллажа
        public void AddCollage(Collage collage)
        {
            if (collage is null) throw new ArgumentNullException(nameof(collage), "Невозможно добавить пустой коллаж.");
            _collages.Add(collage);
        }

        // Метод отрисовки коллажа и добавления результата как картинки
        public void RenderCollage(int index)
        {
            if (index < 0 || index >= Collages.Count) throw new ArgumentOutOfRangeException(nameof(index), "Индекс выходит за границы списка.");
            Picture pic = _collages[index].Render();
            _pictures.Add(pic);
            CurrentIndex = _pictures.Count - 1;
        }
    }
}