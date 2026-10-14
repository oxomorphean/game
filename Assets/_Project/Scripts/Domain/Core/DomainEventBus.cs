using System;
using System.Collections.Generic;
using System.Linq;

namespace LinhThu.Domain.Core
{
    /// <summary>Single-threaded, queued delivery after state commits. Listener failures do not replay commands.</summary>
    public sealed class DomainEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> listeners = new Dictionary<Type, List<Delegate>>();
        private readonly Queue<Action<List<Exception>>> pending = new Queue<Action<List<Exception>>>();
        private bool flushing;
        public bool HasPending => pending.Count != 0 || flushing;
        public IDisposable Subscribe<T>(Action<T> handler)
            where T : struct
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (!listeners.TryGetValue(typeof(T), out var list))
                listeners.Add(typeof(T), list = new List<Delegate>());
            list.Add(handler);
            return new Subscription(() => list.Remove(handler));
        }

        public void Publish<T>(T value)
            where T : struct
        {
            pending.Enqueue(errors =>
            {
                if (!listeners.TryGetValue(typeof(T), out var list))
                    return;
                foreach (var handler in list.ToArray())
                    try
                    {
                        ((Action<T>)handler)(value);
                    }
                    catch (Exception e)
                    {
                        errors.Add(e);
                    }
            });
        }

        public void Flush()
        {
            if (flushing)
                throw new InvalidOperationException("Reentrant event flush.");
            var errors = new List<Exception>();
            flushing = true;
            try
            {
                int count = pending.Count;
                for (int i = 0; i < count; i++)
                    pending.Dequeue()(errors);
            }
            finally
            {
                flushing = false;
            }

            if (errors.Count > 0)
                throw new AggregateException("Domain event listeners failed after commit.", errors);
        }

        private sealed class Subscription : IDisposable
        {
            private Action dispose;
            internal Subscription(Action dispose)
            {
                this.dispose = dispose;
            }

            public void Dispose()
            {
                var action = dispose;
                dispose = null;
                action?.Invoke();
            }
        }
    }
}
