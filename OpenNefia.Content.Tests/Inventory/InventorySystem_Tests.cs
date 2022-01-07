﻿using NUnit.Framework;
using OpenNefia.Content.Equipment;
using OpenNefia.Content.Inventory;
using OpenNefia.Core.Containers;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.Maps;
using OpenNefia.Core.Maths;
using OpenNefia.Core.Prototypes;
using OpenNefia.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.Tests.Inventory
{
    [TestFixture]
    [TestOf(typeof(InventorySystem))]
    public class InventorySystem_Tests
    {
        private static readonly PrototypeId<EquipSlotPrototype> TestSlot1ID = new("TestSlot1");
        private static readonly PrototypeId<EquipSlotPrototype> TestSlot2ID = new("TestSlot2");

        private static readonly string Prototypes = @$"
- type: Elona.EquipSlot
  id: {TestSlot1ID}
- type: Elona.EquipSlot
  id: {TestSlot2ID}
";


        private ISimulation SimulationFactory()
        {
            var sim = ContentFullGameSimulation
                .NewSimulation()
                .RegisterPrototypes(protoMan => protoMan.LoadString(Prototypes))
                .InitializeInstance();

            return sim;
        }

        [Test]
        public void TestInitializeEquipSlots()
        {
            var sim = SimulationFactory();

            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var invSys = sim.GetEntitySystem<InventorySystem>();

            var map = sim.CreateMapAndSetActive(10, 10);

            var ent = entMan.SpawnEntity(null, map.AtPos(Vector2i.One));

            Assert.That(entMan.HasComponent<InventoryComponent>(ent), Is.False);
            Assert.That(entMan.HasComponent<ContainerManagerComponent>(ent), Is.False);

            List<PrototypeId<EquipSlotPrototype>> equipSlotProtos = new() 
            { 
                TestSlot1ID,
                TestSlot2ID,
                TestSlot2ID,
            };

            invSys.InitializeEquipSlots(ent, equipSlotProtos);

            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<InventoryComponent>(ent), Is.True);
                Assert.That(entMan.HasComponent<ContainerManagerComponent>(ent), Is.True);

                Assert.That(invSys.TryGetEquipSlots(ent, out var equipSlots), Is.True);
                Assert.That(equipSlots!.Count, Is.EqualTo(3));

                Assert.That(equipSlots[0].ID, Is.EqualTo(TestSlot1ID));
                Assert.That(equipSlots[1].ID, Is.EqualTo(TestSlot2ID));
                Assert.That(equipSlots[2].ID, Is.EqualTo(TestSlot2ID));

                Assert.That((string)equipSlots[0].ContainerID, Is.EqualTo($"Elona.EquipSlot:TestSlot1:0"));
                Assert.That((string)equipSlots[1].ContainerID, Is.EqualTo($"Elona.EquipSlot:TestSlot2:0"));
                Assert.That((string)equipSlots[2].ContainerID, Is.EqualTo($"Elona.EquipSlot:TestSlot2:1"));
            });
        }

        [Test]
        public void TestAddEquipSlots()
        {
            var sim = SimulationFactory();

            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var containerSys = sim.GetEntitySystem<ContainerSystem>();
            var invSys = sim.GetEntitySystem<InventorySystem>();

             var map = sim.CreateMapAndSetActive(10, 10);

            var ent = entMan.SpawnEntity(null, map.AtPos(Vector2i.One));

            Assert.That(entMan.HasComponent<InventoryComponent>(ent), Is.False);
            Assert.That(entMan.HasComponent<ContainerManagerComponent>(ent), Is.False);
            var inventory = entMan.EnsureComponent<InventoryComponent>(ent);
            var containers = entMan.EnsureComponent<ContainerManagerComponent>(ent);

            Assert.Multiple(() =>
            {
                var oldContainerCount = containers.Containers.Count;

                Assert.That(invSys.TryAddEquipSlot(ent, TestSlot1ID, out var containerSlot, out var equipSlot), Is.True);
                Assert.That(invSys.HasEquipSlot(ent, TestSlot1ID), Is.True);

                Assert.That(inventory.EquipSlots!.Count, Is.EqualTo(1), "Equip slot count");
                Assert.That(containers.Containers.Count, Is.EqualTo(oldContainerCount + 1), "Container count");

                Assert.That(equipSlot!.ID, Is.EqualTo(TestSlot1ID));
                Assert.That((string)equipSlot.ContainerID, Is.EqualTo($"Elona.EquipSlot:TestSlot1:0"));
                Assert.That(containerSys.HasContainer(ent, equipSlot.ContainerID), Is.True);
            });
        }

        [Test]
        public void TestRemoveEquipSlots()
        {
            var sim = SimulationFactory();

            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var containerSys = sim.GetEntitySystem<ContainerSystem>();
            var invSys = sim.GetEntitySystem<InventorySystem>();

            var map = sim.CreateMapAndSetActive(10, 10);

            var ent = entMan.SpawnEntity(null, map.AtPos(Vector2i.One));
            var entItem = entMan.SpawnEntity(null, map.AtPos(Vector2i.One));

            Assert.That(entMan.HasComponent<InventoryComponent>(ent), Is.False);
            Assert.That(entMan.HasComponent<ContainerManagerComponent>(ent), Is.False);

            List<PrototypeId<EquipSlotPrototype>> equipSlotProtos = new()
            {
                TestSlot1ID,
                TestSlot2ID,
                TestSlot2ID,
            };

            var inventory = entMan.EnsureComponent<InventoryComponent>(ent);
            var containers = entMan.EnsureComponent<ContainerManagerComponent>(ent);

            invSys.InitializeEquipSlots(ent, equipSlotProtos);

            var equipSlot = inventory.EquipSlots[0];
            Assert.That(invSys.TryGetContainerForEquipSlot(ent, equipSlot, out var container), Is.True);

            container!.Insert(entItem);

            Assert.Multiple(() =>
            {
                var oldContainerCount = containers.Containers.Count;

                Assert.That(invSys.TryRemoveEquipSlot(ent, equipSlot), Is.True);
                Assert.That(invSys.HasEquipSlot(ent, TestSlot1ID), Is.False);

                // Equipment in the slot should be deleted.
                Assert.That(entMan.EntityExists(entItem), Is.False);

                Assert.That(inventory.EquipSlots!.Count, Is.EqualTo(2), "Equip slot count");
                Assert.That(containers.Containers.Count, Is.EqualTo(oldContainerCount - 1), "Container count");

                Assert.That(invSys.TryRemoveEquipSlot(ent, equipSlot), Is.False);
            });
        }
    }
}