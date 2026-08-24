using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Aoiro.Tests
{
    public sealed class ArchitectureTests
    {
        [SetUp]
        public void SetUp()
        {
            TestArchitecture.Shutdown();
            TestArchitecture.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            TestArchitecture.Shutdown();
        }

        [Test]
        public void Initialize_InitializesByLayerAndShutdownsInReverseDependencyOrder()
        {
            TestArchitecture.Initialize();
            CollectionAssert.AreEqual(new[] { "service:init", "model:init", "system:init" }, TestArchitecture.Log);

            TestArchitecture.Shutdown();

            CollectionAssert.AreEqual(new[]
            {
                "service:init", "model:init", "system:init", "pre", "system:shutdown", "model:shutdown", "service:shutdown"
            }, TestArchitecture.Log);
            Assert.IsFalse(TestArchitecture.IsInitialized);
        }

        [Test]
        public void RegisterModule_ReplacementShutdownsOldModuleAndEnumerationUsesRegistrationOrder()
        {
            TestArchitecture.Initialize();
            var architecture = TestArchitecture.Instance;
            var replacement = new TestSystem("replacement");

            architecture.RegisterModule(replacement);

            Assert.AreSame(replacement, architecture.GetModule<TestSystem>());
            Assert.AreEqual(1, architecture.EnumerateModules<ISystem>().Count());
            CollectionAssert.Contains(TestArchitecture.Log, "system:shutdown");
            CollectionAssert.Contains(TestArchitecture.Log, "replacement:init");
        }

        [Test]
        public void RegisterModule_FailureRollsBackRegistryAndArchitectureReference()
        {
            TestArchitecture.Initialize();
            var failing = new FailingSystem();

            Assert.Throws<InvalidOperationException>(() => TestArchitecture.Instance.RegisterModule(failing));

            Assert.IsFalse(TestArchitecture.Instance.ContainsModule<FailingSystem>());
        }

        [Test]
        public void Initialize_FailureClearsSingletonAndAlreadyInitializedModules()
        {
            TestArchitecture.FailInitialization = true;

            Assert.Throws<InvalidOperationException>(TestArchitecture.Initialize);

            Assert.IsFalse(TestArchitecture.IsInitialized);
            Assert.IsFalse(TestArchitecture.TryGetInstance(out _));
            CollectionAssert.Contains(TestArchitecture.Log, "service:shutdown");
        }

        [Test]
        public void CommandsQueriesAndEventsUseUnifiedApi()
        {
            TestArchitecture.Initialize();
            var architecture = TestArchitecture.Instance;
            var eventValue = 0;
            architecture.SubscribeEvent<int>(value => eventValue = value);

            architecture.ExecuteCommand(new PublishCommand(7));
            var result = architecture.ExecuteQuery<DoubleQuery, int>(new DoubleQuery(6));

            Assert.AreEqual(7, eventValue);
            Assert.AreEqual(12, result);
        }

        [Test]
        public void ModuleBase_DoesNotExposeArchitectureToDerivedModules()
        {
            var exposed = typeof(ModuleBase).GetMembers(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(member => member is FieldInfo field && field.IsFamily || member is PropertyInfo property && property.GetMethod?.IsFamily == true)
                .Where(member => member is FieldInfo field && typeof(IArchitecture).IsAssignableFrom(field.FieldType) || member is PropertyInfo property && typeof(IArchitecture).IsAssignableFrom(property.PropertyType));

            Assert.IsEmpty(exposed);
        }

        [Test]
        public void SystemAccessor_RequiresExplicitCapability()
        {
            Assert.IsFalse(typeof(ISystemAccessor).IsAssignableFrom(typeof(ISystem)));
            Assert.IsTrue(typeof(ISystemAccessor).IsAssignableFrom(typeof(SystemWithSystemAccessor)));
        }

        private sealed class TestArchitecture : Architecture<TestArchitecture>
        {
            internal static readonly List<string> Log = new List<string>();
            internal static bool FailInitialization;

            internal static void Reset()
            {
                Log.Clear();
                FailInitialization = false;
            }

            protected override void OnInitialize()
            {
                RegisterModule(new TestService());
                RegisterModule(new TestModel());
                RegisterModule(new TestSystem("system"));
                if (FailInitialization)
                {
                    RegisterModule(new FailingSystem());
                }
            }

            protected override void OnShutdown() { }
        }

        private abstract class TestModuleBase : ModuleBase
        {
            private readonly string _name;
            protected TestModuleBase(string name) => _name = name;
            protected override void OnInitialize() => TestArchitecture.Log.Add(_name + ":init");
            protected override void OnShutdown() => TestArchitecture.Log.Add(_name + ":shutdown");
        }

        private sealed class TestService : TestModuleBase, IService
        {
            internal TestService() : base("service") { }
        }

        private sealed class TestModel : TestModuleBase, IModel
        {
            internal TestModel() : base("model") { }
        }

        private sealed class TestSystem : TestModuleBase, ISystem, IPreShutdownModule
        {
            internal TestSystem(string name) : base(name) { }
            void IPreShutdownModule.PreShutdown() => TestArchitecture.Log.Add("pre");
        }

        private sealed class FailingSystem : ModuleBase, ISystem
        {
            protected override void OnInitialize() => throw new InvalidOperationException();
        }

        private sealed class SystemWithSystemAccessor : ModuleBase, ISystem, ISystemAccessor { }

        private readonly struct PublishCommand : ICommand
        {
            private readonly int _value;
            internal PublishCommand(int value) => _value = value;
            void ICommand.Execute(ICommandContext context) => context.PublishEvent(_value);
        }

        private readonly struct DoubleQuery : IQuery<int>
        {
            private readonly int _value;
            internal DoubleQuery(int value) => _value = value;
            int IQuery<int>.Execute(IQueryContext context) => _value * 2;
        }
    }
}
