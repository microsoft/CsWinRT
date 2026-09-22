// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Markup;
using WindowsRuntime.InteropServices;

[assembly: WindowsRuntimeNativeExposedType(typeof(DependencyObjectCollection))]

namespace NativeExposedType;

internal static class XamlTests
{
    public static void Run(bool freshOnly)
    {
        using WindowsXamlManager manager = WindowsXamlManager.InitializeForCurrentThread();

        if (freshOnly)
        {
            CheckNativeResource();

            return;
        }

        TextBlock first = new() { Text = "one" };
        TextBlock second = new() { Text = "two" };
        object[] sources =
        [
            new List<DependencyObject> { first, second },
            new DependencyObjectCollection { first, second },
            new DerivedCollection { first, second },
            XamlReader.Load("""
                <DependencyObjectCollection xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                    <TextBlock Text="one"/>
                    <TextBlock Text="two"/>
                </DependencyObjectCollection>
                """)
        ];

        foreach (object source in sources)
        {
            ItemsControl control = new() { ItemsSource = source };
            CheckItems(control);
            control.ItemsSource = null;
            control.ItemsSource = source;
            CheckItems(control);
            control.ItemsSource = null;
            GC.KeepAlive(source);
        }

        CheckNativeResource();
    }

    private static void CheckNativeResource()
    {
        Grid root = (Grid)XamlReader.Load("""
            <Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <Grid.Resources>
                    <DependencyObjectCollection x:Key="Items">
                        <TextBlock Text="one"/>
                        <TextBlock Text="two"/>
                    </DependencyObjectCollection>
                </Grid.Resources>
                <ItemsControl x:Name="Target" ItemsSource="{StaticResource Items}"/>
            </Grid>
            """);
        ItemsControl target = (ItemsControl)root.FindName("Target");
        CheckItems(target);
        target.ItemsSource = null;
    }

    private static void CheckItems(ItemsControl control)
    {
        CollectionTests.Check(control.Items.Count == 2, "XAML did not receive both native collection items.");
        CollectionTests.Check(control.Items[0] is TextBlock { Text: "one" }, "The first XAML collection item is incorrect.");
        CollectionTests.Check(control.Items[1] is TextBlock { Text: "two" }, "The second XAML collection item is incorrect.");
    }

    private sealed class DerivedCollection : DependencyObjectCollection;
}
