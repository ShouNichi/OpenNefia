﻿using NUnit.Framework;
using OpenNefia.Core.ContentPack;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Serialization.Manager;
using OpenNefia.Core.Serialization.Markdown.Mapping;
using OpenNefia.Core.Serialization.Markdown.Value;
using System.Reflection;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace OpenNefia.Tests.Core.Serialization.TypeSerializers
{
    public delegate string TestDelegate(int foo);

    public sealed class TestSystem : EntitySystem
    {
        public int Invocations { get; private set; } = 0;

        public string TestCallback(int foo)
        {
            Invocations++;
            return $"{foo}";
        }

        public string TestCallback_WrongArgs()
        {
            return string.Empty;
        }

        public void TestCallback_WrongRetval(int foo)
        {
        }
    }
    
    [TestFixture]
    public class DelegateSerializerTest : SerializationTest
    {
        protected override IEnumerable<Type> ExtraSystemTypes => new[] { typeof(TestSystem) };

        [Test]
        public void DeserializationTest()
        {
            var node = new ValueDataNode("OpenNefia.Tests.Core.Serialization.TypeSerializers.TestSystem:TestCallback");

            var sys = EntitySystem.Get<TestSystem>();
            var invocations = sys.Invocations;
            var deserializedDelegate = Serialization.Read<TestDelegate>(node);
            Assert.That(deserializedDelegate!.Invoke(42), Is.EqualTo("42"));
            Assert.That(sys.Invocations, Is.EqualTo(invocations + 1));
        }

        [Test]
        public void DeserializationTestMapping()
        {
            var node = new MappingDataNode();
            node["system"] = new ValueDataNode("OpenNefia.Tests.Core.Serialization.TypeSerializers.TestSystem");
            node["method"] = new ValueDataNode("TestCallback");

            var sys = EntitySystem.Get<TestSystem>();
            var invocations = sys.Invocations;
            var deserializedDelegate = Serialization.Read<TestDelegate>(node);
            Assert.That(deserializedDelegate!.Invoke(42), Is.EqualTo("42"));
            Assert.That(sys.Invocations, Is.EqualTo(invocations + 1));
        }
    }
}