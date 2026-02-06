//using System;
//using System.Reactive;
//using System.Reactive.Linq;

//namespace ItemCodex
//{
//    public sealed class ReactiveHover<T>
//    {
//        private readonly IObservable<T?> _source;

//        public IObservable<T> HoverStarted { get; }
//        public IObservable<Unit> HoverEnded { get; }
//        public IObservable<bool> IsHovering { get; }
//        public IObservable<T?> State { get; }

//        public ReactiveHover(IObservable<T?> source)
//        {
//            _source = source;

//            // Emits true when first non-null appears
//            // Emits false when a stable null appears
//            IsHovering =
//                _source
//                    .Scan(
//                        (hovering: false, prev: (T?)default),
//                        (state, next) =>
//                        {
//                            if (next is not null)
//                                return (hovering: true, prev: next);

//                            // next is null
//                            if (state.hovering)
//                                return (hovering: false, prev: default);

//                            return state;
//                        })
//                    .Select(s => s.hovering)
//                    .DistinctUntilChanged()
//                    .Publish()
//                    .RefCount();

//            HoverStarted =
//                _source
//                    .Where(x => x is not null)
//                    .WithLatestFrom(IsHovering, (item, hovering) => (item, hovering))
//                    .Where(t => t.hovering)
//                    .Select(t => t.item!)
//                    .DistinctUntilChanged()
//                    .Publish()
//                    .RefCount();

//            HoverEnded =
//                IsHovering
//                    .Where(h => h == false)
//                    .Select(_ => Unit.Default)
//                    .Publish()
//                    .RefCount();

//            State =
//                HoverStarted
//                    .Select<T, T?>(x => x)
//                    .Merge(HoverEnded.Select(_ => default(T?)))
//                    .Publish()
//                    .RefCount();
//        }
//    }
//}
