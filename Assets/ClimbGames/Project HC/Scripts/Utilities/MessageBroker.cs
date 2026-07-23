using System;
using System.Collections.Concurrent;
using R3;

namespace ClimbGames.Project_HC
{
    public sealed class MessageBroker : IDisposable
    {
        private static readonly Lazy<MessageBroker> _default = new(() => new MessageBroker());
        public static MessageBroker Default => _default.Value;

        private readonly ConcurrentDictionary<Type, object> _subjects = new();

        public Observable<T> Receive<T>()
        {
            var subject = (Subject<T>)_subjects.GetOrAdd(typeof(T), _ => new Subject<T>());
            return subject.AsObservable();
        }

        public void Publish<T>(T message)
        {
            if (_subjects.TryGetValue(typeof(T), out var obj))
            {
                ((ISubject<T>)obj).OnNext(message);
            }
        }

        public void Dispose()
        {
            foreach (var pair in _subjects)
            {
                if (pair.Value is IDisposable disposable)
                    disposable.Dispose();
            }

            _subjects.Clear();
        }
    }
}