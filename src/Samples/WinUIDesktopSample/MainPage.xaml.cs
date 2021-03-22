using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUIDesktopSample
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        int num = 0;

        public MainPage()
        {
            InitializeComponent();

            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        DispatcherTimer timer = new DispatcherTimer();
        private void Timer_Tick(object sender, object e)
        {
            GarbageCollect();
        }

        private void Button1Click(object sender, RoutedEventArgs e)
        {
            var button = new CustomButton();
            if (button != null)
            {
                num++;
            }
        }

        private void Button2Click(object sender, RoutedEventArgs e)
        {
            var button = new CustomButton2();
            var res = button.Tag;
        }


        private void Button3Click(object sender, RoutedEventArgs e)
        {
            var button = new CustomButton3();
            button.Click += (s, e) => button.Content = "Click";
        }

        private void Button4Click(object sender, RoutedEventArgs e)
        {
            var button = new Button();
            if (button != null)
            {
                num++;
            }
        }

        private void Button5Click(object sender, RoutedEventArgs e)
        {
            var button = new Button();
            button.Tag = 42;
        }

        private void Button6Click(object sender, RoutedEventArgs e)
        {
            var button = new Button();
            button.Click += (s, e) => button.Content = "Click";
        }

        private WeakReference baseRef;
        private WeakReference derivedRef;
        private WeakReference gridRef;
        private WeakReference derivedGridRef;
        private List<object> pressure = new List<object>();

        static WeakReference CreateObject(bool withCapture, bool derived)
        {
            var obj = derived ? new DerivedGrid() : new Grid();
            var captured = withCapture ? obj : null;
            obj.SizeChanged +=
                    (object sender, SizeChangedEventArgs e) => Debug.Assert(sender == captured);
            return new WeakReference(obj);
        }

        private void WithoutCapture_Click(object sender, RoutedEventArgs e)
        {
            // Succeeds, as there's no cycle between object and event handler
            var withoutCapture = CreateObject(withCapture: false, derived: false);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Status.Text = withoutCapture.IsAlive ? "Grid leaked" : "Grid collected";
        }

        private void WithCapture_Click(object sender, RoutedEventArgs e)
        {
            // Fails due to cycle between object and event handler (unlike UWP)
            var withCapture = CreateObject(withCapture: true, derived: false);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Status.Text = withCapture.IsAlive ? "Grid leaked" : "Grid collected";
        }

        private void DerivedWithoutCapture_Click(object sender, RoutedEventArgs e)
        {
            // Succeeds, as there's no cycle between object and event handler
            var withoutCapture = CreateObject(withCapture: false, derived: true);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Status.Text = withoutCapture.IsAlive ? "Derived Grid leaked" : "Derived Grid collected";
        }

        private void DerivedWithCapture_Click(object sender, RoutedEventArgs e)
        {
            // Fails due to cycle between object and event handler (unlike UWP)
            var withCapture = CreateObject(withCapture: true, derived: true);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Status.Text = withCapture.IsAlive ? "Derived Grid leaked" : "Derived Grid collected";
        }

        private void Alloc_Click(object sender, RoutedEventArgs e)
        {
            var page = new Page();
            baseRef = new WeakReference(page);
            var derived = new Derived();
            derivedRef = new WeakReference(derived);
            var grid = new Grid();
            // Accessing _defaultLazy.Value will also cause a leak by QI-ing through _inner
            // Attach/detach fix insufficient - need to protect all accesses to _inner via IReferenceTracker
            // Even with backing out 2 AddRefs for _default, grid still leaks with event handler attached
            //var ah = grid.ActualHeight;
            grid.SizeChanged += (object sender, SizeChangedEventArgs e) =>
            {
                // uncomment following line to create a reference cycle between grid and delegate, causing leak
                if (sender == grid)
                    throw new NotImplementedException();
            };
            gridRef = new WeakReference(grid);
            var derivedGrid = new DerivedGrid();
            derivedGridRef = new WeakReference(derivedGrid);

            Status.Text = "(click Check Leaks repeatedly)";
        }

        private void Check_Click(object sender, RoutedEventArgs e)
        {
            pressure.Add(new byte[10_000_000]);
            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            var baseStatus = baseRef.IsAlive ? "Page leaked" : "Page collected";
            var derivedStatus = derivedRef.IsAlive ? "Derived leaked" : "Derived collected";
            var gridStatus = gridRef.IsAlive ? "Grid leaked" : "Grid collected";
            var derivedGridStatus = derivedGridRef.IsAlive ? "DerivedGrid leaked" : "DerivedGrid collected";
            Status.Text = baseStatus + ", " + derivedStatus + ", " + gridStatus + ", " + derivedGridStatus;
        }

        private void GarbageCollect()
        {
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
        }
    }

    public partial class CustomButton : Button
    {
        byte[] bytes = new byte[10_000_000];
    }

    public partial class CustomButton2 : Button
    {
        byte[] bytes = new byte[10_000_000];
    }

    public partial class CustomButton3 : Button
    {
        byte[] bytes = new byte[10_000_000];
    }

    public class DerivedGrid : Grid
    {
        byte[] bytes = new byte[10_000_000];
    };

    public class Derived : Page
    {
        byte[] bytes = new byte[10_000_000];
    };
}
