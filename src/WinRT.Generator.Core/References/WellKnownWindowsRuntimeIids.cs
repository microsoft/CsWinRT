// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.Generator.References;

/// <summary>
/// IIDs of the Windows Runtime interface and delegate definitions that are custom-mapped to, or manually projected
/// as, well-known managed types, keyed by the fully qualified metadata names of those managed types.
/// </summary>
/// <remarks>
/// Lookups only match names, so callers must validate the exact identity of each type before using this table.
/// </remarks>
internal static class WellKnownWindowsRuntimeIids
{
    /// <summary>
    /// Tries to get the IID of a well-known Windows Runtime interface or delegate definition.
    /// </summary>
    /// <param name="name">The fully qualified metadata name of the validated managed type definition.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <param name="iid">The resulting IID, if found.</param>
    /// <returns>Whether <paramref name="iid"/> was retrieved successfully.</returns>
    public static bool TryGetIid(string name, bool useWindowsUIXamlProjections, out Guid iid)
    {
        iid = name switch
        {
            // Windows.Foundation types
            "System.IDisposable" => new Guid("30D5A829-7FA4-4026-83BB-D75BAE4EA99E"),
            "System.Nullable`1" => new Guid("61C17706-2D65-11E0-9AE8-D48564015472"),

            // Windows.Foundation event handlers
            "System.EventHandler" => new Guid("C50898F6-C536-5F47-8583-8B2C2438A13B"),
            "System.EventHandler`1" => new Guid("9DE1C535-6AE1-11E0-84E1-18A905BCC53F"),
            "System.EventHandler`2" => new Guid("9DE1C534-6AE1-11E0-84E1-18A905BCC53F"),

            // Windows.Foundation async interfaces
            "Windows.Foundation.IAsyncOperation`1" => new Guid("9FC2B0BB-E446-44E2-AA61-9CAB8F636AF2"),
            "Windows.Foundation.IAsyncOperationWithProgress`2" => new Guid("B5D036D7-E297-498F-BA60-0289E76E23DD"),
            "Windows.Foundation.IAsyncActionWithProgress`1" => new Guid("1F6DB258-E803-48A1-9546-EB7353398884"),

            // Windows.Foundation async delegates
            "Windows.Foundation.AsyncActionCompletedHandler" => new Guid("A4ED5C81-76C9-40BD-8BE6-B1D90FB20AE7"),
            "Windows.Foundation.AsyncActionProgressHandler`1" => new Guid("6D844858-0CFF-4590-AE89-95A5A5C8B4B8"),
            "Windows.Foundation.AsyncActionWithProgressCompletedHandler`1" => new Guid("9C029F91-CC84-44FD-AC26-0A6C4E555281"),
            "Windows.Foundation.AsyncOperationCompletedHandler`1" => new Guid("FCDCF02C-E5D8-4478-915A-4D90B74B83A5"),
            "Windows.Foundation.AsyncOperationProgressHandler`2" => new Guid("55690902-0AAB-421A-8778-F8CE5026D758"),
            "Windows.Foundation.AsyncOperationWithProgressCompletedHandler`2" => new Guid("E85DF41D-6AA7-46E3-A8E2-F009D840C627"),

            // Windows.Foundation.Collections interfaces
            "System.Collections.Generic.KeyValuePair`2" => new Guid("02B51929-C1C4-4A7E-8940-0312B5C18500"),
            "System.Collections.Generic.IEnumerable`1" => new Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3"),
            "System.Collections.Generic.IEnumerator`1" => new Guid("6A79E863-4300-459A-9966-CBB660963EE1"),
            "System.Collections.Generic.IList`1" => new Guid("913337E9-11A1-4345-A3A2-4E7F956E222D"),
            "System.Collections.Generic.IReadOnlyList`1" => new Guid("BBE1FA4C-B0E3-4583-BAEF-1F1B2E483E56"),
            "System.Collections.Generic.IDictionary`2" => new Guid("3C2925FE-8519-45C1-AA79-197B6718C1C1"),
            "System.Collections.Generic.IReadOnlyDictionary`2" => new Guid("E480CE40-A338-4ADA-ADCF-272272E48CB9"),
            "Windows.Foundation.Collections.IObservableVector`1" => new Guid("5917EB53-50B4-4A0D-B309-65862B3F1DBC"),
            "Windows.Foundation.Collections.IObservableMap`2" => new Guid("65DF2BF5-BF39-41B5-AEBC-5A9D865E472B"),
            "Windows.Foundation.Collections.IVectorChangedEventArgs" => new Guid("575933DF-34FE-4480-AF15-07691F3D5D9B"),
            "Windows.Foundation.Collections.IMapChangedEventArgs`1" => new Guid("9939F4DF-050A-4C0F-AA60-77075F9C4777"),

            // Windows.Foundation.Collections delegates
            "Windows.Foundation.Collections.VectorChangedEventHandler`1" => new Guid("0C051752-9FBF-4C70-AA0C-0E4C82D9A761"),
            "Windows.Foundation.Collections.MapChangedEventHandler`2" => new Guid("179517F3-94EE-41F8-BDDC-768A895544F3"),

            // XAML interfaces (bindable collections)
            "System.Collections.IEnumerable" => new Guid("036D2C08-DF29-41AF-8AA2-D774BE62BA6F"),
            "System.Collections.IEnumerator" => new Guid("6A1D6C07-076D-49F2-8314-F52C9C9A8331"),
            "System.Collections.IList" => new Guid("393DE7DE-6FD0-4C0D-BB71-47244A113E93"),

            // XAML interfaces (data binding and commands)
            "System.ComponentModel.INotifyPropertyChanged" when useWindowsUIXamlProjections => new Guid("CF75D69C-F2F4-486B-B302-BB4C09BAEBFA"),
            "System.ComponentModel.INotifyPropertyChanged" => new Guid("90B17601-B065-586E-83D9-9ADC3A695284"),
            "System.ComponentModel.INotifyDataErrorInfo" => new Guid("0EE6C2CC-273E-567D-BC0A-1DD87EE51EBA"),
            "System.Collections.Specialized.INotifyCollectionChanged" when useWindowsUIXamlProjections => new Guid("28B167D5-1A31-465B-9B25-D5C3AE686C40"),
            "System.Collections.Specialized.INotifyCollectionChanged" => new Guid("530155E1-28A5-5693-87CE-30724D95A06D"),
            "System.Windows.Input.ICommand" => new Guid("E5AF3542-CA67-4081-995B-709DD13792DF"),
            "System.IServiceProvider" => new Guid("68B3A2DF-8173-539F-B524-C8A2348F5AFB"),

            // XAML delegates
            "System.ComponentModel.PropertyChangedEventHandler" when useWindowsUIXamlProjections => new Guid("50F19C16-0A22-4D8E-A089-1EA9951657D2"),
            "System.ComponentModel.PropertyChangedEventHandler" => new Guid("E3DE52F6-1E32-5DA6-BB2D-B5B6096C962D"),
            "System.Collections.Specialized.NotifyCollectionChangedEventHandler" when useWindowsUIXamlProjections => new Guid("CA10B37C-F382-4591-8557-5E24965279B0"),
            "System.Collections.Specialized.NotifyCollectionChangedEventHandler" => new Guid("8B0909DC-2005-5D93-BF8A-725F017BAA8D"),
            _ => Guid.Empty
        };

        return iid != Guid.Empty;
    }
}
