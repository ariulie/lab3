using System;
using System.IO;
using PhotoEditor.Core.Models;

namespace PhotoEditor.Core.State
{
    public static class WorkspaceMapper
    {
        // Преобразование картинки в DTO
        private static PictureDto MapPicture(Picture pic)
        {
            if (pic is null) throw new ArgumentNullException(nameof(pic), "Изображение не может быть пустым.");
            var picDto = new PictureDto();
            picDto.Name = pic.Name;
            picDto.Width = pic.Width;
            picDto.Height = pic.Height;
            picDto.Data = (byte[])pic.Data.Clone();
            return picDto;
        }

        // Преобразование рабочего пространства в DTO
        public static WorkspaceDto ToDto(Workspace workspace)
        {
            if (workspace is null) throw new ArgumentNullException(nameof(workspace), "Рабочее пространство не может быть пустым.");
            var dto = new WorkspaceDto();
            dto.CurrentIndex = workspace.CurrentIndex;
            // Преобразование картинок
            foreach (var pic in workspace.Pictures)
                dto.Pictures.Add(MapPicture(pic));
            // Преобразование коллажей
            foreach (var collage in workspace.Collages)
            {
                var collageDto = new CollageDto();
                collageDto.Name = collage.Name;
                collageDto.Width = collage.Width;
                collageDto.Height = collage.Height;
                collageDto.BackgroundR = collage.Background.R;
                collageDto.BackgroundG = collage.Background.G;
                collageDto.BackgroundB = collage.Background.B;
                collageDto.BackgroundA = collage.Background.A;
                // Преобразование элементов коллажа
                foreach (var item in collage.Items)
                {
                    var collageItemDto = new CollageItemDto();
                    collageItemDto.Picture = MapPicture(item.Picture);
                    collageItemDto.X = item.X;
                    collageItemDto.Y = item.Y;
                    collageDto.Items.Add(collageItemDto);
                }
                dto.Collages.Add(collageDto);
            }
            return dto;
        }

        // Восстановление рабочего пространства из DTO
        public static Workspace FromDto(WorkspaceDto dto)
        {
            // Проверка входных данных
            if (dto is null) throw new InvalidDataException("Данные для загрузки не могут быть пустыми.");
            if (dto.Pictures is null) throw new InvalidDataException("В файле сохранения отсутствует список картинок.");
            if (dto.Collages is null) throw new InvalidDataException("В файле сохранения отсутствует список коллажей.");
            Workspace workspace = new Workspace();
            // Восстановление картинок
            foreach (var picDto in dto.Pictures)
            {
                if (picDto is null) throw new InvalidDataException("В списке картинок найден пустой элемент.");
                try
                {
                    Picture pic = new Picture(picDto.Name, picDto.Width, picDto.Height, picDto.Data);
                    workspace.Add(pic);
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidDataException("Ошибка при создании изображения из сохранения. Данные повреждены.", ex);

                }
            }
            // Восстановление коллажей
            foreach (var collageDto in dto.Collages)
            {
                if (collageDto is null) throw new InvalidDataException("В списке коллажей найден пустой элемент.");
                try
                {
                    Pixel background = new Pixel(collageDto.BackgroundR, collageDto.BackgroundG, collageDto.BackgroundB, collageDto.BackgroundA);
                    Collage collage = new Collage(collageDto.Name, collageDto.Width, collageDto.Height, background);
                    if (collageDto.Items is null) throw new InvalidDataException("В файле сохранения отсутствует список элементов для одного из коллажей.");
                    // Восстановление элементов коллажа
                    foreach (var item in collageDto.Items)
                    {
                        if (item?.Picture is null) throw new InvalidDataException("В коллаже найден элемент без картинки.");
                        Picture pic = new Picture(item.Picture.Name, item.Picture.Width, item.Picture.Height, item.Picture.Data);
                        collage.Add(pic, item.X, item.Y);
                    }
                    workspace.AddCollage(collage);
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidDataException("Ошибка при создании коллажа из сохранения. Данные повреждены.", ex);
                }
            }
            // Восстановление текущего индекса
            if (workspace.Pictures.Count > 0)
            {
                try
                {
                    workspace.Select(dto.CurrentIndex);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    throw new InvalidDataException($"Некорректный текущий индекс ({dto.CurrentIndex}). Данные повреждены.", ex);
                }
            }
            else
            {
                if (dto.CurrentIndex != -1) throw new InvalidDataException("Список картинок пуст, но текущий индекс не равен -1.");
            }
            return workspace;
        }
    }
}