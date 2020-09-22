using System;
using System.IO;
using TestComponentCSharp;
using Windows.Storage;
using Xunit;

namespace UnitTest
{
    public class TestCSharpNet5
    {
        public Class TestObject { get; private set; }

        public TestCSharpNet5()
        {
            TestObject = new Class();
        }

        [Fact]
        public async void TestIBufferByteAcces()
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            string fileName = "IBufferByteAccessTest.txt";
            Windows.Storage.StorageFile file = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            string text = "";
            using (var reader = new System.IO.StreamReader(await file.OpenStreamForReadAsync(), true))
            {
                try
                {
                    text = await reader.ReadToEndAsync();
                }
                catch (InvalidCastException)
                {
                    Assert.True(false);
                }

                Assert.True(true);
            }
        }
    }
}
