using System;
using System.IO;
using PhotoEditor.Core.Codecs;
using PhotoEditor.Core.Models;
using Xunit;

namespace PhotoEditor.Tests.Codecs
{
    public class CodecRegistryTests
    {
        // Заглушечный кодек для проверки расширяемости
        private class FakeCodec : IImageCodec
        {
            // Флаг вызова кодека
            public bool WasCalled { get; set; }

            // Проверка поддержки расширения .fake
            public bool CanHandle(string extension)
            {
                return extension.Equals(".fake", StringComparison.OrdinalIgnoreCase);
            }

            // Заглушка загрузки
            public Picture Load(string path)
            {
                WasCalled = true;
                return new Picture("fake", 1, 1);
            }

            // Заглушка сохранения
            public void Save(Picture picture, string path)
            {
                WasCalled = true;
            }
        }

        // Тест сохранения и загрузки BMP
        [Fact]
        public void SaveAndLoad_WithBmp_WorksCorrectly()
        {
            var picture = new Picture("test", 2, 2);
            picture.SetPixel(0, 0, new Pixel(255, 0, 0));
            var registry = new CodecRegistry();
            string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.bmp");

            try
            {
                registry.Save(picture, tempPath);
                var loaded = registry.Load(tempPath);

                Assert.Equal(picture.Width, loaded.Width);
                Assert.Equal(picture.Height, loaded.Height);
                Assert.Equal(picture.Data, loaded.Data);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        // Тест работы с расширением в верхнем регистре
        [Fact]
        public void Load_UppercaseExtension_WorksCorrectly()
        {
            var picture = new Picture("test", 1, 1);
            var registry = new CodecRegistry();

            // Используем .BMP 
            string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.BMP");

            try
            {
                registry.Save(picture, tempPath);
                var loaded = registry.Load(tempPath);
                Assert.Equal(picture.Data, loaded.Data);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        // Тест неподдерживаемого расширения
        [Theory]
        [InlineData("test.png")]
        [InlineData("test")] // Файл без расширения
        public void LoadAndSave_UnsupportedExtension_ThrowsNotSupportedException(string path)
        {
            var registry = new CodecRegistry();
            var picture = new Picture("test", 1, 1);

            Assert.Throws<NotSupportedException>(() => registry.Load(path));
            Assert.Throws<NotSupportedException>(() => registry.Save(picture, path));
        }

        // Тест регистрации и использования кастомного кодека
        [Fact]
        public void Register_CustomCodec_RegistryUsesIt()
        {
            var registry = new CodecRegistry();
            var fakeCodec = new FakeCodec();
            registry.Register(fakeCodec);
            var picture = new Picture("test", 1, 1);

            registry.Load("image.fake");
            Assert.True(fakeCodec.WasCalled, "Реестр не выбрал FakeCodec для Load.");
            
            fakeCodec.WasCalled = false; 
            registry.Save(picture, "image.fake");
            Assert.True(fakeCodec.WasCalled, "Реестр не выбрал FakeCodec для Save.");
        }

        // Тест поиска BMP среди нескольких кодеков
        [Fact]
        public void Registry_MultipleCodecs_StillFindsBmp()
        {
            var registry = new CodecRegistry();
            registry.Register(new FakeCodec());

            Assert.Throws<FileNotFoundException>(() => registry.Load("not_existent.bmp"));
        }

        // Тест невалидного пути
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void LoadSave_InvalidPath_ThrowsArgumentException(string? invalidPath)
        {
            var registry = new CodecRegistry();
            var picture = new Picture("test", 1, 1);

            Assert.Throws<ArgumentException>(() => registry.Load(invalidPath!));
            Assert.Throws<ArgumentException>(() => registry.Save(picture, invalidPath!));
        }

        // Тест регистрации null-кодека
        [Fact]
        public void Register_NullCodec_ThrowsArgumentNullException()
        {
            var registry = new CodecRegistry();
            Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
        }
    }
}