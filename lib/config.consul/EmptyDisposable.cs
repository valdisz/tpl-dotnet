namespace Sable
{
    using System;

    public class EmptyDisposable : IDisposable
    {
        private EmptyDisposable() { }

        public void Dispose() { }

        private static readonly Lazy<EmptyDisposable> instance =
            new Lazy<EmptyDisposable>(() => new EmptyDisposable(), true);

        internal static EmptyDisposable Instance => instance.Value;
    }
}
