using PhotoEditor.Core.Models;
using Xunit;

namespace PhotoEditor.Tests.Models;

public class PixelTests
{
    [Fact]
    public void Constructor_FourArguments_SetsAll()
    {
        var pix = new Pixel(128, 255, 62, 135);
        Assert.Equal(128, pix.R);
        Assert.Equal(255, pix.G);
        Assert.Equal(62, pix.B);
        Assert.Equal(135, pix.A);
    }

    [Fact]
    public void Constructor_ThreeArguments_SetsDefaultA()
    {
        var pix = new Pixel(255, 128, 64);
        Assert.Equal(255, pix.A);
    }
}
