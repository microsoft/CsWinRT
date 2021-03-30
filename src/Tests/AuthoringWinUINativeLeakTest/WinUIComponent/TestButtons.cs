using System;
using Microsoft.UI.Xaml.Controls;

#pragma warning disable CA1416

namespace WinUIComponent
{
    public static class TestButtons
    {
        public static WeakReference weakRefCustomButton = null;
        public static WeakReference weakRefRegularButton = null;
        public static CustomButton customButton = null;
        public static Button regularButton = null;

        public static CustomButton GetCustomButton()
        {
            var customButton = new CustomButton();
            weakRefCustomButton = new WeakReference(customButton);
            return customButton;
        }

        public static Button GetRegularButton()
        {
            Button button = new Button
            {
                Content = "Button"
            };
            weakRefRegularButton = new WeakReference(button);
            return button;
        }

        public static void SetCustomButton(CustomButton button)
        {
            customButton = button;
            weakRefCustomButton = new WeakReference(customButton);
        }

        public static void SetButton(Button button)
        {
            regularButton = button;
            weakRefRegularButton = new WeakReference(regularButton);
        }

        public static void ReleaseCustomButton()
        {
            customButton = null;
        }

        public static void ReleaseRegularButton()
        {
            regularButton = null;
        }

        public static bool IsAliveCustomButton()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return (weakRefCustomButton != null && weakRefCustomButton.IsAlive);
        }

        public static bool IsAliveRegularButton()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return (weakRefRegularButton != null && weakRefRegularButton.IsAlive);
        }
    }
}
