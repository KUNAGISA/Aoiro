using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Aoiro.Tests
{
    public sealed class BindingAndCancellationTests
    {
        [Test]
        public void BindableProperty_UsesInstanceComparerAndOptionalImmediateNotification()
        {
            var insensitive = new BindableProperty<string>("a", StringComparer.OrdinalIgnoreCase);
            var ordinal = new BindableProperty<string>("a", StringComparer.Ordinal);
            var insensitiveCalls = 0;
            var ordinalValues = new List<string>();
            insensitive.Subscribe(_ => ++insensitiveCalls, true);
            ordinal.Subscribe(ordinalValues.Add, true);

            insensitive.Value = "A";
            ordinal.Value = "A";
            ordinal.SetValueWithoutNotify("B");

            Assert.AreEqual(1, insensitiveCalls);
            CollectionAssert.AreEqual(new[] { "a", "A" }, ordinalValues);
            Assert.AreEqual("B", ordinal.Value);
        }

        [Test]
        public void BindableProperty_ImmediateCallbackExceptionStillRegistersCancellableSubscription()
        {
            var property = new BindableProperty<int>(1);
            var values = new List<int>();
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: immediate notification failed"));
            var token = property.Subscribe(value =>
            {
                values.Add(value);
                if (value == 1)
                {
                    property.Value = 2;
                    throw new InvalidOperationException("immediate notification failed");
                }
            }, true);

            CollectionAssert.AreEqual(new[] { 1 }, values);
            Assert.AreEqual(2, property.Value);
            property.Value = 3;
            token.Cancel();
            property.Value = 4;

            CollectionAssert.AreEqual(new[] { 1, 3 }, values);
            Assert.AreEqual(4, property.Value);
        }

        [Test]
        public void EventBus_IsolatesTypesAndQueuesSameTypeRecursion()
        {
            var bus = new EventBus();
            var values = new List<string>();
            bus.Subscribe<int>(value =>
            {
                values.Add("i" + value);
                if (value == 1)
                {
                    bus.Publish("nested");
                    bus.Publish(2);
                }
            });
            bus.Subscribe<string>(value => values.Add(value));

            bus.Publish(1);

            CollectionAssert.AreEqual(new[] { "i1", "nested", "i2" }, values);
        }

        [Test]
        public void CancelAllTokens_CancelsEveryTokenAndAggregatesFailures()
        {
            var gameObject = new GameObject("cancel-test");
            try
            {
                var successful = new TestToken();
                var failing = new TestToken(true);
                successful.CancelOnDestroy(gameObject);
                failing.CancelOnDisable(gameObject);

                var exception = Assert.Throws<AggregateException>(gameObject.CancelAllTokens);

                Assert.AreEqual(1, successful.Count);
                Assert.AreEqual(1, failing.Count);
                Assert.AreEqual(1, exception.InnerExceptions.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CancelToken_CancelsWrappedStructOnlyOnce()
        {
            var counter = new CancelCounter();
            var token = new CancelToken<StructToken>(new StructToken(counter));

            token.Cancel();
            token.Cancel();

            Assert.AreEqual(1, counter.Count);
        }

        [Test]
        public void CancelToken_ExceptionStillMakesWrapperIdempotent()
        {
            var counter = new CancelCounter();
            var token = new CancelToken<StructToken>(new StructToken(counter, true));

            Assert.Throws<InvalidOperationException>(token.Cancel);
            Assert.DoesNotThrow(token.Cancel);

            Assert.AreEqual(1, counter.Count);
        }

        [Test]
        public void CancelToken_EqualStructValuesKeepIndependentIdentity()
        {
            var counter = new CancelCounter();
            var value = new StructToken(counter);
            var tokens = new HashSet<ICancelToken>
            {
                new CancelToken<StructToken>(value),
                new CancelToken<StructToken>(value)
            };

            Assert.AreEqual(2, tokens.Count);
            foreach (var token in tokens)
            {
                token.Cancel();
            }

            Assert.AreEqual(2, counter.Count);
        }

        private sealed class TestToken : ICancelToken
        {
            private readonly bool _throws;
            internal int Count { get; private set; }
            internal TestToken(bool throws = false) => _throws = throws;

            public void Cancel()
            {
                ++Count;
                if (_throws)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private sealed class CancelCounter
        {
            internal int Count;
        }

        private readonly struct StructToken : ICancelToken
        {
            private readonly CancelCounter _counter;
            private readonly bool _throws;

            internal StructToken(CancelCounter counter, bool throws = false)
            {
                _counter = counter;
                _throws = throws;
            }

            public void Cancel()
            {
                ++_counter.Count;
                if (_throws)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
