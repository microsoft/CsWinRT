using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using TestComponentCSharp;
using Windows.Foundation.Collections;

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

instance.StringPropertyChanged += Instance_StringPropertyChanged;
instance.RaiseStringChanged();
instance.RaiseStringChanged();
instance.StringPropertyChanged -= Instance_StringPropertyChanged;
instance.RaiseStringChanged();
events_expected += 2;

instance.EnumStructPropertyChanged += Instance_EnumStructPropertyChanged;
instance.RaiseEnumStructChanged();
events_expected += 1;

instance.Event0 += Instance_Event0;
instance.InvokeEvent0();
instance.Event0 -= Instance_Event0;
instance.InvokeEvent0();
// This event here from before the unsubscribe and
// the lambda still registered at the top which is
// fired twice.
events_expected += 3;

instance.PropertyChangedEventHandler += (object sender, PropertyChangedEventArgs args) =>
{
};

ProvideInt s = () => 4;
instance.ObjectProperty = s;
bool boxedDelegateMatches = ((ProvideInt)instance.ObjectProperty) == s;

TestDelegate t = () => true;
instance.ObjectProperty = t;
boxedDelegateMatches &= ((TestDelegate)instance.ObjectProperty) == t;

System.EventHandler<int> u = (object sender, int args) => { };
instance.ObjectProperty = u;
boxedDelegateMatches &= ((System.EventHandler<int>)instance.ObjectProperty) == u;

System.EventHandler<CancellationToken> v = (object sender, CancellationToken args) => { };
instance.ObjectProperty = v;
boxedDelegateMatches &= ((System.EventHandler<CancellationToken>)instance.ObjectProperty) == v;

TestDelegate[] arr = new TestDelegate[] { t, t };
instance.ObjectProperty = arr;

NotifyCollectionChangedEventHandler n = (object sender, NotifyCollectionChangedEventArgs args) => { };
instance.ObjectProperty = n;

return events_received == events_expected && uriMatches && boxedDelegateMatches ? 100 : 101;

void Instance_Event0()
{
    events_received++;
}

void Instance_EnumStructPropertyChanged(object sender, EnumStruct e)
{
    events_received++;
}

void Instance_StringPropertyChanged(Class sender, string args)
{
    events_received++;
}

partial class ManagedUriHandler : IUriHandler
{
    public Uri Uri { get; private set; }

    public void AddUriHandler(ProvideUri provideUri)
    {
        Uri = provideUri();
    }
}

partial class ObservableDictionaryChangedEventArgs : IMapChangedEventArgs<string>
{
    public ObservableDictionaryChangedEventArgs(CollectionChange change, string key)
    {
        CollectionChange = change;
        Key = key;
    }

    public CollectionChange CollectionChange { get; private set; }
    public string Key { get; private set; }
}

delegate bool TestDelegate();