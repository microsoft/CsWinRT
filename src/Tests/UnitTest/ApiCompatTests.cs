
// Workstation tests require local resources or configuration that may not be available in automation.
// These can be enabled for additional local testing.
// #define ENABLE_WORKSTATION_TESTS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using WinRT;

using WFC = Windows.Foundation.Collections;
using Windows.Foundation;

using Windows.Devices.Geolocation;
using Windows.Globalization;
using Windows.Devices.Enumeration;

using Windows.Data.Json;

using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Storage.FileProperties;

using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;

namespace UnitTest
{
    using A = IEnumerable<IStringable>;
    using B = KeyValuePair<string, IAsyncOperationWithProgress</*A*/IEnumerable<IStringable>, float>>;

    public class TestAPIs
    {

        [Fact]
        public void TestCalendarAPIs()
        {
            List<string> languages = new List<string>();
            languages.Add("en-us");
            Calendar cal = new Calendar(languages);
            Assert.NotEqual("", cal.DayOfWeekAsString());
        }

#if ENABLE_WORKSTATION_TESTS 
        // Even with exception handling fall-backs, this test fails on x64 builds in automation 
        // runs. Leaving disabled for now in automation, root cause unknown.

        // This test fails if geolocation is disabled, as is often the case in automation servers.
        async Task InvokeGeolocation()
        {
            // Actual position doesn't matter (unless you're on a weather bouy in 
            // the Gulf of Guinea. Then this might give a false failure). 
            // This test is just validating the results of calling a simply async OS API.
            try
            {
                Geolocator locator = new Geolocator();
                Geoposition pos = await locator.GetGeopositionAsync();
                Assert.NotEqual(0, pos.Coordinate.Latitude);
                Assert.NotEqual(0, pos.Coordinate.Longitude);
            }
            catch(System.Exception e)
            {
                // Location services are disabled, ignore.
                // This is common on build servers. There are a few different
                // exception types that can be triggered to reach this case 
                // depending on configuration
                UInt32 hr = (UInt32)e.HResult;
                Assert.True(0x80070422 == hr ||
                            0x80070005 == hr);
            }
        }

        [Fact]
        public void TestGeolocation()
        {
            Assert.True(InvokeGeolocation().Wait(1000));
        }

        // This test validates reading device property bag, but requires a USB drive to be connected, so is disabled by default.
        async Task InvokeDeviceEnumeration()
        {
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.PortableStorageDevice);
            Assert.NotEmpty(devices);
            foreach(DeviceInformation device in devices)
            {
                var properties = device.Properties;
                foreach (string key in properties.Keys)
                    Assert.NotNull(key);
            }
        }

        [Fact]
        public void TestDeviceEnumeration()
        {
            Assert.True(InvokeDeviceEnumeration().Wait(5*1000));
        }
    }

        static readonly string dateAccessedProperty = "System.DateAccessed";
        static readonly string fileOwnerProperty = "System.FileOwner";

        async Task ValidateFileIO()
        {
            string processFile = Process.GetCurrentProcess().MainModule.FileName;
            StorageFile file = await StorageFile.GetFileFromPathAsync(processFile);
            IRandomAccessStreamWithContentType stream = await file.OpenReadAsync();

            byte[] read = new byte[4];
            stream.Seek(0x0);
            await stream.ReadAsync(read.AsBuffer(), (uint)read.Length, InputStreamOptions.None);

            // Check the PE signature of the host executable (dotnet.exe).
            Assert.Equal(0x4D, read[0]);
            Assert.Equal(0x5A, read[1]);
            Assert.Equal(0x90, read[2]);
            Assert.Equal(0, read[3]);

            // Get basic properties
            BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
            Assert.NotEqual((ulong)0, basicProperties.Size);

            // Get extra properties
            IDictionary<string, object> extraProperties = await file.Properties.RetrievePropertiesAsync(new[] { dateAccessedProperty, fileOwnerProperty });
            var propValue = extraProperties[dateAccessedProperty];
            Assert.NotNull(propValue);
        }

        [Fact]
        public void TestFileIO()
        {
            Assert.True(ValidateFileIO().Wait(5 * 1000));
        }

        [Fact]
        public void TestJSONAPIs()
        {
            string sampleText = "{\r\n" +
                "  \"id\": \"1146217767\",\r\n" +
                "  \"phone\": null,\r\n" +
                "  \"name\": \"Satya Nadella\",\r\n" +
                "  \"education\": [\r\n" +
                "    {\r\n" +
                "      \"school\": {\r\n" +
                "        \"id\": \"204165836287254\",\r\n" +
                "        \"name\": \"Contoso High School\"\r\n" +
                "      },\r\n" +
                "      \"type\": \"High School\"\r\n" +
                "    },\r\n" +
                "    {\r\n" +
                "      \"school\": {\r\n" +
                "        \"id\": \"116138758396662\",\r\n" +
                "        \"name\": \"Contoso University\"\r\n" +
                "      },\r\n" +
                "      \"type\": \"College\"\r\n" +
                "    }\r\n" +
                "  ],\r\n" +
                "  \"timezone\": -8,\r\n" +
                "  \"verified\": true\r\n" +
                "}";

            JsonObject jsonObject = JsonObject.Parse(sampleText);
            Assert.Equal("1146217767", jsonObject.GetNamedString("id", ""));

            IJsonValue phoneJsonValue = jsonObject.GetNamedValue("phone");
            Assert.NotNull(phoneJsonValue);
            Assert.Equal(JsonValueType.Null, phoneJsonValue.ValueType);
            Assert.True(jsonObject.GetNamedBoolean("verified", false));

            foreach (IJsonValue jsonValue in jsonObject.GetNamedArray("education", new JsonArray()))
            {
                if (jsonValue.ValueType == JsonValueType.Object)
                {
                    Assert.NotEqual("", jsonValue.GetObject().GetNamedString("type"));
                }
            }
        }
#endif

    }
}