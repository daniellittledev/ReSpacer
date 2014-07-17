using System;
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
}
