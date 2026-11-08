using System;
using System.IO;
using System.Linq;
using PhotoEditor.Core.Codecs;
using PhotoEditor.Core.Models;
using Xunit;

namespace PhotoEditor.Tests.Codecs
{
    public class BmpCodecTests
    {
        // Создание валидного BMP в виде массива байт
        private byte[] GetValidBmpBytes(Picture? picture = null)
        {
            picture ??= new Picture("test", 2, 2);
            using var stream = new MemoryStream();
            new BmpCodec().Write(picture, stream);
            return stream.ToArray();
        }
        // Тест записи заголовка и проверки длины файла
        [Fact]
        public void Write_WritesHeaderAndPixels_FileLengthIsCorrect()
        {
            var picture = new Picture("t", 3, 2);
            var codec = new BmpCodec();
            using var stream = new MemoryStream();
            codec.Write(picture, stream);
            byte[] bytes = stream.ToArray();

            Assert.Equal(78, bytes.Length);
            Assert.Equal(78, BitConverter.ToInt32(bytes, 2)); // Поле размера файла в заголовке
            Assert.Equal(66, bytes[0]); // 'B'
            Assert.Equal(77, bytes[1]); // 'M'
            Assert.Equal(54, BitConverter.ToInt32(bytes, 10)); // Смещение до пикселей
            Assert.Equal(3, BitConverter.ToInt32(bytes, 18)); // Ширина 
            Assert.Equal(2, BitConverter.ToInt32(bytes, 22)); // Высота 
        }

        // Тест порядка пикселей и выравнивания
        [Fact]
        public void Write_WritesPixelsInCorrectOrderAndPadding()
        {
            var picture = new Picture("t", 2, 2);
            picture.SetPixel(0, 0, new Pixel(255, 0, 0));
            picture.SetPixel(1, 0, new Pixel(0, 255, 0));
            picture.SetPixel(0, 1, new Pixel(0, 0, 255));
            picture.SetPixel(1, 1, new Pixel(255, 255, 255));
            var codec = new BmpCodec();
            using var stream = new MemoryStream();
            codec.Write(picture, stream);

            byte[] bytes = stream.ToArray();
            byte[] actualPixelData = bytes.Skip(54).ToArray();
            byte[] expectedPixelData = new byte[]
            {
                0xFF, 0x00, 0x00,  0xFF, 0xFF, 0xFF,  0x00, 0x00,
                0x00, 0x00, 0xFF,  0x00, 0xFF, 0x00,  0x00, 0x00
            };

            Assert.Equal(expectedPixelData, actualPixelData);
        }

        // Тест расчёта выравнивания строк
        [Theory]
        [InlineData(1, 58)] // rowSize = 4 (3+1), 54 + 4*1
        [InlineData(2, 62)] // rowSize = 8 (6+2), 54 + 8*1
        [InlineData(3, 66)] // rowSize = 12 (9+3), 54 + 12*1
        [InlineData(4, 66)] // rowSize = 12 (12+0), 54 + 12*1
        public void Write_PaddingCalculation_ReturnsCorrectFileLength(int width, int expectedLength)
        {
            var picture = new Picture("t", width, 1);
            var codec = new BmpCodec();
            using var stream = new MemoryStream();
            codec.Write(picture, stream);

            Assert.Equal(expectedLength, stream.ToArray().Length);
        }

        // Тест чтения после записи (восстановление данных)
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void Read_AfterWrite_ReturnsOriginalData(int width)
        {
            var original = new Picture("test", width, 3);
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    original.SetPixel(x, y, new Pixel((byte)(x * 20), (byte)(y * 40), (byte)(x + y)));
                }
            }

            var codec = new BmpCodec();
            using var stream = new MemoryStream();
            codec.Write(original, stream);
            stream.Position = 0;
            var restored = codec.Read(stream, "test");

            Assert.Equal(original.Width, restored.Width);
            Assert.Equal(original.Height, restored.Height);
            Assert.Equal("test", restored.Name);
            Assert.Equal(original.Data, restored.Data);
        }

        // Тест потери альфа-канала при чтении
        [Fact]
        public void Read_LosesAlphaChannel()
        {
            var original = new Picture("test", 1, 1);
            original.SetPixel(0, 0, new Pixel(10, 20, 30, 100)); // Альфа 100
            var codec = new BmpCodec();
            using var stream = new MemoryStream();
            codec.Write(original, stream);
            stream.Position = 0;
            var restored = codec.Read(stream, "test");

            Assert.Equal(255, restored.GetPixel(0, 0).A);
        }

        // Тест чтения эталонных байт
        [Fact]
        public void Read_ReferenceBytes_ReturnsCorrectPixels()
        {
            var original = new Picture("test", 2, 2);
            original.SetPixel(0, 0, new Pixel(255, 0, 0));
            original.SetPixel(1, 0, new Pixel(0, 255, 0));
            original.SetPixel(0, 1, new Pixel(0, 0, 255));
            original.SetPixel(1, 1, new Pixel(255, 255, 255));

            var codec = new BmpCodec();
            using var stream = new MemoryStream();
            codec.Write(original, stream);
            stream.Position = 0;

            var restored = codec.Read(stream, "test");

            Assert.Equal(new Pixel(255, 0, 0), restored.GetPixel(0, 0));
            Assert.Equal(new Pixel(0, 255, 0), restored.GetPixel(1, 0));
            Assert.Equal(new Pixel(0, 0, 255), restored.GetPixel(0, 1));
            Assert.Equal(new Pixel(255, 255, 255), restored.GetPixel(1, 1));
        }

        // Тест неверной сигнатуры
        [Fact]
        public void Read_InvalidSignature_ThrowsInvalidDataException()
        {
            byte[] bytes = GetValidBmpBytes();
            bytes[0] = 0; // Портим 'B'

            Assert.Throws<InvalidDataException>(() => new BmpCodec().Read(new MemoryStream(bytes), "test"));
        }

        // Тест 32-битного BMP
        [Fact]
        public void Read_32BitBmp_ThrowsNotSupportedException()
        {
            byte[] bytes = GetValidBmpBytes();
            bytes[28] = 32; // Ставим 32 бита на пиксель

            Assert.Throws<NotSupportedException>(() => new BmpCodec().Read(new MemoryStream(bytes), "test"));
        }

        // Тест сжатого BMP
        [Fact]
        public void Read_CompressedBmp_ThrowsNotSupportedException()
        {
            byte[] bytes = GetValidBmpBytes();
            bytes[30] = 1; // Ставим сжатие

            Assert.Throws<NotSupportedException>(() => new BmpCodec().Read(new MemoryStream(bytes), "test"));
        }

        // Тест обрезанного файла
        [Fact]
        public void Read_TruncatedFile_ThrowsEndOfStreamException()
        {
            byte[] bytes = GetValidBmpBytes();
            byte[] truncated = bytes.Take(60).ToArray(); // Обрезаем: заголовок + часть пикселей

            Assert.Throws<EndOfStreamException>(() => new BmpCodec().Read(new MemoryStream(truncated), "test"));
        }

        // Тест нулевой ширины
        [Fact]
        public void Read_ZeroWidth_ThrowsInvalidDataException()
        {
            byte[] bytes = GetValidBmpBytes();
            Array.Copy(BitConverter.GetBytes(0), 0, bytes, 18, 4); // Ширина = 0

            Assert.Throws<InvalidDataException>(() => new BmpCodec().Read(new MemoryStream(bytes), "test"));
        }
        // Тест нулевой высоты
        [Fact]
        public void Read_ZeroHeight_ThrowsInvalidDataException()
        {
            byte[] bytes = GetValidBmpBytes();
            Array.Copy(BitConverter.GetBytes(0), 0, bytes, 22, 4); // Высота = 0

            Assert.Throws<InvalidDataException>(() => new BmpCodec().Read(new MemoryStream(bytes), "test"));
        }

        // Тест чтения BMP сверху вниз
        [Fact]
        public void Read_TopDownBmp_ReadsRowsInCorrectOrder()
        {
            var original = new Picture("test", 1, 2);
            original.SetPixel(0, 0, new Pixel(255, 0, 0)); // Верхняя строка
            original.SetPixel(0, 1, new Pixel(0, 0, 255)); // Нижняя строка
            byte[] bytes = GetValidBmpBytes(original);
            Array.Copy(BitConverter.GetBytes(-2), 0, bytes, 22, 4);
            var restored = new BmpCodec().Read(new MemoryStream(bytes), "test");

            Assert.Equal(new Pixel(0, 0, 255), restored.GetPixel(0, 0)); // Синий переехал наверх
            Assert.Equal(new Pixel(255, 0, 0), restored.GetPixel(0, 1)); // Красный уехал вниз
        }
        // Тест сохранения и загрузки для реальных файлов
        [Fact]
        public void SaveAndLoad_WorkWithRealFiles()
        {
            var original = new Picture("test", 2, 2);
            original.SetPixel(0, 0, new Pixel(255, 0, 0));
            var codec = new BmpCodec();
            string tempPath = Path.Combine(Path.GetTempPath(), "test_image.bmp");

            try
            {
                codec.Save(original, tempPath);
                var restored = codec.Load(tempPath);

                Assert.Equal(original.Data, restored.Data);
                Assert.Equal("test_image", restored.Name);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }
    }
}