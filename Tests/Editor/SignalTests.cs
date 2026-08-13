using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Aoiro.Tests
{
    public sealed class SignalTests
    {
        [Test]
        public void Signal_PreservesOrderAndSupportsPreciseCancellation()
        {
            var signal = new Signal<int>();
            var values = new List<int>();
            Action<int> duplicate = value => values.Add(value * 10);
            signal.Subscribe(value => values.Add(value));
            var firstDuplicate = signal.Subscribe(duplicate);
            signal.Subscribe(duplicate);

            firstDuplicate.Cancel();
            firstDuplicate.Cancel();
            signal.Emit(2);

            CollectionAssert.AreEqual(new[] { 2, 20 }, values);
        }

        [Test]
        public void Signal_DefersNewSubscriptionAndAppliesCancellationImmediately()
        {
            var signal = new Signal();
            var calls = new List<string>();
            SubscriptionToken second = default;
            signal.Subscribe(() =>
            {
                calls.Add("first");
                second.Cancel();
                signal.Subscribe(() => calls.Add("late"));
            });
            second = signal.Subscribe(() => calls.Add("second"));

            signal.Emit();
            signal.Emit();

            CollectionAssert.AreEqual(new[] { "first", "first", "late" }, calls);
        }

        [Test]
        public void Signal_OldTokenCannotCancelReusedSlot()
        {
            var signal = new Signal();
            var old = signal.Subscribe(() => { });
            old.Cancel();
            var calls = 0;
            signal.Subscribe(() => ++calls);
            old.Cancel();

            signal.Emit();

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Signal_RejectsRecursionAndRecoversAfterException()
        {
            var signal = new Signal();
            var recursive = signal.Subscribe(signal.Emit);
            Assert.Throws<InvalidOperationException>(signal.Emit);
            recursive.Cancel();
            var calls = 0;
            signal.Subscribe(() => ++calls);
            Assert.DoesNotThrow(signal.Emit);
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Signal_ClearStopsCurrentDispatchAndInvalidatesTokens()
        {
            var signal = new Signal();
            var calls = 0;
            var old = signal.Subscribe(() =>
            {
                ++calls;
                signal.Clear();
            });
            signal.Subscribe(() => ++calls);

            signal.Emit();
            signal.Subscribe(() => ++calls);
            old.Cancel();
            signal.Emit();

            Assert.AreEqual(2, calls);
        }

        [Test]
        public void QueuedSignal_DispatchesRecursiveValuesInFifoOrder()
        {
            var signal = new QueuedSignal<int>();
            var values = new List<string>();
            signal.Subscribe(value =>
            {
                values.Add($"a{value}");
                if (value == 1)
                {
                    signal.Emit(2);
                    signal.Emit(3);
                }
            });
            signal.Subscribe(value => values.Add($"b{value}"));

            signal.Emit(1);

            CollectionAssert.AreEqual(new[] { "a1", "b1", "a2", "b2", "a3", "b3" }, values);
        }

        [Test]
        public void QueuedSignal_ExceptionClearsPendingValuesAndRecovers()
        {
            var signal = new QueuedSignal<int>();
            SubscriptionToken throwing = default;
            throwing = signal.Subscribe(value =>
            {
                if (value == 1)
                {
                    signal.Emit(2);
                    throw new TestException();
                }
            });
            Assert.Throws<TestException>(() => signal.Emit(1));
            throwing.Cancel();
            var observed = 0;
            signal.Subscribe(value => observed = value);
            signal.Emit(3);
            Assert.AreEqual(3, observed);
        }

        [Test]
        public void QueuedSignal_StopsPossibleEventLoopAtBudget()
        {
            var signal = new QueuedSignal<int>();
            signal.Subscribe(value => signal.Emit(value + 1));
            Assert.Throws<InvalidOperationException>(() => signal.Emit(0));
            signal.Clear();
            Assert.DoesNotThrow(() => signal.Emit(0));
        }

        private sealed class TestException : Exception { }
    }
}
