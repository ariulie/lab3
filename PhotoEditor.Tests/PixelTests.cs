using PhotoEditor.Core.Models;
using Xunit;

namespace PhotoEditor.Tests.Models;

public class PixelTests
{
    // Тест конструктора с четырьмя аргументами
    [Fact]
    public void Constructor_FourArguments_SetsAll()
    {
        var pix = new Pixel(128, 255, 62, 135);
        Assert.Equal(128, pix.R);
        Assert.Equal(255, pix.G);
        Assert.Equal(62, pix.B);
        Assert.Equal(135, pix.A);
    }

    // Тест конструктора с тремя аргументами (альфа по умолчанию)
    [Fact]
    public void Constructor_ThreeArguments_SetsDefaultA()
    {
        var pix = new Pixel(255, 128, 64);
        Assert.Equal(255, pix.A);
    }
}