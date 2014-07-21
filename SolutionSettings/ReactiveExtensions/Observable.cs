using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Enexure.SolutionSettings.ReactiveExtensions
{
	static class ObservableExtensions
	{
		public static IObservable<Unit> AsCompletion<T>(this IObservable<T> observable)
		{
			return Observable.Create<Unit>(observer => {
				Action onCompleted = () => {
					observer.OnNext(Unit.Default);
					observer.OnCompleted();
				};
				return observable.Subscribe(_ => { }, observer.OnError, onCompleted);
			});
		}

		public static IObservable<TRet> ContinueAfter<T, TRet>(
		  this IObservable<T> observable, Func<IObservable<TRet>> selector)
		{
			return observable.AsCompletion().SelectMany(_ => selector());
		}
	}

	public static class ObservableTrace
	{
		public static IObservable<TSource> Trace<TSource>(this IObservable<TSource> source, string name)
		{
#if DEBUG
			var id = 0;
			return Observable.Create<TSource>(observer => {
				
				var itemId = ++id;
				Action<string, object> trace = (m, v) => Log.Information("{name}{id}: {method}({value})", name, itemId, m, v);

				trace("Subscribe", null);
				IDisposable disposable = source.Subscribe(
					v => { trace("OnNext", v); observer.OnNext(v); },
					e => { trace("OnError", e); observer.OnError(e); },
					() => { trace("OnCompleted", null); observer.OnCompleted(); });

				return () => { trace("Dispose", null); disposable.Dispose(); };
			});
#else
			return source;
#endif

		}
	}
}
