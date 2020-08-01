using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Xunit;
using WinRT;

using WFC = Windows.Foundation.Collections;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Media3D;


using TestComponentCSharp;

namespace WinRT
{
    public static class Module
    {
        // For HostTest: provide pass-through of activation factory
        public static IntPtr GetActivationFactory(IntPtr hstrRuntimeClassId)
        {
            return IntPtr.Zero;
        }
    }
}


namespace UnitTest
{
    using A = IEnumerable<IStringable>;
    using B = KeyValuePair<string, IAsyncOperationWithProgress</*A*/IEnumerable<IStringable>, float>>;

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
            Func<IntPtr, IntPtr> del = WinRT.Module.GetActivationFactory;
            var name = del.GetType().Name;

            //System.Func`2[[System.IntPtr, System.Private.CoreLib, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = 7cec85d7bea7798e],[System.IntPtr, System.Private.CoreLib, Version= 4.0.0.0, Culture = neutral, PublicKeyToken = 7cec85d7bea7798e]]
            //System.Func`2[[System.IntPtr],[System.IntPtr]]
            var t = Type.GetType("System.Func`2[[System.IntPtr],[System.IntPtr]]");


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
            AssertGuid<IEnumerable<A>>("96565eb9-a692-59c8-bcb5-647cde4e6c4d");
            AssertGuid<IEnumerator<A>>("3c9b1e27-8357-590b-8828-6e917f172390");
            AssertGuid<KeyValuePair<A, B>>("89336cd9-8b66-50a7-9759-eb88ccb2e1fe");
            AssertGuid<WFC.IMapChangedEventArgs<A>>("e1aa5138-12bd-51a1-8558-698dfd070abe");
            AssertGuid<IReadOnlyDictionary<A, B>>("b78f0653-fa89-59cf-ba95-726938aae666");
            AssertGuid<IDictionary<A, B>>("9962cd50-09d5-5c46-b1e1-3c679c1c8fae");
            AssertGuid<WFC.IObservableMap<A, B>>("75f99e2a-137e-537e-a5b1-0b5a6245fc02");
            AssertGuid<WFC.IObservableVector<A>>("d24c289f-2341-5128-aaa1-292dd0dc1950");
            AssertGuid<IReadOnlyList<A>>("5f07498b-8e14-556e-9d2e-2e98d5615da9");
            AssertGuid<IList<A>>("0e3f106f-a266-50a1-8043-c90fcf3844f6");
            AssertGuid<WFC.MapChangedEventHandler<A, B>>("19046f0b-cf81-5dec-bbb2-7cc250da8b8b");
            AssertGuid<WFC.VectorChangedEventHandler<A>>("a1e9acd7-e4df-5a79-aefa-de07934ab0fb");

            // Bindable GUIDs
            AssertGuid<IEnumerable>("036d2c08-df29-41af-8aa2-d774be62ba6f"); // IBindableIterable
            AssertGuid<IList>("393de7de-6fd0-4c0d-bb71-47244a113e93"); // IBindableVector

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
            AssertGuid<Color?>("ab8e5d11-b0c1-5a21-95ae-f16bf3a37624");
            AssertGuid<CornerRadius?>("13e1d34b-5909-579e-9df0-b0d002d32def");
            AssertGuid<Duration?>("031db157-bb1c-5f3b-9bd1-86b518e41838");
            AssertGuid<GridLength?>("42caaa58-4cc0-51c0-8bb5-e03d1840af39");
            AssertGuid<Thickness?>("62390189-15ec-5fd5-86d5-0d5321feb589");
            AssertGuid<GeneratorPosition?>("9af7b86e-f961-5729-8bc3-8a049a324d78");
            AssertGuid<Matrix?>("9633fa73-15ed-5161-b49f-5303116e3417");
            AssertGuid<KeyTime?>("3272a278-58ab-5647-8b95-77ad17795919");
            AssertGuid<RepeatBehavior?>("ac730575-6aa4-5827-8ce5-c3298584e7cf");
            AssertGuid<Matrix3D?>("f1ba40c4-c05d-5258-8866-134054cb5c05");
            AssertGuid<Matrix3x2?>("76358cfd-2cbd-525b-a49e-90ee18247b71");
            AssertGuid<Matrix4x4?>("dacbffdc-68ef-5fd0-b657-782d0ac9807e");
            AssertGuid<Plane?>("46d542a1-52f7-58e7-acfc-9a6d364da022");
            AssertGuid<Quaternion?>("b27004bb-c014-5dce-9a21-799c5a3c1461");
            AssertGuid<Vector2?>("48f6a69e-8465-57ae-9400-9764087f65ad");
            AssertGuid<Vector3?>("1ee770ff-c954-59ca-a754-6199a9be282c");
            AssertGuid<Vector4?>("a5e843c9-ed20-5339-8f8d-9fe404cf3654");

            // Enums, structs, IInspectable, classes, and delegates
            AssertGuid<PropertyType?>("ecebde54-fac0-5aeb-9ba9-9e1fe17e31d5");
            AssertGuid<Point?>("84f14c22-a00a-5272-8d3d-82112e66df00");
            AssertGuid<IList<object>>("b32bdca4-5e52-5b27-bc5d-d66a1a268c2a");
            AssertGuid<IList<Uri>>("0d82bd8d-fe62-5d67-a7b9-7886dd75bc4e");
            AssertGuid<IList<AsyncActionCompletedHandler>>("5dafe591-86dc-59aa-bfda-07f5d59fc708");
            AssertGuid<IList<ComposedNonBlittableStruct>>("c8477314-b257-511b-a3a1-9e4eb6385152");
        }
    }
}
