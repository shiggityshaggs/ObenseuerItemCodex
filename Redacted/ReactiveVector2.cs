//using System;
//using System.Reactive.Linq;
//using System.Reactive.Subjects;
//using UnityEngine;

//namespace ItemCodex
//{
//    public sealed class ReactiveVector2 : IDisposable
//    {
//        private readonly BehaviorSubject<Vector2> subject;
//        public IObservable<Vector2> Changed => subject.DistinctUntilChanged();

//        public Vector2 Value
//        {
//            get => subject.Value;
//            set
//            {
//                if (subject.Value.x != value.x || subject.Value.y != value.y)
//                    subject.OnNext(value);
//            }
//        }

//        public ReactiveVector2() : this(Vector2.zero) { }

//        public ReactiveVector2(Vector2 initialValue)
//        {
//            subject = new BehaviorSubject<Vector2>(initialValue);
//        }

//        public void Dispose()
//        {
//            subject.Dispose();
//        }

//        public override string ToString() => Value.ToString();
//    }
//}
