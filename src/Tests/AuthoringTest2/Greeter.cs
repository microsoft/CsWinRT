using System;
using System.Collections.Generic;
using Windows.Foundation;
using ManagedAbi = ABI.AuthoringTest2;

namespace AuthoringTest2;

public sealed class Greeter : IAdder
{
    private event EventHandler<LocalEventArgs> LocalValueChanged;

    public string Greet(string name)
    {
        return $"Hello, {name}!";
    }

    public int Add(int a, int b)
    {
        return a + b;
    }

    // Generic collection returns; exercise the merged interop's marshalling closure.
    public IList<int> GetNumbers()
    {
        return new List<int> { 1, 2, 3, 5, 8, 13 };
    }

    public IDictionary<string, int> GetCounts()
    {
        return new Dictionary<string, int>
        {
            { "alpha", 1 },
            { "beta", 2 },
            { "gamma", 3 },
        };
    }

    public int RaiseLocalEvent()
    {
        int result = 0;
        EventHandler<LocalEventArgs> handler = (_, args) => result = args.Value;
        LocalValueChanged += handler;

        try
        {
            LocalValueChanged.Invoke(this, new LocalEventArgs { Value = 42 });
        }
        finally
        {
            LocalValueChanged -= handler;
        }

        return result;
    }

    public int ExerciseInternalCollections()
    {
        LocalHelper[] array = { new LocalHelper { Value = 7 } };
        IList<LocalHelper> list = new List<LocalHelper>(array);
        IEnumerable<LocalHelper> enumerable = list;
        IReadOnlyList<LocalHelper> view = new List<LocalHelper>(enumerable);
        IDictionary<string, LocalHelper> dictionary = new Dictionary<string, LocalHelper>
        {
            { "answer", view[0] },
        };
        KeyValuePair<string, LocalHelper> pair = new("answer", dictionary["answer"]);

        int result = array[0].Value + list[0].Value + view[0].Value + dictionary[pair.Key].Value + pair.Value.Value;

        foreach (LocalHelper helper in enumerable)
        {
            result += helper.Value;
        }

        return result;
    }

    public int ExerciseNestedCollections()
    {
        NestedHelper[] array = { new NestedHelper { Value = 7 } };
        IList<NestedHelper> list = new List<NestedHelper>(array);
        IEnumerable<NestedHelper> enumerable = list;
        IReadOnlyList<NestedHelper> view = new List<NestedHelper>(enumerable);
        IDictionary<string, NestedHelper> dictionary = new Dictionary<string, NestedHelper>
        {
            { "answer", view[0] },
        };
        KeyValuePair<string, NestedHelper> pair = new("answer", dictionary["answer"]);

        int result = array[0].Value + list[0].Value + view[0].Value + dictionary[pair.Key].Value + pair.Value.Value;

        foreach (NestedHelper helper in enumerable)
        {
            result += helper.Value;
        }

        return result;
    }

    public int ExerciseAbiCollections()
    {
        IList<ManagedAbi.ManagedHelper> helpers = new List<ManagedAbi.ManagedHelper>
        {
            new ManagedAbi.ManagedHelper { Value = 7 },
        };
        IEnumerable<ManagedAbi.IManagedHelper> enumerable = new ManagedAbi.IManagedHelper[] { helpers[0] };
        IReadOnlyList<ABI.ManagedValue> values = new ABI.ManagedValue[] { new ABI.ManagedValue { Value = 7 } };
        IDictionary<string, ManagedAbi.ManagedCallback> callbacks = new Dictionary<string, ManagedAbi.ManagedCallback>
        {
            { "answer", value => value },
        };
        KeyValuePair<ManagedAbi.ManagedKind, ABI.ManagedValue> pair = new(ManagedAbi.ManagedKind.Seven, values[0]);

        int result = helpers[0].Value + values[0].Value + callbacks["answer"](7) + (int)pair.Key + pair.Value.Value;

        foreach (ManagedAbi.IManagedHelper helper in enumerable)
        {
            result += helper.GetValue();
        }

        return result;
    }

    public IReadOnlyList<object> GetLocalHelpers()
    {
        return new List<LocalStringable> { new LocalStringable() };
    }

    public IEnumerable<object> GetNestedHelpers()
    {
        return new NestedStringable[] { new NestedStringable() };
    }

    public object GetBoxedManagedValue()
    {
        return new ABI.ManagedValue { Value = 42 };
    }

    public IList<Greeter> GetGreeters()
    {
        return new List<Greeter> { this };
    }

    public IReadOnlyList<IAdder> GetAdders()
    {
        return new List<IAdder> { this };
    }

    public IDictionary<string, AuthoredValue> GetAuthoredValues()
    {
        return new Dictionary<string, AuthoredValue>
        {
            { "answer", new AuthoredValue { Value = 42 } },
        };
    }

    public object GetBoxedAuthoredValue()
    {
        return new AuthoredValue { Value = 42 };
    }

    public IList<AuthoredValueKind> GetAuthoredKinds()
    {
        return new List<AuthoredValueKind> { AuthoredValueKind.Answer };
    }

    public IList<AuthoredValueCallback> GetAuthoredCallbacks()
    {
        return new List<AuthoredValueCallback> { value => value * 2 };
    }

    // Public nested types are managed implementation details, not authored WinRT types.
    public sealed class NestedHelper
    {
        public int Value { get; set; }
    }

    public sealed class NestedStringable : IStringable
    {
        public override string ToString()
        {
            return "nested helper";
        }
    }
}
