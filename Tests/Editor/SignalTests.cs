using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
            var calls = 0;
            signal.Subscribe(() => ++calls);
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: Recursive emission of the same Signal instance is not supported\\."));
            Assert.DoesNotThrow(signal.Emit);
            Assert.AreEqual(1, calls);

            recursive.Cancel();
            Assert.DoesNotThrow(signal.Emit);
            Assert.AreEqual(2, calls);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Signal_LogsCallbackExceptionsAndContinuesDispatch(bool withValue)
        {
            var calls = new List<int>();
            Action emit;
            if (withValue)
            {
                var signal = new Signal<int>();
                signal.Subscribe(value => calls.Add(value));
                signal.Subscribe(_ => throw new TestException());
                signal.Subscribe(value => calls.Add(value * 10));
                emit = () => signal.Emit(2);
            }
            else
            {
                var signal = new Signal();
                signal.Subscribe(() => calls.Add(2));
                signal.Subscribe(() => throw new TestException());
                signal.Subscribe(() => calls.Add(20));
                emit = signal.Emit;
            }

            LogAssert.Expect(LogType.Exception, new Regex("TestException"));
            Assert.DoesNotThrow(() => emit());
            LogAssert.Expect(LogType.Exception, new Regex("TestException"));
            Assert.DoesNotThrow(() => emit());

            CollectionAssert.AreEqual(new[] { 2, 20, 2, 20 }, calls);
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
        public void QueuedSignal_CallbackExceptionPreservesPendingValuesAndContinuesDispatch()
        {
            var signal = new QueuedSignal<int>();
            var values = new List<string>();
            signal.Subscribe(value =>
            {
                values.Add($"a{value}");
                if (value == 1)
                {
                    signal.Emit(2);
                    throw new TestException();
                }
            });
            signal.Subscribe(value => values.Add($"b{value}"));

            LogAssert.Expect(LogType.Exception, new Regex("TestException"));
            Assert.DoesNotThrow(() => signal.Emit(1));
            CollectionAssert.AreEqual(new[] { "a1", "b1", "a2", "b2" }, values);

            signal.Emit(3);
            CollectionAssert.AreEqual(new[] { "a1", "b1", "a2", "b2", "a3", "b3" }, values);
        }

        [Test]
        public void QueuedSignal_StopsPossibleEventLoopAtBudget()
        {
            var signal = new QueuedSignal<int>();
            signal.Subscribe(value => signal.Emit(value + 1));
            Assert.Throws<InvalidOperationException>(() => signal.Emit(0));
            signal.Reset();
            Assert.DoesNotThrow(() => signal.Emit(0));
        }

        private sealed class TestException : Exception { }
    }
}
