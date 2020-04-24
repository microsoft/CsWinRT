
namespace Windows.Foundation
{
    using global::System;
    using global::System.Globalization;

    [global::WinRT.WindowsRuntimeType]
    [StructLayout(LayoutKind.Sequential)]
    public struct Point : IFormattable
    {
        float _x;
        float _y;

        public Point(double x, double y)
        {
            _x = (float)x;
            _y = (float)y;
        }

        public double X
        {
            get { return _x; }
            set { _x = (float)value; }
        }

        public double Y
        {
            get { return _y; }
            set { _y = (float)value; }
        }

        public override string ToString()
        {
            return ConvertToString(null, null);
        }

        public string ToString(IFormatProvider provider)
        {
            return ConvertToString(null, provider);
        }

        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            return ConvertToString(format, provider);
        }

        private string ConvertToString(string format, IFormatProvider provider)
        {
            char separator = GetNumericListSeparator(provider);
            return string.Format(provider, "{1:" + format + "}{0}{2:" + format + "}", separator, _x, _y);
        }

        static char GetNumericListSeparator(IFormatProvider provider)
        {
            // If the decimal separator is a comma use ';'
            char numericSeparator = ',';
            var numberFormat = NumberFormatInfo.GetInstance(provider);
            if ((numberFormat.NumberDecimalSeparator.Length > 0) && (numberFormat.NumberDecimalSeparator[0] == numericSeparator))
            {
                numericSeparator = ';';
            }

            return numericSeparator;
        }

        public static bool operator==(Point point1, Point point2)
        {
            return point1.X == point2.X && point1.Y == point2.Y;
        }

        public static bool operator!=(Point point1, Point point2)
        {
            return !(point1 == point2);
        }

        public override bool Equals(object o)
        {
            return o is Point && this == (Point)o;
        }

        public bool Equals(Point value)
        {
            return (this == value);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }
    }

    [global::WinRT.WindowsRuntimeType]
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect : IFormattable
    {
        private float _x;
        private float _y;
        private float _width;
        private float _height;

        private const double EmptyX = double.PositiveInfinity;
        private const double EmptyY = double.PositiveInfinity;
        private const double EmptyWidth = double.NegativeInfinity;
        private const double EmptyHeight = double.NegativeInfinity;

        private static readonly Rect s_empty = CreateEmptyRect();

        public Rect(double x,
                    double y,
                    double width,
                    double height)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height), SR.ArgumentOutOfRange_NeedNonNegNum);

            _x = (float)x;
            _y = (float)y;
            _width = (float)width;
            _height = (float)height;
        }

        public Rect(Point point1,
                    Point point2)
        {
            _x = (float)Math.Min(point1.X, point2.X);
            _y = (float)Math.Min(point1.Y, point2.Y);

            _width = (float)Math.Max(Math.Max(point1.X, point2.X) - _x, 0);
            _height = (float)Math.Max(Math.Max(point1.Y, point2.Y) - _y, 0);
        }

        public Rect(Point location, Size size)
        {
            if (size.IsEmpty)
            {
                this = s_empty;
            }
            else
            {
                _x = (float)location.X;
                _y = (float)location.Y;
                _width = (float)size.Width;
                _height = (float)size.Height;
            }
        }

        public double X
        {
            get { return _x; }
            set { _x = (float)value; }
        }

        public double Y
        {
            get { return _y; }
            set { _y = (float)value; }
        }

        public double Width
        {
            get { return _width; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Width), SR.ArgumentOutOfRange_NeedNonNegNum);

                _width = (float)value;
            }
        }

        public double Height
        {
            get { return _height; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Height), SR.ArgumentOutOfRange_NeedNonNegNum);

                _height = (float)value;
            }
        }

        public double Left
        {
            get { return _x; }
        }

        public double Top
        {
            get { return _y; }
        }

        public double Right
        {
            get
            {
                if (IsEmpty)
                {
                    return double.NegativeInfinity;
                }

                return _x + _width;
            }
        }

        public double Bottom
        {
            get
            {
                if (IsEmpty)
                {
                    return double.NegativeInfinity;
                }

                return _y + _height;
            }
        }

        public static Rect Empty
        {
            get { return s_empty; }
        }

        public bool IsEmpty
        {
            get { return _width < 0; }
        }

        public bool Contains(Point point)
        {
            return ContainsInternal(point.X, point.Y);
        }

        public void Intersect(Rect rect)
        {
            if (!this.IntersectsWith(rect))
            {
                this = s_empty;
            }
            else
            {
                double left = Math.Max(X, rect.X);
                double top = Math.Max(Y, rect.Y);

                //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)
                Width = Math.Max(Math.Min(X + Width, rect.X + rect.Width) - left, 0);
                Height = Math.Max(Math.Min(Y + Height, rect.Y + rect.Height) - top, 0);

                X = left;
                Y = top;
            }
        }

        public void Union(Rect rect)
        {
            if (IsEmpty)
            {
                this = rect;
            }
            else if (!rect.IsEmpty)
            {
                double left = Math.Min(Left, rect.Left);
                double top = Math.Min(Top, rect.Top);


                // We need this check so that the math does not result in NaN
                if ((rect.Width == double.PositiveInfinity) || (Width == double.PositiveInfinity))
                {
                    Width = double.PositiveInfinity;
                }
                else
                {
                    //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)
                    double maxRight = Math.Max(Right, rect.Right);
                    Width = Math.Max(maxRight - left, 0);
                }

                // We need this check so that the math does not result in NaN
                if ((rect.Height == double.PositiveInfinity) || (Height == double.PositiveInfinity))
                {
                    Height = double.PositiveInfinity;
                }
                else
                {
                    //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)
                    double maxBottom = Math.Max(Bottom, rect.Bottom);
                    Height = Math.Max(maxBottom - top, 0);
                }

                X = left;
                Y = top;
            }
        }

        public void Union(Point point)
        {
            Union(new Rect(point, point));
        }

        private bool ContainsInternal(double x, double y)
        {
            return ((x >= X) && (x - Width <= X) &&
                    (y >= Y) && (y - Height <= Y));
        }

        internal bool IntersectsWith(Rect rect)
        {
            if (Width < 0 || rect.Width < 0)
            {
                return false;
            }

            return (rect.X <= X + Width) &&
                   (rect.X + rect.Width >= X) &&
                   (rect.Y <= Y + Height) &&
                   (rect.Y + rect.Height >= Y);
        }

        private static Rect CreateEmptyRect()
        {
            Rect rect = default;

            // TODO: for consistency with width/height we should change these
            //       to assign directly to the backing fields.
            rect.X = EmptyX;
            rect.Y = EmptyY;

            // the width and height properties prevent assignment of
            // negative numbers so assign directly to the backing fields.
            rect._width = (float)EmptyWidth;
            rect._height = (float)EmptyHeight;

            return rect;
        }

        public override string ToString()
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, null /* format provider */);
        }

        public string ToString(IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, provider);
        }

        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(format, provider);
        }


        internal string ConvertToString(string format, IFormatProvider provider)
        {
            if (IsEmpty)
            {
                return "Empty.";
            }

            // Helper to get the numeric list separator for a given culture.
            char separator = TokenizerHelper.GetNumericListSeparator(provider);
            return string.Format(provider,
                                 "{1:" + format + "}{0}{2:" + format + "}{0}{3:" + format + "}{0}{4:" + format + "}",
                                 separator,
                                 _x,
                                 _y,
                                 _width,
                                 _height);
        }

        public bool Equals(Rect value)
        {
            return (this == value);
        }

        public static bool operator ==(Rect rect1, Rect rect2)
        {
            return rect1.X == rect2.X &&
                   rect1.Y == rect2.Y &&
                   rect1.Width == rect2.Width &&
                   rect1.Height == rect2.Height;
        }

        public static bool operator !=(Rect rect1, Rect rect2)
        {
            return !(rect1 == rect2);
        }

        public override bool Equals(object o)
        {
            return o is Rect && this == (Rect)o;
        }

        public override int GetHashCode()
        {
            // Perform field-by-field XOR of HashCodes
            return X.GetHashCode() ^
                   Y.GetHashCode() ^
                   Width.GetHashCode() ^
                   Height.GetHashCode();
        }
    }

    [global::WinRT.WindowsRuntimeType]
    [StructLayout(LayoutKind.Sequential)]
    public struct Size
    {
        private float _width;
        private float _height;

        private static readonly Size s_empty = CreateEmptySize();

        public Size(double width, double height)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height), SR.ArgumentOutOfRange_NeedNonNegNum);
            _width = (float)width;
            _height = (float)height;
        }

        public double Width
        {
            get { return _width; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Width), SR.ArgumentOutOfRange_NeedNonNegNum);

                _width = (float)value;
            }
        }

        public double Height
        {
            get { return _height; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Height), SR.ArgumentOutOfRange_NeedNonNegNum);

                _height = (float)value;
            }
        }

        public static Size Empty
        {
            get { return s_empty; }
        }


        public bool IsEmpty
        {
            get { return Width < 0; }
        }

        private static Size CreateEmptySize()
        {
            Size size = default;
            // We can't set these via the property setters because negatives widths
            // are rejected in those APIs.
            size._width = float.NegativeInfinity;
            size._height = float.NegativeInfinity;
            return size;
        }

        public static bool operator ==(Size size1, Size size2)
        {
            return size1.Width == size2.Width &&
                   size1.Height == size2.Height;
        }

        public static bool operator !=(Size size1, Size size2)
        {
            return !(size1 == size2);
        }

        public override bool Equals(object o)
        {
            return o is Size && Size.Equals(this, (Size)o);
        }

        public bool Equals(Size value)
        {
            return Size.Equals(this, value);
        }

        public override int GetHashCode()
        {
            if (IsEmpty)
            {
                return 0;
            }
            else
            {
                // Perform field-by-field XOR of HashCodes
                return Width.GetHashCode() ^
                       Height.GetHashCode();
            }
        }

        private static bool Equals(Size size1, Size size2)
        {
            if (size1.IsEmpty)
            {
                return size2.IsEmpty;
            }
            else
            {
                return size1.Width.Equals(size2.Width) &&
                       size1.Height.Equals(size2.Height);
            }
        }

        public override string ToString()
        {
            if (IsEmpty)
            {
                return "Empty";
            }

            return string.Format("{0},{1}", _width, _height);
        }
    }

    public static class TokenizerHelper
    {
        internal static char GetNumericListSeparator(IFormatProvider provider)
        {
            char numericSeparator = ',';

            // Get the NumberFormatInfo out of the provider, if possible
            // If the IFormatProvider doesn't not contain a NumberFormatInfo, then
            // this method returns the current culture's NumberFormatInfo.
            NumberFormatInfo numberFormat = NumberFormatInfo.GetInstance(provider);

            // Is the decimal separator is the same as the list separator?
            // If so, we use the ";".
            if ((numberFormat.NumberDecimalSeparator.Length > 0) && (numericSeparator == numberFormat.NumberDecimalSeparator[0]))
            {
                numericSeparator = ';';
            }

            return numericSeparator;
        }
    }
}

namespace ABI.Windows.Foundation
{
    public static class Point
    {
        public static string GetGuidSignature()
        {
            return "struct(Windows.Foundation.Point;f4;f4)";
        }
    }
    
    public static class Rect
    {
        public static string GetGuidSignature()
        {
            return "struct(Windows.Foundation.Rect;f4;f4;f4;f4)";
        }
    }
    
    public static class Size
    {
        public static string GetGuidSignature()
        {
            return "struct(Windows.Foundation.Size;f4;f4)";
        }
    }
}

namespace System
{
    using global::System.Diagnostics;
    using global::System.Runtime.CompilerServices;
    using global::System.Runtime.InteropServices;
    using global::System.Threading;
    using global::System.Threading.Tasks;
    using global::Windows.Foundation;

    public static class WindowsRuntimeSystemExtensions
    {
        public static Task AsTask(this IAsyncAction source, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // TODO: Handle the scenario where the 'IAsyncAction' is actually a task (i.e. originated from native code
            // but projected into an IAsyncAction)

            switch (source.Status)
            {
                case AsyncStatus.Completed:
                    return Task.CompletedTask;

                case AsyncStatus.Error:
                    return Task.FromException(source.ErrorCode);

                case AsyncStatus.Canceled:
                    return Task.FromCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
            }

            var bridge = new AsyncInfoToTaskBridge<VoidValueTypeParameter, VoidValueTypeParameter>(cancellationToken);
            source.Completed = new AsyncActionCompletedHandler(bridge.CompleteFromAsyncAction);
            bridge.RegisterForCancellation(source);
            return bridge.Task;
        }

        public static Task AsTask(this IAsyncAction source)
        {
            return AsTask(source, CancellationToken.None);
        }

        public static TaskAwaiter GetAwaiter(this IAsyncAction source)
        {
            return AsTask(source).GetAwaiter();
        }

        public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // TODO: Handle the scenario where the 'IAsyncOperation' is actually a task (i.e. originated from native code
            // but projected into an IAsyncOperation)

            switch (source.Status)
            {
                case AsyncStatus.Completed:
                    return Task.FromResult(source.GetResults());

                case AsyncStatus.Error:
                    return Task.FromException<TResult>(source.ErrorCode);

                case AsyncStatus.Canceled:
                    return Task.FromCanceled<TResult>(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
            }

            var bridge = new AsyncInfoToTaskBridge<TResult, VoidValueTypeParameter>(cancellationToken);
            source.Completed = new AsyncOperationCompletedHandler<TResult>(bridge.CompleteFromAsyncOperation);
            bridge.RegisterForCancellation(source);
            return bridge.Task;
        }

        public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source)
        {
            return AsTask(source, CancellationToken.None);
        }

        public static TaskAwaiter<TResult> GetAwaiter<TResult>(this IAsyncOperation<TResult> source)
        {
            return AsTask(source).GetAwaiter();
        }

        public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, CancellationToken cancellationToken, IProgress<TProgress> progress)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // TODO: Handle the scenario where the 'IAsyncActionWithProgress' is actually a task (i.e. originated from native code
            // but projected into an IAsyncActionWithProgress)

            switch (source.Status)
            {
                case AsyncStatus.Completed:
                    return Task.CompletedTask;

                case AsyncStatus.Error:
                    return Task.FromException(source.ErrorCode);

                case AsyncStatus.Canceled:
                    return Task.FromCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
            }

            if (progress != null)
            {
                SetProgress(source, progress);
            }

            var bridge = new AsyncInfoToTaskBridge<VoidValueTypeParameter, TProgress>(cancellationToken);
            source.Completed = new AsyncActionWithProgressCompletedHandler<TProgress>(bridge.CompleteFromAsyncActionWithProgress);
            bridge.RegisterForCancellation(source);
            return bridge.Task;
        }

        private static void SetProgress<TProgress>(IAsyncActionWithProgress<TProgress> source, IProgress<TProgress> sink)
        {
            // This is separated out into a separate method so that we only pay the costs of compiler-generated closure if progress is non-null.
            source.Progress = new AsyncActionProgressHandler<TProgress>((_, info) => sink.Report(info));
        }

        public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source)
        {
            return AsTask(source, CancellationToken.None, null);
        }

        public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, CancellationToken cancellationToken)
        {
            return AsTask(source, cancellationToken, null);
        }

        public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, IProgress<TProgress> progress)
        {
            return AsTask(source, CancellationToken.None, progress);
        }

        public static TaskAwaiter GetAwaiter<TProgress>(this IAsyncActionWithProgress<TProgress> source)
        {
            return AsTask(source).GetAwaiter();
        }

        public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, CancellationToken cancellationToken, IProgress<TProgress> progress)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // TODO: Handle the scenario where the 'IAsyncOperationWithProgress' is actually a task (i.e. originated from native code
            // but projected into an IAsyncOperationWithProgress)

            switch (source.Status)
            {
                case AsyncStatus.Completed:
                    return Task.FromResult(source.GetResults());

                case AsyncStatus.Error:
                    return Task.FromException<TResult>(source.ErrorCode);

                case AsyncStatus.Canceled:
                    return Task.FromCanceled<TResult>(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
            }

            if (progress != null)
            {
                SetProgress(source, progress);
            }

            var bridge = new AsyncInfoToTaskBridge<TResult, TProgress>(cancellationToken);
            source.Completed = new AsyncOperationWithProgressCompletedHandler<TResult, TProgress>(bridge.CompleteFromAsyncOperationWithProgress);
            bridge.RegisterForCancellation(source);
            return bridge.Task;
        }

        private static void SetProgress<TResult, TProgress>(IAsyncOperationWithProgress<TResult, TProgress> source, IProgress<TProgress> sink)
        {
            // This is separated out into a separate method so that we only pay the costs of compiler-generated closure if progress is non-null.
            source.Progress = new AsyncOperationProgressHandler<TResult, TProgress>((_, info) => sink.Report(info));
        }

        public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
        {
            return AsTask(source, CancellationToken.None, null);
        }


        public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, CancellationToken cancellationToken)
        {
            return AsTask(source, cancellationToken, null);
        }

        public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, IProgress<TProgress> progress)
        {
            return AsTask(source, CancellationToken.None, progress);
        }

        public static TaskAwaiter<TResult> GetAwaiter<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
        {
            return AsTask(source).GetAwaiter();
        }

        public static IAsyncAction AsAsyncAction(this Task source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new TaskToAsyncActionAdapter(source, underlyingCancelTokenSource: null);
        }

        public static IAsyncOperation<TResult> AsAsyncOperation<TResult>(this Task<TResult> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new TaskToAsyncOperationAdapter<TResult>(source, underlyingCancelTokenSource: null);
        }
    }

    // Marker type since generic parameters cannot be 'void'
    struct VoidValueTypeParameter { }

    /// <summary>This can be used instead of <code>VoidValueTypeParameter</code> when a reference type is required.
    /// In case of an actual instantiation (e.g. through <code>default(T)</code>),
    /// using <code>VoidValueTypeParameter</code> offers better performance.</summary>
    internal class VoidReferenceTypeParameter { }

    sealed class AsyncInfoToTaskBridge<TResult, TProgress> : TaskCompletionSource<TResult>
    {
        private readonly CancellationToken _ct;
        private CancellationTokenRegistration _ctr;
        private bool _completing;

        internal AsyncInfoToTaskBridge(CancellationToken cancellationToken)
        {
            // TODO: AsyncCausality?
            _ct = cancellationToken;
        }

        internal void RegisterForCancellation(IAsyncInfo asyncInfo)
        {
            Debug.Assert(asyncInfo != null);

            try
            {
                if (_ct.CanBeCanceled && !_completing)
                {
                    var ctr = _ct.Register(ai => ((IAsyncInfo)ai).Cancel(), asyncInfo);
                    bool disposeOfCtr = false;
                    lock (this)
                    {
                        if (_completing)
                        {
                            disposeOfCtr = true;
                        }
                        else
                        {
                            _ctr = ctr;
                        }
                    }

                    if (disposeOfCtr)
                    {
                        ctr.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                if (!base.Task.IsFaulted)
                {
                    Debug.Fail($"Expected base task to already be faulted but found it in state {base.Task.Status}");
                    base.TrySetException(ex);
                }
            }
        }

        internal void CompleteFromAsyncAction(IAsyncAction asyncInfo, AsyncStatus asyncStatus)
        {
            Complete(asyncInfo, null, asyncStatus);
        }

        internal void CompleteFromAsyncActionWithProgress(IAsyncActionWithProgress<TProgress> asyncInfo, AsyncStatus asyncStatus)
        {
            Complete(asyncInfo, null, asyncStatus);
        }

        internal void CompleteFromAsyncOperation(IAsyncOperation<TResult> asyncInfo, AsyncStatus asyncStatus)
        {
            Complete(asyncInfo, ai => ((IAsyncOperation<TResult>)ai).GetResults(), asyncStatus);
        }

        internal void CompleteFromAsyncOperationWithProgress(IAsyncOperationWithProgress<TResult, TProgress> asyncInfo, AsyncStatus asyncStatus)
        {
            Complete(asyncInfo, ai => ((IAsyncOperationWithProgress<TResult, TProgress>)ai).GetResults(), asyncStatus);
        }

        private void Complete(IAsyncInfo asyncInfo, Func<IAsyncInfo, TResult> getResultsFunction, AsyncStatus asyncStatus)
        {
            if (asyncInfo == null)
            {
                throw new ArgumentNullException(nameof(asyncInfo));
            }

            // TODO: AsyncCausality?

            try
            {
                Debug.Assert(asyncInfo.Status == asyncStatus, "asyncInfo.Status does not match asyncStatus; are we dealing with a faulty IAsyncInfo implementation?");
                if (Task.IsCompleted)
                {
                    Debug.Fail("Expected the task to not yet be completed.");
                    throw new InvalidOperationException("The asynchronous operation could not be completed.");
                }

                // Clean up our registration with the cancellation token, noting that we're now in the process of cleaning up.
                CancellationTokenRegistration ctr;
                lock (this)
                {
                    _completing = true;
                    ctr = _ctr;
                    _ctr = default;
                }
                ctr.Dispose();

                try
                {
                    if (asyncStatus != AsyncStatus.Completed && asyncStatus != AsyncStatus.Canceled && asyncStatus != AsyncStatus.Error)
                    {
                        Debug.Fail("The async operation should be in a terminal state.");
                        throw new InvalidOperationException("The asynchronous operation could not be completed.");
                    }

                    TResult result = default(TResult);
                    Exception error = null;
                    if (asyncStatus == AsyncStatus.Error)
                    {
                        error = asyncInfo.ErrorCode;

                        // Defend against a faulty IAsyncInfo implementation
                        if (error is null)
                        {
                            Debug.Fail("IAsyncInfo.Status == Error, but ErrorCode returns a null Exception (implying S_OK).");
                            error = new InvalidOperationException("The asynchronous operation could not be completed.");
                        }
                    }
                    else if (asyncStatus == AsyncStatus.Completed && getResultsFunction != null)
                    {
                        try
                        {
                            result = getResultsFunction(asyncInfo);
                        }
                        catch (Exception resultsEx)
                        {
                            // According to the WinRT team, this can happen in some egde cases, such as marshalling errors in GetResults.
                            error = resultsEx;
                            asyncStatus = AsyncStatus.Error;
                        }
                    }

                    // Complete the task based on the previously retrieved results:
                    bool success = false;
                    switch (asyncStatus)
                    {
                        case AsyncStatus.Completed:
                            // TODO: AsyncCausality?
                            success = base.TrySetResult(result);
                            break;

                        case AsyncStatus.Error:
                            Debug.Assert(error != null, "The error should have been retrieved previously.");
                            success = base.TrySetException(error);
                            break;

                        case AsyncStatus.Canceled:
                            success = base.TrySetCanceled(_ct.IsCancellationRequested ? _ct : new CancellationToken(true));
                            break;
                    }

                    Debug.Assert(success, "Expected the outcome to be successfully transfered to the task.");
                }
                catch (Exception exc)
                {
                    Debug.Fail($"Unexpected exception in Complete: {exc}");

                    // TODO: AsyncCausality

                    if (!base.TrySetException(exc))
                    {
                        Debug.Fail("The task was already completed and thus the exception couldn't be stored.");
                        throw;
                    }
                }
            }
            finally
            {
                // We may be called on an STA thread which we don't own, so make sure that the RCW is released right
                // away. Otherwise, if we leave it up to the finalizer, the apartment may already be gone.
                if (ComWrappersSupport.TryUnwrapObject(asyncInfo, out var objRef))
                {
                    objRef.Dispose();
                }
            }
        }
    }
}
