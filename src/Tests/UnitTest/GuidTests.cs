using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using WFC = Windows.Foundation.Collections;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Media3D;

using TestComponentCSharp;
using WindowsRuntime.InteropServices;

namespace UnitTest
{
    using A = IEnumerable<IStringable>;
    using B = KeyValuePair<string, IAsyncOperationWithProgress</*A*/IEnumerable<IStringable>, float>>;

    [TestClass]
    public class TestGuids
    {
        /* TODO
        private static void AssertGuid<T>(string expected)
        {
            var actual = GuidGenerator.CreateIID(typeof(T));
            Assert.AreEqual(new Guid(expected), actual);
        }

        [TestMethod]
        public void TestGenerics()
        {
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
            AssertGuid<EventHandler<A, B>>("edb31843-b4cf-56eb-925a-d4d0ce97a08d");

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

            // ... rest unchanged ...
        }
        */
    }
}