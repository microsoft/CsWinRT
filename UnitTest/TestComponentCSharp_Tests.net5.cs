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

        [Fact]
        public void TestReadToEndAsync()
        {
            var folderTask = StorageFolder.GetFolderFromPathAsync(System.IO.Path.GetTempPath()).AsTask();
            Assert.True(folderTask.Wait(1000));
            StorageFolder localFolder = folderTask.Result;
            
            var fileTask = localFolder.CreateFileAsync("temp.txt", CreationCollisionOption.ReplaceExisting).AsTask();
            Assert.True(fileTask.Wait(1000));
            StorageFile file = fileTask.Result;
           
            string text = "";
           
            var openTask = file.OpenStreamForReadAsync();
            Assert.True(openTask.Wait(1000));
            using (var reader = new System.IO.StreamReader(openTask.Result, true))
            {
                var readTask = reader.ReadToEndAsync();
                Assert.True(readTask.Wait(1000));
                text = readTask.Result;
            }
        }

        [Fact]
        public void TestStreamWriteAndRead()
        {
            var random = new Random(42);
            byte[] data = new byte[256];
            random.NextBytes(data);

            using var stream = new InMemoryRandomAccessStream().AsStream();
            var writeTask = stream.WriteAsync(data, 0, data.Length);
            Assert.True(writeTask.Wait(1000));
            stream.Seek(0, SeekOrigin.Begin);

            byte[] read = new byte[256];
            var readTask = stream.ReadAsync(read, 0, read.Length);
            Assert.True(readTask.Wait(1000));
            
            Assert.Equal(read, data);
        }
    }
}
