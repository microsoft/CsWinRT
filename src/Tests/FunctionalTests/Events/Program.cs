using System;
using System.Collections.Generic;
using System.Linq;
using TestComponentCSharp;

int events_expected = 0;
int events_received = 0;

var instance = new Class();

instance.Event0 += () => events_received++;
instance.InvokeEvent0();
events_expected++;

instance.Event1 += (Class sender) =>
{
    events_received++;
};
instance.InvokeEvent1(instance);
events_expected++;

int int0 = 42;
instance.Event2 += (Class sender, int arg0) =>
{
    if (arg0 == int0)
    {
        events_received++;
    }
};
instance.InvokeEvent2(instance, int0);
events_expected++;

var collection0 = new int[] { 42, 1729 };
var collection1 = new Dictionary<int, string> { [1] = "foo", [2] = "bar" };
instance.CollectionEvent += (Class sender, IList<int> arg0, IDictionary<int, string> arg1) =>
{
    if (arg0.SequenceEqual(collection0) && arg1.SequenceEqual(collection1))
    {
        events_received++;
    }
};
instance.InvokeCollectionEvent(instance, collection0, collection1);
events_expected++;

var managedUriHandler = new ManagedUriHandler();
instance.AddUriHandler(managedUriHandler);
bool uriMatches = managedUriHandler.Uri == new Uri("http://github.com");

return events_received == events_expected && uriMatches ? 100 : 101;

partial class ManagedUriHandler : IUriHandler
{
    public Uri Uri { get; private set; }

    public void AddUriHandler(ProvideUri provideUri)
    {
        Uri = provideUri();
    }
}