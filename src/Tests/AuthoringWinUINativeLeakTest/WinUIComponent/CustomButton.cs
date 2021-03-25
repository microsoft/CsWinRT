using System;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace WinUIComponent
{
    public sealed class CustomButton : Button
    {
        public string Text { get; private set; }
        public bool OverrideEntered { get; set; }

        public CustomButton()
            : this("CustomButton")
        {
        }

        public CustomButton(string text)
        {
            Text = text;
            Content = text;
            OverrideEntered = true;
        }

        protected override void OnPointerEntered(global::Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!OverrideEntered)
            {
                base.OnPointerEntered(e);
                return;
            }

            Text = Content?.ToString();
            Content = "Entered";
        }

        protected override void OnPointerExited(global::Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!OverrideEntered)
            {
                base.OnPointerExited(e);
                return;
            }

            Content = Text;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var size = new Size(160, 30);
            base.MeasureOverride(size);
            return size;
        }

        public string GetText()
        {
            return Text;
        }
    }
}
