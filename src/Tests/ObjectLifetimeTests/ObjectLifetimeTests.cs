using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using WinRT.Interop;

namespace ObjectLifetimeTests
{
    public class TestException : Exception
    {
        public TestException(string message) : base(message)
        { }
    }

    [TestClass]
    public class ObjectLifetimeTestsRunner
    {
        private AsyncQueue _asyncQueue;
        private Panel mainCanvas;
        private WeakReference _childRef;


        public ObjectLifetimeTestsRunner()
        {
            mainCanvas = ((ObjectLifetimeTests.Lifted.App)Microsoft.UI.Xaml.Application.Current).m_window.LifeTimePage.Root;
            _asyncQueue = new AsyncQueue(((ObjectLifetimeTests.Lifted.App)Microsoft.UI.Xaml.Application.Current).m_window.DispatcherQueue);
        }
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void GC_ForceCollect(int cnt = 1)
        {
            while (cnt-- > 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        int _gcCounter = 0;
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void GC_Collect()
        {
            if (++_gcCounter % 2 == 0)
            {
                var mre = new ManualResetEvent(false);

                Task task = new Task(() =>
                {
                    GC.Collect();
                    mre.Set();
                });

                task.Start();
                mre.WaitOne();
            }
            else
                GC.Collect();

            GC.WaitForPendingFinalizers();
        }


        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        void Verify(bool condition, string message)
        {
            if (!condition)
            {
                Debug.WriteLine($"[Failed] {message}");
                throw new TestException(message);
            }
        }

        //+------------------------------------------------------------------
        //
        //  MakeNull
        //
        //  This is a helper that sets an object variable to null.  We 
        //  separate this out into a separate routine in order to prevent
        //  pre-mature collection. For more information on pre-mature
        //  collection, see the GC.KeepAlive documentation.
        //
        //+------------------------------------------------------------------

        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void MakeNull<T>(ref T o)
            where T : class
        {
            // This call to KeepAlive wouldn't seem to be necessary, because the
            // caller is already making a call to MakeNull, and therefore presumably
            // making the object inelible for collection.  But if this method gets inlined,
            // that might then not prevent collection.
            GC.KeepAlive(o);

            o = null;
        }

        [TestMethod]
        public void TestInitializeWithWindow()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    Windows.Storage.Pickers.FolderPicker folderMenu = new();
                    Microsoft.UI.Xaml.Window testWindow = new();
                    var windowHandle = WindowNative.GetWindowHandle(testWindow);
                    InitializeWithWindow.Initialize(testWindow, windowHandle);
                    Verify(windowHandle != IntPtr.Zero, "Failed to initialize");
                });
        }


        [TestMethod]
        public void TestWindowNative()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    Microsoft.UI.Xaml.Window testWindow = new();
                    var windowHandle = WindowNative.GetWindowHandle(testWindow);
                    Verify(windowHandle != IntPtr.Zero, "Window Handle was null");
                });
        }

        [TestMethod]
        public void BasicTest1()
        {
            //System.Diagnostics.Debugger.Launch();
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    // Get weak references to various parts of the content 
                    Canvas moveCanvas = mainCanvas.FindName("MoveCanvas") as Canvas;
                    _childRef = new WeakReference(moveCanvas);

                    // Move element to a new position.
                    mainCanvas.Children.Remove(moveCanvas);

                    //System.Diagnostics.Debugger.Launch();
                    mainCanvas.Children.Append(moveCanvas);
                })
                .CallFromUIThread(() =>
                {
                    //Force a GC
                    GC_ForceCollect();

                    //System.Diagnostics.Debugger.Launch();
                    Verify(!_childRef.IsAlive, "MoveCanvas is alive");

                    Canvas moveCanvas = mainCanvas.FindName("MoveCanvas") as Canvas;

                    Verify(moveCanvas != null, "MoveCanvas not found after moved");
                });

            _asyncQueue.Run();
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void BasicTest2()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    // Remove GradientStop
                    GradientStop stop1 = mainCanvas.FindName("Stop1") as GradientStop;
                    _childRef = new WeakReference(stop1);

                    // Replace the background with a new brush.
                    SolidColorBrush b = new SolidColorBrush();
                    //this.HostPanel.Background = b;
                })
                .CallFromUIThread(() =>
                {
                    // Force a GC
                    GC_ForceCollect();

                    Verify(!_childRef.IsAlive, "Stop1 is alive");
                });

            _asyncQueue.Run();
        }

        [TestMethod]
        public void BasicTest3()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    // Remove item from ItemsControl.Items.
                    ItemsControl itemsControlLeakTest = mainCanvas.FindName("_itemsControlLeakTest") as ItemsControl;
                    _childRef = new WeakReference(itemsControlLeakTest.Items.GetAt(0));
                    itemsControlLeakTest.Items.RemoveAt(0);
                })
                .CallFromUIThread(() =>
                {
                    // Force a GC
                    GC_ForceCollect();

                    Verify(!_childRef.IsAlive, "ItemsControlLeakTest Rectangle is alive");
                });

            _asyncQueue.Run();
        }


        //
        // Test a custom control removed.
        //
        [TestMethod]
        public void BasicTest4()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    // Remove custom control.
                    MyControl myControl = mainCanvas.FindName("MyControlToRemove") as MyControl;
                    _childRef = new WeakReference(myControl);
                    mainCanvas.Children.Remove(myControl);
                })
                .CallFromUIThread(() =>
                {
                    // Force GC more than once.
                    GC_ForceCollect(2);

                    Verify(!_childRef.IsAlive, "MyControlToRemove is alive");
                });

            _asyncQueue.Run();
        }

        FrameworkElement _parent = null;
        WeakReference _parentRef = null;

        [TestMethod]
        public void BasicTest5()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    StackPanel stackPanel = new StackPanel();
                    _parent = stackPanel;
                    MyControl myControl = new MyControl();
                    stackPanel.SetValue(CycleTestCanvas.DP1Property, myControl);

                    // Get weak references to the managed objects so that we can monitor them.
                    _parentRef = new WeakReference(_parent);
                    _childRef = new WeakReference(myControl);
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(_parentRef.IsAlive, "_parent died");
                    Verify(_childRef.IsAlive, "Custom control on attached DP died");

                    // Verify that when we drop the strong reference on the parent,
                    // both objects can be collected.

                    MakeNull(ref _parent);

                    GC_ForceCollect();

                    Verify(!_parentRef.IsAlive, "_parent is alive");
                    Verify(!_childRef.IsAlive, "Custom control on attached DP is alive");
                });

            _asyncQueue.Run();
        }


        [TestMethod]
        public void BasicTest6()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    StackPanel stackPanel = new StackPanel();
                    _parent = stackPanel;
                    MyControl myControl = new MyControl();
                    BindingOperations.SetBinding(stackPanel, CycleTestCanvas.DP2Property, new Binding() { Source = myControl });

                    // Get weak references to the managed objects so that we can monitor them.
                    _parentRef = new WeakReference(_parent);
                    _childRef = new WeakReference(myControl);
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(_parentRef.IsAlive, "Parent died");
                    Verify(_childRef.IsAlive, "Child died");

                    // Verify that when we drop the strong reference on the parent,
                    // both objects can be collected.

                    MakeNull(ref _parent);

                    GC_ForceCollect();

                    Verify(!_parentRef.IsAlive, "Parent is alive");
                    Verify(!_childRef.IsAlive, "Child is alive");
                });

            _asyncQueue.Run();
        }

        [TestMethod]
        public void BasicTest6b()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    StackPanel stackPanel1 = new StackPanel();
                    _element1 = stackPanel1;

                    StackPanel stackPanel2 = new StackPanel();
                    _element2 = stackPanel2;

                    ClrObject2 _clrObj = new ClrObject2();

                    BindingOperations.SetBinding(stackPanel1, FrameworkElement.TagProperty, new Binding() { Source = _clrObj, Path = new PropertyPath("IntProperty") });
                    BindingOperations.SetBinding(stackPanel2, FrameworkElement.TagProperty, new Binding() { Source = _clrObj, Path = new PropertyPath("IntProperty") });

                    // Get weak references to the managed objects so that we can monitor them.
                    _element1Ref = new WeakReference(_element1);
                    _element2Ref = new WeakReference(_element2);
                    _clrObjRef = new WeakReference(_clrObj);
                })
                .CallFromUIThread(() =>
                {
                    // Verify all objects are still alive.
                    GC_ForceCollect();

                    Verify(_element1Ref.IsAlive, "Element 1 died");
                    Verify(_element2Ref.IsAlive, "Element 2 died");
                    Verify(_clrObjRef.IsAlive, "CLR object died");

                    Verify((int)((FrameworkElement)_element1Ref.Target).Tag == 0, "Element 1 Tag is wrong value");
                    Verify((int)((FrameworkElement)_element2Ref.Target).Tag == 0, "Element 2 Tag is wrong value");

                    // Let go of first element, and force GC.
                    MakeNull(ref _element1);
                })
                .CollectUntilTrue(() => !_element1Ref.IsAlive, "Timed out waiting for element 1 to die")
                .CallFromUIThread(() =>
                {
                    Verify(_element2Ref.IsAlive, "Element 2 died");
                    Verify(_clrObjRef.IsAlive, "CLR object died");

                    // Cause PropertyChanged event.
                    ClrObject2 clrObj = _clrObjRef.Target as ClrObject2;
                    clrObj.IntProperty = 1;

                    // Verify((int)((FrameworkElement)_element1Ref.Target).Tag == 1, "Element 1 Tag is wrong value");
                    Verify((int)((FrameworkElement)_element2Ref.Target).Tag == 1, "Element 2 Tag is wrong value");
                })
                .CallFromUIThread(() =>
                {
                    // Let go of second element, and force GC.
                    MakeNull(ref _element2);
                })
                .CollectUntilTrue(() => !_element2Ref.IsAlive, "Timed out waiting for element 2 to die")
                .CollectUntilTrue(() => !_clrObjRef.IsAlive, "Timed out waiting for CLR object to die");

            _asyncQueue.Run();
        }
        WeakReference _clrObjRef = null;
        WeakReference _element1Ref = null;
        WeakReference _element2Ref = null;
        object _element1 = null;
        object _element2 = null;


        AutoResetEvent loadedSignal = new AutoResetEvent(false);
        AutoResetEvent unloadedSignal = new AutoResetEvent(false);


        [TestMethod]
        public void CycleTest1()
        {
            //Debugger.Launch();
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    StackPanel stackPanel = new StackPanel();
                    _parent = stackPanel;
                    _parentRef = new WeakReference(_parent);

                    // Add parent to tree so we can verify Loaded/Unloaded events.
                    mainCanvas.Children.Append(stackPanel);
                    Verify(mainCanvas.IsLoaded, "mainCanvas isn't loaded");

                    // Add event handlers to child, release child reference.
                    Rectangle rectangle = new Rectangle();
                    _childRef = new WeakReference(rectangle);
                    rectangle.Loaded += (s, a) => loadedSignal.Set();
                    rectangle.Unloaded += (s, a) =>
                    {
                        stackPanel.ToString();
                        unloadedSignal.Set();
                    };
                    stackPanel.Children.Append(rectangle);
                })
                .WaitForHandle(loadedSignal, "waiting for Loaded")
                .CallFromUIThread(() =>
                {
                    // Verify child is collected.
                    GC_ForceCollect();

                    Verify(_parentRef.IsAlive, "Parent died");
                    Verify(!_childRef.IsAlive, "Child is alive");

                    // Remove rooted references to parent and child.  
                    // Parent should remain alive until Unloaded delegate is called.
                    MakeNull(ref _parent);
                    mainCanvas.Children.RemoveAt(mainCanvas.Children.Count - 1);

                    GC_ForceCollect();

                    //parent should be dead
                    Verify(_parentRef.IsAlive, "Parent died");
                    Verify(!_childRef.IsAlive, "Child is alive");
                })
                .WaitForHandle(unloadedSignal, "waiting for Unloaded")
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect(2);

                    Verify(!_parentRef.IsAlive, "Parent is alive");
                    Verify(!_childRef.IsAlive, "Child is alive");
                });

            _asyncQueue.Run();
        }

        //
        // Cycle from StackPanel to custom element to delegate back to StackPanel.
        //

        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest1b()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    StackPanel stackPanel = new StackPanel();
                    _parent = stackPanel;
                    _parentRef = new WeakReference(_parent);

                    // Add parent to tree so we can verify Loaded/Unloaded events.
                    mainCanvas.Children.Append(stackPanel);

                    // Add event handlers to child, release child reference.
                    MyControl child = new MyControl();
                    _childRef = new WeakReference(child);
                    child.Loaded += (s, a) => loadedSignal.Set();
                    child.Unloaded += (s, a) =>
                    {
                        stackPanel.ToString();
                        unloadedSignal.Set();
                    };
                    stackPanel.Children.Append(child);
                })
                .WaitForHandle(loadedSignal, "waiting for Loaded")
                .CallFromUIThread(() =>
                {
                    // Verify child is not collected.
                    GC_ForceCollect();

                    Verify(_parentRef.IsAlive, "Parent died");
                    Verify(_childRef.IsAlive, "Child died");

                    // Remove rooted references to parent and child.  
                    // Parent should remain alive until Unloaded delegate is called.
                    MakeNull(ref _parent);
                    mainCanvas.Children.RemoveAt(mainCanvas.Children.Count - 1);

                    GC_ForceCollect();

                    //parent should be dead 
                    Verify(_parentRef.IsAlive, "Parent died");
                    Verify(_childRef.IsAlive, "Child died");
                })
                .WaitForHandle(unloadedSignal, "waiting for Unloaded")
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect(2);

                    Verify(!_parentRef.IsAlive, "Parent is alive");
                    Verify(!_childRef.IsAlive, "Child is alive");
                });

            _asyncQueue.Run();
        }

        //
        // Cycle from StackPanel to built-in element to delegate on framework event back to StackPanel.
        //


        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest1c()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    StackPanel sp = new StackPanel();
                    mainCanvas.Children.Append(sp);

                    Button button = new Button();
                    button.Click += (s, a) => sp.ToString();
                    sp.Children.Add(button);

                    object obj = new { Value = "foo" };
                    sp.Tag = obj;

                    _element1Ref = new WeakReference(obj);
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(_element1Ref.IsAlive, "Child died");

                    mainCanvas.Children.RemoveAt(mainCanvas.Children.Count - 1);

                    GC_ForceCollect();

                    Verify(!_element1Ref.IsAlive, "Child is alive");
                });

            _asyncQueue.Run();
        }


        //
        // Cycle from StackPanel to built-in element on DataContext back to StackPanel.
        //
        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest2()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    StackPanel stackPanel = new StackPanel();
                    _parent = stackPanel;

                    Rectangle r = new Rectangle();
                    stackPanel.DataContext = r;
                    r.Tag = stackPanel;

                    // Get weak references to the managed objects so that we can monitor them.
                    _parentRef = new WeakReference(_parent);
                    _childRef = new WeakReference(stackPanel.DataContext);
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(_parentRef.IsAlive, "Parent died");

                    // This may seem odd. Starting with Threshold, the DataContext property no longer uses a loopback mechanism to deal with 
                    // cycles. As such, the RCW is actually expected to die, and our WeakReference should reflect that.
                    Verify(!_childRef.IsAlive, "Child RCW leaked.");

                    Rectangle rectangle = ((StackPanel)_parent).DataContext as Rectangle;
                    Verify(rectangle.Tag == _parent, "Child cycle reference does not equal parent");

                    // Verify that when we drop the strong reference on the parent 
                    // both objects can be collected.

                    MakeNull(ref rectangle);
                    MakeNull(ref _parent);
                })
                .CollectUntilTrue(() => !_parentRef.IsAlive, "Timed out waiting for parent to die")
                .CollectUntilTrue(() => !_childRef.IsAlive, "Timed out waiting for child to die");

            _asyncQueue.Run();
        }

        //
        // Cycle from StackPanel to custom object on DataContext back to StackPanel.
        //
        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest2b()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    StackPanel stackPanel = new StackPanel();
                    _parent = stackPanel;

                    stackPanel.DataContext = new { Value = stackPanel };

                    // Get weak references to the managed objects so that we can monitor them.
                    _parentRef = new WeakReference(_parent);
                    _childRef = new WeakReference(stackPanel.DataContext);
                })
                .CallFromUIThread(() =>
                {
                    CompleteCycleTestCommon();
                });

            _asyncQueue.Run();
        }


        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        void CompleteCycleTestCommon()
        {
            GC_ForceCollect();

            Verify(_parentRef.IsAlive, "Parent died");
            Verify(_childRef.IsAlive, "Child died");

            // Verify that when we drop the strong reference on the parent 
            // both objects can be collected.

            MakeNull(ref _parent);

            GC_ForceCollect();

            Verify(!_parentRef.IsAlive, "Parent is alive");
            Verify(!_childRef.IsAlive, "Child is alive");
        }

        //
        // Cycle from StackPanel to built-in element on Binding back to StackPanel.
        //
        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest2c()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    StackPanel stackPanel = new StackPanel();
                    _parent = stackPanel;

                    var rectangle = new ContentControl();
                    rectangle.Tag = stackPanel;
                    BindingOperations.SetBinding(stackPanel, CycleTestCanvas.DP2Property, new Binding() { Source = rectangle });

                    // Get weak references to the managed objects so that we can monitor them.
                    _parentRef = new WeakReference(_parent);
                    _objIWeakRef = new WeakReference(rectangle); //new WDT.TestHost().GetTestReference(r).MakeWeak();
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(_parentRef.IsAlive, "Parent died");

                    // bugbug: Not working with WUX; the Binding isn't keeping its source RCW alive
                    // Verify(_objIWeakRef.IsAlive, "Child died");

                    MakeNull(ref _parent);

                    GC_ForceCollect();

                    Verify(!_parentRef.IsAlive, "Parent is alive");
                    Verify(!_objIWeakRef.IsAlive, "Child is alive");
                });

            _asyncQueue.Run();
        }
        WeakReference _objIWeakRef = null;

        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest3()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    ContentControl cycleCC = new ContentControl();
                    _parent = cycleCC;

                    Rectangle r = new Rectangle();
                    cycleCC.Content = r;
                    r.Tag = cycleCC;

                    _parentRef = new WeakReference(_parent);
                    _childRef = new WeakReference(r);
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(_parentRef.IsAlive, "Parent died");
                    Verify(!_childRef.IsAlive, "Child is alive");

                    Rectangle rectangle = ((ContentControl)_parent).Content as Rectangle;
                    Verify(rectangle.Tag == _parent, "Child cycle reference does not equal parent");

                    // Verify that when we drop the strong reference on the parent 
                    // both objects can be collected.

                    MakeNull(ref rectangle);
                    MakeNull(ref _parent);
                })
                .CollectUntilTrue(() => !_parentRef.IsAlive, "Timed out waiting for parent to die")
                .CollectUntilTrue(() => !_childRef.IsAlive, "Timed out waiting for child to die");

            _asyncQueue.Run();
        }

        //
        // Cycle from ContentControl to custom control back to ContentControl.
        //
        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest3b()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    ContentControl cycleCC = new ContentControl();
                    _parent = cycleCC;

                    MyControl r = new MyControl();
                    cycleCC.Content = r;
                    r.CustomTag = cycleCC;

                    _parentRef = new WeakReference(_parent);
                    _childRef = new WeakReference(r);
                })
                .CallFromUIThread(() =>
                {
                    CompleteCycleTestCommon();
                });

            _asyncQueue.Run();
        }

        //
        // Cycle from ItemsControl to custom control back to ItemsControl.
        //
        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest4()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    ItemsControl cycleIC = new ItemsControl();
                    _parent = cycleIC;

                    MyControl button = new MyControl();
                    button.CustomTag = cycleIC;
                    cycleIC.Items.Append(button);

                    _parentRef = new WeakReference(_parent);
                    _childRef = new WeakReference(button);
                })
                .CallFromUIThread(() =>
                {
                    CompleteCycleTestCommon();
                });

            _asyncQueue.Run();
        }

        //
        // Cycle from built-in parent element to clr object to built-in child element back to parent.
        // 
        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest5()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    ContentControl parent = new ContentControl();
                    _parent = parent;

                    Button child = new Button();
                    parent.Content = new { Value = child };
                    child.Tag = parent;

                    _parentRef = new WeakReference(_parent);
                    _childRef = new WeakReference(child);
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();
                    MakeNull(ref _parent);
                    GC_ForceCollect();

                    Verify(!_parentRef.IsAlive, "Parent is alive");
                    Verify(!_childRef.IsAlive, "Child is alive");
                });

            _asyncQueue.Run();
        }


        //
        // Cycle:
        // StackPanel1.Tag = StackPanel1
        //
        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest6()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    StackPanel stackPanel1 = new StackPanel();
                    _element1 = stackPanel1;

                    stackPanel1.Tag = stackPanel1;

                    _element1Ref = new WeakReference(_element1);
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(_element1Ref.IsAlive, "Element died");

                    MakeNull(ref _element1);

                    GC_ForceCollect();

                    Verify(!_element1Ref.IsAlive, "Element is alive");
                });

            _asyncQueue.Run();
        }

        //
        // Cycle:
        // StackPanel1.Tag = StackPanel2
        // => StackPanel2.Tag = StackPanel1
        //
        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest6b()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    StackPanel stackPanel1 = new StackPanel();
                    _element1 = stackPanel1;

                    StackPanel stackPanel2 = new StackPanel();

                    stackPanel1.Tag = stackPanel2;
                    stackPanel2.Tag = stackPanel1;

                    _element1Ref = new WeakReference(stackPanel1);
                    _objIWeakRef = new WeakReference(stackPanel2); //bugbug: new WDT.TestHost().GetTestReference(stackPanel2).MakeWeak();
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(_element1Ref.IsAlive, "Element 1 died");

                    // bugbug: Verify(_objIWeakRef.IsAlive, "Element 2 died");

                    MakeNull(ref _element1);

                    GC_ForceCollect();

                    Verify(!_element1Ref.IsAlive, "Element 1 is alive");
                    Verify(!_objIWeakRef.IsAlive, "Element 2 is alive");
                });

            _asyncQueue.Run();
        }

        //
        // Cycle:
        // StackPanel1.Tag = ClrObj
        // => ClrObj.MyTag = StackPanel2
        // => StackPanel2.Tag = StackPanel1
        //
        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest6c()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    StackPanel stackPanel1 = new StackPanel();
                    _element1 = stackPanel1;

                    StackPanel stackPanel2 = new StackPanel();

                    ClrObject3 clrObj = new ClrObject3();

                    stackPanel1.Tag = clrObj;
                    clrObj.MyTag = stackPanel2;
                    stackPanel2.Tag = stackPanel1;

                    _element1Ref = new WeakReference(stackPanel1);
                    _element2Ref = new WeakReference(stackPanel2);
                    _clrObjRef = new WeakReference(clrObj);
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(_element1Ref.IsAlive, "Element 1 died");
                    Verify(_element2Ref.IsAlive, "Element 2 died");
                    Verify(_clrObjRef.IsAlive, "ClrObject died");

                    MakeNull(ref _element1);

                    GC_ForceCollect();

                    Verify(!_element1Ref.IsAlive, "Element 1 is alive");
                    Verify(!_element2Ref.IsAlive, "Element 2 is alive");
                    Verify(!_clrObjRef.IsAlive, "ClrObject is alive");
                });

            _asyncQueue.Run();
        }

        //
        // Cycle:
        // StackPanel1.Tag = Binding(ClrObj.MyTag)
        // => ClrObject.MyTag = StackPanel1
        //
        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest7()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    StackPanel stackPanel1 = new StackPanel();
                    _element1 = stackPanel1;

                    ClrObject3 clrObj = new ClrObject3();

                    BindingOperations.SetBinding(stackPanel1, FrameworkElement.TagProperty, new Binding() { Source = clrObj, Path = new PropertyPath("MyTag") });

                    clrObj.MyTag = stackPanel1;

                    _element1Ref = new WeakReference(_element1);
                    _clrObjRef = new WeakReference(clrObj);
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(_element1Ref.IsAlive, "Element died");
                    Verify(_clrObjRef.IsAlive, "ClrObject died");

                    MakeNull(ref _element1);

                    GC_ForceCollect();

                    Verify(!_element1Ref.IsAlive, "Element is alive");
                    Verify(!_clrObjRef.IsAlive, "ClrObject is alive");
                });

            _asyncQueue.Run();
        }



        //
        // Cycle:
        // CustomControl.Content = Button
        // => Button Binding(CustomDependencyObject.AttachedObject = FrameworkElement.Tag)
        //
        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest8()
        {
            WeakReference weakRef1 = null;
            WeakReference weakRef2 = null;

            _asyncQueue
                .CallFromUIThread(() =>
                {
                    // Create cycle.
                    CustomControl parent = new CustomControl();
                    Button child = new Button();
                    parent.Content = child;

                    BindingOperations.SetBinding(
                        child,
                        FrameworkElement.TagProperty,
                        new Binding() { Source = parent, Path = new PropertyPath("Tag") }
                    );

                    mainCanvas.Children.Append(parent);

                    weakRef1 = new WeakReference(parent);
                    weakRef2 = new WeakReference(child);
                })
                .CallFromUIThread(() =>
                {
                    Verify(weakRef1.IsAlive, "parent died");

                    // Verify child is collected.
                    GC_ForceCollect();

                    Verify(weakRef1.IsAlive, "parent died");

                    // Remove references to parent.
                    mainCanvas.Children.RemoveAt(mainCanvas.Children.Count - 1);
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(!weakRef1.IsAlive, "parent is alive");
                    Verify(!weakRef2.IsAlive, "child is alive");
                });

            _asyncQueue.Run();
        }

        //
        // Cycle:
        // Grid
        // => Binding(CustomDependencyObject.AttachedObject = Button.Tag)
        // => Button.Tag = Grid
        //
        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest9()
        {
            WeakReference weakRef1 = null;

            _asyncQueue
                .CallFromUIThread(() =>
                {
                    // Create cycle.
                    Grid grid = new Grid();
                    Button button = new Button();
                    BindingOperations.SetBinding(grid, CustomDependencyObject.AttachedObjectProperty, new Binding() { Source = button, Path = new PropertyPath("Tag") });
                    button.Tag = grid;

                    // Verify collection.
                    weakRef1 = new WeakReference(grid);
                    MakeNull(ref grid);
                    MakeNull(ref button);
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(!weakRef1.IsAlive, "grid is alive");
                });

            _asyncQueue.Run();
        }

        //
        // Cycle:
        // CustomDependencyObject
        // => Binding(CustomDependencyObject.AttachedObject = Button.Tag)
        // => Button.Tag = CustomDependencyObject
        //
        //[Disabled("595913", "CodeDefect")] // resolved Won't Fix
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest9b()
        {
            WeakReference weakRef1 = null;

            _asyncQueue
                .CallFromUIThread(() =>
                {
                    // Create cycle.
                    CustomDependencyObject custom = new CustomDependencyObject();
                    Button button = new Button();
                    BindingOperations.SetBinding(custom, CustomDependencyObject.AttachedObjectProperty, new Binding() { Source = button, Path = new PropertyPath("Tag") });
                    button.Tag = custom;

                    // Verify collection.
                    weakRef1 = new WeakReference(custom);
                    MakeNull(ref custom);
                    MakeNull(ref button);
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(!weakRef1.IsAlive, "custom is alive");
                });

            _asyncQueue.Run();
        }

        //
        // Cycle:
        //   ViewBox => (itself)
        // 
        // This is a special cycle because it's within the core, not in DXaml.
        //

        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void CycleTest10()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    // Create the cycle
                    var viewbox = new Viewbox();
                    viewbox.Child = viewbox;

                    // This shouldn't stack overflow
                    GC_Collect();

                    // Prevent a core native leak check (bug 591953).
                    viewbox.Child = null;
                });

            _asyncQueue.Run();
        }


        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void LeakTest1()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    StackPanel stackPanel = new StackPanel();
                    mainCanvas.Children.Append(stackPanel);

                    Button button = new Button();
                    stackPanel.Children.Add(button);

                    object obj = new { Value = "foo" };
                    button.Tag = obj;

                    _element1Ref = new WeakReference(obj);
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(_element1Ref.IsAlive, "Child died");

                    // Remove child and force collection.
                    // Verify that child is collected.
                    StackPanel sp = mainCanvas.Children[mainCanvas.Children.Count - 1] as StackPanel;
                    sp.Children.RemoveAt(0);
                    MakeNull(ref sp);

                    GC_ForceCollect();

                    Verify(!_element1Ref.IsAlive, "Child is alive");
                });

            _asyncQueue.Run();
        }

        //
        // Verify item removed from FlipView is collected.
        //
        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void LeakTest1b()
        {
            _asyncQueue
                .CallFromUIThread(() =>
                {
                    FlipView flipView = new FlipView();
                    mainCanvas.Children.Append(flipView);

                    // Add an item to the FlipView.  This is a dummy item that we won't use,
                    // but it will trigger selection events in UIA that will hold a reference to the FlipViewItem
                    // and make it impossible to test lifetime.
                    FlipViewItem item = new FlipViewItem();
                    flipView.Items.Add(item);
                    flipView.SelectedIndex = 0;

                    // Now create the item we're actually going to test with.
                    item = new FlipViewItem();
                    flipView.Items.Add(item);

                    object obj = new { Value = "foo" };
                    item.Tag = obj;

                    _element1Ref = new WeakReference(obj);
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(_element1Ref.IsAlive, "Child died");

                    // Remove child and force collection.
                    // Verify that child is collected.
                    // We have to use Clear() rather than RemoveAt(0) to trigger FlipView's AutomationPeer
                    // to release it's caches.
                    FlipView fv = mainCanvas.Children[mainCanvas.Children.Count - 1] as FlipView;
                    fv.Items.Clear();

                    GC_ForceCollect();
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(!_element1Ref.IsAlive, "Child is alive");
                });

            _asyncQueue.Run();
        }

        //
        // Verify delegate on FrameworkElement.Loaded
        //
        // bugbug: [Test]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void LeakTest2()
        {
            StackPanel element = null;
            WeakReference weakRef = null;
            AutoResetEvent loadedSignal = new AutoResetEvent(false);
            RoutedEventHandler handler = new RoutedEventHandler((s, a) =>
            {
                //TestLogger.LogMessage("In Loaded event");
                loadedSignal.Set();
            });

            _asyncQueue
                .CallFromUIThread(() =>
                {
                    // Add handler to element.
                    element = new StackPanel();

                    element.Loaded += handler;
                    mainCanvas.Children.Append(element);

                    weakRef = new WeakReference(handler);
                    handler = null;
                })
                .WaitForHandle(loadedSignal, "loadedSignal")
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(weakRef.IsAlive, "delegate died");

                    // Remove handler.
                    element.Loaded -= weakRef.Target as RoutedEventHandler;
                })
                .CollectUntilTrue(() => !weakRef.IsAlive, "Timed out waiting for delegate to die")
                .CallFromUIThread(() =>
                {
                    // Remove element.
                    mainCanvas.Children.RemoveAt(mainCanvas.Children.Count - 1);
                    MakeNull(ref element);
                });

            _asyncQueue.Run();
        }

        //
        // Verify delegate on static CompositionTarget events.
        //
        // bugbug: [Test]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void LeakTest2b()
        {
            WeakReference weakRef = null;
            AutoResetEvent eventSignal = new AutoResetEvent(false);
            EventHandler<object> handler = new EventHandler<object>((s, a) =>
            {
                //TestLogger.LogMessage("In event handler");
                eventSignal.Set();
            });

            _asyncQueue
                .CallFromUIThread(() =>
                {
                    CompositionTarget.Rendering += handler;
                    CompositionTarget.SurfaceContentsLost += handler;

                    weakRef = new WeakReference(handler);
                    handler = null;
                })
                .WaitForHandle(eventSignal, "waiting for event")
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(weakRef.IsAlive, "Object died");

                    CompositionTarget.Rendering -= weakRef.Target as EventHandler<object>;
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(weakRef.IsAlive, "Object died");

                    CompositionTarget.SurfaceContentsLost -= weakRef.Target as EventHandler<object>;
                })
                .CollectUntilTrue(() => !weakRef.IsAlive, "Timed out waiting for object to die");

            _asyncQueue.Run();
        }



        //
        // Verify LayoutUpdated event (bug 626259)
        //

        // bugbug: [Test]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void LeakTest2c()
        {
            WeakReference weakRef = null;
            StackPanel stackPanel = null;
            Button button = null;
            AutoResetEvent eventSignal = new AutoResetEvent(false);

            _asyncQueue
                .CallFromUIThread(() =>
                {
                    // Create a LayoutUpdated event handler that references an object
                    object obj = new object();
                    EventHandler<object> handler = new EventHandler<object>((s, a) =>
                    {
                        //TestLogger.LogMessage("In event handler, " + obj.ToString());

                        // Signal when fired to allow the test to continue
                        eventSignal.Set();
                    });

                    // Hook up the LayoutUpdated event handler to an element in a live tree
                    stackPanel = new StackPanel();
                    stackPanel.LayoutUpdated += handler;
                    stackPanel.UpdateLayout();
                    mainCanvas.Children.Add(stackPanel);

                    // Create an unrelated dummy object to invoke layout
                    button = new Button();

                    // Keep a weak ref on the event handler for later probing
                    weakRef = new WeakReference(handler);
                })

                // Wait for LayoutUpdated
                .WaitForHandle(eventSignal, "waiting for event")

                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(weakRef.IsAlive, "Object died");

                    // Remove the element from the tree, which should allow the event handler
                    // to go away.
                    mainCanvas.Children.Remove(stackPanel);
                    MakeNull(ref stackPanel);


                    // Do a collect, and force a layout to ensure we don't crash (bug 626259).
                    // Note that we call Collect here without WaitForPendingFinalizers, in order to repro that bug.

                    GC_Collect();
                    button.Width = 23;
                    button.UpdateLayout();
                })
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    Verify(!weakRef.IsAlive, "Object is alive");
                });

            _asyncQueue.Run();
        }

        // Bug 411198 
        // Not running because it modifies Window.Current
        // [Test]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void ReproTestGroupedCollection()
        {
            Frame rootFrame = null;

            Action a = () =>
            {
                GC_ForceCollect();

                rootFrame.BackStack.Clear();
                rootFrame.Navigate(typeof(CustomGroupedItemsPage));

                GC_ForceCollect();

                CustomViewModel.Current.LoadData();

                GC_ForceCollect();
            };

            _asyncQueue
                .CallFromUIThread(() =>
                {
                    rootFrame = new Frame();
                    Window.Current.Content = rootFrame;
                })
                .CallFromUIThread(() => a())
                .CallFromUIThread(() => a())
                .Run();
        }

        // Bug 411198 
        // Not running because it modifies Window.Current
        // [Test]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void ReproTestGroupedCollection2()
        {
            Frame rootFrame = null;

            Action a = () =>
            {
                rootFrame.BackStack.Clear();
                rootFrame.Navigate(typeof(CustomGroupedItemsPage2));
            };

            _asyncQueue
                .CallFromUIThread(() =>
                {
                    rootFrame = new Frame();
                    Window.Current.Content = rootFrame;

                    CustomViewModel.Current.LoadData();
                })
                .CallFromUIThread(() => a())
                .CallFromUIThread(() => a())
                .CallFromUIThread(() => a())
                .CallFromUIThread(() =>
                {
                    GC_ForceCollect();

                    CustomViewModel.Current.ChangeItems();
                })
                .Run();
        }

        //
        // Verify RadioButton group updates (bug 643496)
        //

        [TestMethod]
        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void RadioButtonGroupTest()
        {
            WeakReference weakRef = null;
            var groupName = "RadioButtonGroupTest";
            //RadioButton mainRadioButton = null;

            _asyncQueue
                .CallFromUIThread(() =>
                {
                    //mainRadioButton = new RadioButton() { GroupName = groupName };

                    var rb = new RadioButton() { GroupName = groupName };
                    rb.IsChecked = true;

                    weakRef = new WeakReference(rb);
                })

                .CallFromUIThread(() =>
                {
                    GC.Collect();
                    Verify(!weakRef.IsAlive, "RadioButton didn't get collected");

                    var rb = new RadioButton() { GroupName = groupName };


                    // Verify that this doesn't crash
                    rb.IsChecked = true;
                    GC_ForceCollect();
                });

            _asyncQueue.Run();
        }

    }
}