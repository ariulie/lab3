using PhotoEditor.Core.State;
using Xunit;

namespace PhotoEditor.Tests
{
    public static class TestHelpers
    {
        public static void AssertWorkspaceEqual(Workspace expected, Workspace actual)
        {
            Assert.Equal(expected.CurrentIndex, actual.CurrentIndex);
            Assert.Equal(expected.Pictures.Count, actual.Pictures.Count);
            Assert.Equal(expected.Collages.Count, actual.Collages.Count);

            for (int i = 0; i < expected.Pictures.Count; i++)
            {
                Assert.Equal(expected.Pictures[i].Name, actual.Pictures[i].Name);
                Assert.Equal(expected.Pictures[i].Width, actual.Pictures[i].Width);
                Assert.Equal(expected.Pictures[i].Height, actual.Pictures[i].Height);
                Assert.Equal(expected.Pictures[i].Data, actual.Pictures[i].Data);
            }

            for (int i = 0; i < expected.Collages.Count; i++)
            {
                var expCol = expected.Collages[i];
                var actCol = actual.Collages[i];
                
                Assert.Equal(expCol.Name, actCol.Name);
                Assert.Equal(expCol.Width, actCol.Width);
                Assert.Equal(expCol.Height, actCol.Height);
                Assert.Equal(expCol.Background.R, actCol.Background.R);
                Assert.Equal(expCol.Background.G, actCol.Background.G);
                Assert.Equal(expCol.Background.B, actCol.Background.B);
                Assert.Equal(expCol.Background.A, actCol.Background.A);
                
                Assert.Equal(expCol.Items.Count, actCol.Items.Count);
                for (int j = 0; j < expCol.Items.Count; j++)
                {
                    Assert.Equal(expCol.Items[j].X, actCol.Items[j].X);
                    Assert.Equal(expCol.Items[j].Y, actCol.Items[j].Y);
                    Assert.Equal(expCol.Items[j].Picture.Name, actCol.Items[j].Picture.Name);
                    Assert.Equal(expCol.Items[j].Picture.Width, actCol.Items[j].Picture.Width);
                    Assert.Equal(expCol.Items[j].Picture.Height, actCol.Items[j].Picture.Height);
                    Assert.Equal(expCol.Items[j].Picture.Data, actCol.Items[j].Picture.Data);
                }
            }
        }
    }
}