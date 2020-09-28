using System;
using System.IO;
using TestComponentCSharp;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;
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
        public void TestAsStream()
        {
            using InMemoryRandomAccessStream winrtStream = new InMemoryRandomAccessStream();
            using Stream normalStream = winrtStream.AsStream();
            using var memoryStream = new MemoryStream();
            normalStream.CopyTo(memoryStream);
        }

        async Task InvokeReadToEndAsync()
        {
            StorageFolder localFolder = await StorageFolder.GetFolderFromPathAsync(System.IO.Path.GetTempPath());
            StorageFile file = await localFolder.CreateFileAsync("temp.txt", CreationCollisionOption.ReplaceExisting);
            string text = "";
            using (var reader = new System.IO.StreamReader(await file.OpenStreamForReadAsync(), true))
            {
                text = await reader.ReadToEndAsync();
            }
        }

        [Fact]
        public void TestReadToEndAsync()
        {
            Assert.True(InvokeReadToEndAsync().Wait(1000));
        }

        async Task InvokeStreamWriteAndReadAsync()
        {
            var random = new Random(42);
            byte[] data = new byte[256];
            random.NextBytes(data);

            using var stream = new InMemoryRandomAccessStream().AsStream();
            await stream.WriteAsync(data, 0, data.Length);
            stream.Seek(0, SeekOrigin.Begin);

            byte[] read = new byte[256];
            await stream.ReadAsync(read, 0, read.Length);
            Assert.Equal(read, data);
        }

        [Fact]
        public void TestStreamWriteAndRead()
        {
            Assert.True(InvokeStreamWriteAndReadAsync().Wait(1000));
        }

    }
}
