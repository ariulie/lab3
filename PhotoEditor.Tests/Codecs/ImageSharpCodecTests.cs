using System;
using System.IO;
using PhotoEditor.Core.Codecs;
using PhotoEditor.Core.Models;
using Xunit;

namespace PhotoEditor.Tests.Codecs
{
    public class ImageSharpCodecTests
    {
        // Создание временного пути с заданным расширением
        private static string TempPath(string extension) =>
            Path.Combine(Path.GetTempPath(), Guid.NewGuid() + extension);

        // Картинка 3×2 с разными цветами и прозрачностью
        private static Picture MakePicture()
        {
            var pic = new Picture("test", 3, 2);
            pic.SetPixel(0, 0, new Pixel(255, 0, 0));
            pic.SetPixel(1, 0, new Pixel(0, 255, 0, 128));
            pic.SetPixel(2, 0, new Pixel(0, 0, 255));
            pic.SetPixel(0, 1, new Pixel(10, 20, 30, 0));
            return pic;
        }

        // Тест поддержки известных расширений
        [Theory]
        [InlineData(".png")]
        [InlineData(".PNG")]
        [InlineData(".jpg")]
        [InlineData(".JPG")]
        [InlineData(".jpeg")]
        public void CanHandle_SupportedExtensions_ReturnsTrue(string extension)
        {
            Assert.True(new ImageSharpCodec().CanHandle(extension));
        }

        // Тест неподдерживаемых расширений
        [Theory]
        [InlineData(".bmp")]
        [InlineData(".gif")]
        [InlineData("")]
        public void CanHandle_OtherExtensions_ReturnsFalse(string extension)
        {
            Assert.False(new ImageSharpCodec().CanHandle(extension));
        }

        // Тест передачи null в CanHandle
        [Fact]
        public void CanHandle_Null_ReturnsFalse()
        {
            Assert.False(new ImageSharpCodec().CanHandle(null!));
        }

        // Тест сохранения и загрузки PNG
        [Fact]
        public void Png_SaveAndLoad_KeepsAllPixelsAndAlpha()
        {
            string path = TempPath(".png");
            try
            {
                var original = MakePicture();
                var codec = new ImageSharpCodec();
                codec.Save(original, path);
                var loaded = codec.Load(path);

                Assert.Equal(3, loaded.Width);
                Assert.Equal(2, loaded.Height);
                Assert.Equal(Path.GetFileNameWithoutExtension(path), loaded.Name);
                Assert.Equal(128, loaded.GetPixel(1, 0).A);
                Assert.Equal(original.GetPixel(0, 0).R, loaded.GetPixel(0, 0).R);
                Assert.Equal(original.GetPixel(2, 0).B, loaded.GetPixel(2, 0).B);
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        // Тест сохранения и загрузки JPEG
        [Theory]
        [InlineData(".jpg")]
        [InlineData(".jpeg")]
        public void Jpeg_SaveAndLoad_KeepsSizeAndApproximateColor(string extension)
        {
            string path = TempPath(extension);
            try
            {
                var original = new Picture("test", 16, 16);
                for (int y = 0; y < 16; y++)
                    for (int x = 0; x < 16; x++)
                        original.SetPixel(x, y, new Pixel(200, 40, 40));

                var codec = new ImageSharpCodec();
                codec.Save(original, path);
                var loaded = codec.Load(path);

                Assert.Equal(16, loaded.Width);
                Assert.Equal(16, loaded.Height);
                var p = loaded.GetPixel(8, 8);
                Assert.InRange(p.R, 190, 210);
                Assert.InRange(p.G, 30, 50);
                Assert.InRange(p.B, 30, 50);
                Assert.Equal(255, p.A);
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        // Тест замены прозрачности на белый фон в JPEG
        [Fact]
        public void Jpeg_Save_TransparentPixelBecomesWhite()
        {
            string path = TempPath(".jpg");
            try
            {
                var pic = new Picture("test", 8, 8);
                for (int y = 0; y < 8; y++)
                    for (int x = 0; x < 8; x++)
                        pic.SetPixel(x, y, new Pixel(0, 0, 0, 0));

                var codec = new ImageSharpCodec();
                codec.Save(pic, path);
                var p = codec.Load(path).GetPixel(4, 4);

                Assert.InRange(p.R, 245, 255);
                Assert.InRange(p.G, 245, 255);
                Assert.InRange(p.B, 245, 255);
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        // Тест загрузки несуществующего файла
        [Fact]
        public void Load_MissingFile_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() =>
                new ImageSharpCodec().Load(TempPath(".png")));
        }

        // Тест загрузки мусорного файла
        [Theory]
        [InlineData(".png")]
        [InlineData(".jpg")]
        public void Load_GarbageFile_ThrowsInvalidDataException(string extension)
        {
            string path = TempPath(extension);
            File.WriteAllText(path, "это не картинка");
            try
            {
                Assert.Throws<InvalidDataException>(() => new ImageSharpCodec().Load(path));
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        // Тест сохранения с неподдерживаемым расширением
        [Fact]
        public void Save_UnsupportedExtension_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() =>
                new ImageSharpCodec().Save(MakePicture(), TempPath(".gif")));
        }

        // Тест невалидных аргументов
        [Fact]
        public void LoadAndSave_InvalidArguments_Throw()
        {
            var codec = new ImageSharpCodec();
            Assert.Throws<ArgumentException>(() => codec.Load(""));
            Assert.Throws<ArgumentException>(() => codec.Load(null!));
            Assert.Throws<ArgumentNullException>(() => codec.Save(null!, "a.png"));
            Assert.Throws<ArgumentException>(() => codec.Save(MakePicture(), " "));
        }

        // Тест работы реестра с PNG и JPG
        [Theory]
        [InlineData(".png")]
        [InlineData(".jpg")]
        public void Registry_SavesAndLoadsPngAndJpg(string extension)
        {
            string path = TempPath(extension);
            try
            {
                var registry = new CodecRegistry();
                registry.Save(new Picture("test", 4, 4), path);
                var loaded = registry.Load(path);
                Assert.Equal(4, loaded.Width);
                Assert.Equal(4, loaded.Height);
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }
    }
}