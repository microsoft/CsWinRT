using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using WinRT;

using WF = Windows.Foundation;
using WFC = Windows.Foundation.Collections;
using Windows.Foundation;
using Windows.Foundation.Collections;

using TestComponentCSharp;

namespace UnitTest
{
    using A = IIterable<IStringable>;
    using B = IKeyValuePair<string, IAsyncOperationWithProgress</*A*/IIterable<IStringable>, float>>;

    public class TestGuids
    {
        private static void AssertGuid<T>(string expected)
        {
            var actual = GuidGenerator.CreateIID(typeof(T));
            Assert.Equal(new Guid(expected), actual);
        }

        [Fact]
        public void TestGenerics()
        {
            // Ensure every generic instance has a unique PIID
            Assert.NotEqual(ABI.Windows.Foundation.Collections.IMap<bool, string>.Vftbl.PIID, ABI.Windows.Foundation.Collections.IMap<string, bool>.Vftbl.PIID);

            AssertGuid<IStringable>("96369f54-8eb6-48f0-abce-c1b211e627c3");

            // Generated Windows.Foundation GUIDs
            AssertGuid<IAsyncActionWithProgress<A>>("dd725452-2da3-5103-9c7d-22ee9bb14ad3");
            AssertGuid<IAsyncOperationWithProgress<A, B>>("94645425-b9e5-5b91-b509-8da4df6a8916");
            AssertGuid<IAsyncOperation<A>>("2bd35ee6-72d9-5c5d-9827-05ebb81487ab");
            AssertGuid<AsyncActionProgressHandler<A>>("c261d8d0-71ba-5f38-a239-872342253a18");
            AssertGuid<AsyncActionWithProgressCompletedHandler<A>>("9a0d211c-0374-5d23-9e15-eaa3570fae63");
            AssertGuid<AsyncOperationCompletedHandler<A>>("9d534225-231f-55e7-a6d0-6c938e2d9160");
            AssertGuid<AsyncOperationProgressHandler<A, B>>("264f1e0c-abe4-590b-9d37-e1cc118ecc75");
            AssertGuid<AsyncOperationWithProgressCompletedHandler<A, B>>("c2d078d8-ac47-55ab-83e8-123b2be5bc5a");
            AssertGuid<EventHandler<A>>("fa0b7d80-7efa-52df-9b69-0574ce57ada4");
            AssertGuid<TypedEventHandler<A, B>>("edb31843-b4cf-56eb-925a-d4d0ce97a08d");

            // Generated Windows.Foundation.Collections GUIDs
            AssertGuid<IIterable<A>>("96565eb9-a692-59c8-bcb5-647cde4e6c4d");
            AssertGuid<IIterator<A>>("3c9b1e27-8357-590b-8828-6e917f172390");
            AssertGuid<IKeyValuePair<A, B>>("89336cd9-8b66-50a7-9759-eb88ccb2e1fe");
            AssertGuid<IMapChangedEventArgs<A>>("e1aa5138-12bd-51a1-8558-698dfd070abe");
            AssertGuid<IMapView<A, B>>("b78f0653-fa89-59cf-ba95-726938aae666");
            AssertGuid<IMap<A, B>>("9962cd50-09d5-5c46-b1e1-3c679c1c8fae");
            AssertGuid<IObservableMap<A, B>>("75f99e2a-137e-537e-a5b1-0b5a6245fc02");
            AssertGuid<IObservableVector<A>>("d24c289f-2341-5128-aaa1-292dd0dc1950");
            AssertGuid<IVectorView<A>>("5f07498b-8e14-556e-9d2e-2e98d5615da9");
            AssertGuid<IVector<A>>("0e3f106f-a266-50a1-8043-c90fcf3844f6");
            AssertGuid<MapChangedEventHandler<A, B>>("19046f0b-cf81-5dec-bbb2-7cc250da8b8b");
            AssertGuid<VectorChangedEventHandler<A>>("a1e9acd7-e4df-5a79-aefa-de07934ab0fb");

            // Generated primitive GUIDs
            AssertGuid<bool?>("3c00fd60-2950-5939-a21a-2d12c5a01b8a");
            AssertGuid<sbyte?>("95500129-fbf6-5afc-89df-70642d741990");
            AssertGuid<short?>("6ec9e41b-6709-5647-9918-a1270110fc4e");
            AssertGuid<int?>("548cefbd-bc8a-5fa0-8df2-957440fc8bf4");
            AssertGuid<long?>("4dda9e24-e69f-5c6a-a0a6-93427365af2a");
            AssertGuid<byte?>("e5198cc8-2873-55f5-b0a1-84ff9e4aad62");
            AssertGuid<ushort?>("5ab7d2c3-6b62-5e71-a4b6-2d49c4f238fd");
            AssertGuid<uint?>("513ef3af-e784-5325-a91e-97c2b8111cf3");
            AssertGuid<ulong?>("6755e376-53bb-568b-a11d-17239868309e");
            AssertGuid<float?>("719cc2ba-3e76-5def-9f1a-38d85a145ea8");
            AssertGuid<double?>("2f2d6c29-5473-5f3e-92e7-96572bb990e2");
            AssertGuid<char?>("fb393ef3-bbac-5bd5-9144-84f23576f415");
            AssertGuid<Guid?>("7d50f649-632c-51f9-849a-ee49428933ea");
            AssertGuid<EventRegistrationToken?>("a9b18291-ce2a-5dae-8a23-b7f7388416db");
            AssertGuid<TimeSpan?>("604d0c4c-91de-5c2a-935f-362f13eaf800");
            AssertGuid<DateTimeOffset?>("5541d8a7-497c-5aa4-86fc-7713adbf2a2c");
            AssertGuid<Point?>("84f14c22-a00a-5272-8d3d-82112e66df00");
            AssertGuid<Rect?>("80423f11-054f-5eac-afd3-63b6ce15e77b");
            AssertGuid<Size?>("61723086-8e53-5276-9f36-2a4bb93e2b75");

            // Enums, structs, IInspectable, classes, and delegates
            AssertGuid<PropertyType?>("ecebde54-fac0-5aeb-9ba9-9e1fe17e31d5");
            AssertGuid<Point?>("84f14c22-a00a-5272-8d3d-82112e66df00");
            AssertGuid<IVector<object>>("b32bdca4-5e52-5b27-bc5d-d66a1a268c2a");
            AssertGuid<IVector<Uri>>("0d82bd8d-fe62-5d67-a7b9-7886dd75bc4e");
            AssertGuid<IVector<AsyncActionCompletedHandler>>("5dafe591-86dc-59aa-bfda-07f5d59fc708");
            AssertGuid<IVector<ComposedNonBlittableStruct>>("c8477314-b257-511b-a3a1-9e4eb6385152");
        }
    }
}
