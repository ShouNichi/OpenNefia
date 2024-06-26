using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using OpenNefia.Core.Utility;

namespace OpenNefia.Core.GameObjects
{
    /// <summary>
    /// Provides a central event bus that EntitySystems can subscribe to. This is the main way that
    /// EntitySystems communicate with each other.
    /// </summary>
    [PublicAPI]
    public interface IBroadcastEventBus
    {
        /// <summary>
        /// Subscribes an event handler for an event type.
        /// </summary>
        /// <typeparam name="T">Event type to subscribe to.</typeparam>
        /// <param name="source"></param>
        /// <param name="subscriber">Subscriber that owns the handler.</param>
        /// <param name="eventHandler">Delegate that handles the event.</param>
        void SubscribeBroadcastEvent<T>(
            IEntityEventSubscriber subscriber,
            BroadcastEventHandler<T> eventHandler,
            long priority = EventPriorities.Default)
            where T : notnull;

        void SubscribeBroadcastEvent<T>(
            IEntityEventSubscriber subscriber,
            BroadcastEventRefHandler<T> eventHandler,
            long priority = EventPriorities.Default)
            where T : notnull;

        /// <summary>
        /// Unsubscribes all event handlers of a given type.
        /// </summary>
        /// <typeparam name="T">Event type being unsubscribed from.</typeparam>
        /// <param name="subscriber">Subscriber that owns the handlers.</param>
        /// 
        void UnsubscribeEvent<T>(IEntityEventSubscriber subscriber) where T : notnull;

        /// <summary>
        /// Immediately raises an event onto the bus.
        /// </summary>
        /// <param name="toRaise">Event being raised.</param>
        /// 
        void RaiseEvent(object toRaise);

        void RaiseEvent<T>(T toRaise) where T : notnull;

        void RaiseEvent<T>(ref T toRaise) where T : notnull;

        /// <summary>
        /// Unsubscribes all event handlers for a given subscriber.
        /// </summary>
        /// <param name="subscriber">Owner of the handlers being removed.</param>
        void UnsubscribeEvents(IEntityEventSubscriber subscriber);
    }

    /// <summary>
    /// Implements the event broadcast functions.
    /// </summary>
    internal partial class EntityEventBus : IBroadcastEventBus
    {
        // Inside this class we pass a lot of things around as "ref Unit unitRef".
        // The idea behind this is to avoid using type arguments in core dispatch that only needs to pass around a ref*
        // Type arguments require the JIT to compile a new method implementation for every event type,
        // which would start to weigh a LOT.

        internal delegate void RefEventHandler(ref Unit ev);

        private readonly Dictionary<Type, List<Registration>> _eventSubscriptions = new();

        private readonly Dictionary<IEntityEventSubscriber, Dictionary<Type, Registration>> _inverseEventSubscriptions
            = new();

        private readonly HashSet<Type> _broadcastDirty = new();

        /// <inheritdoc />
        public void UnsubscribeEvents(IEntityEventSubscriber subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            if (!_inverseEventSubscriptions.TryGetValue(subscriber, out var val))
                return;

            // UnsubscribeEvent modifies _inverseEventSubscriptions, requires val to be cached
            foreach (var (type, tuple) in val.ToList())
            {
                UnsubscribeEvent(type, tuple, subscriber);
            }
        }

        /// <inheritdoc />
        public void SubscribeBroadcastEvent<T>(
            IEntityEventSubscriber subscriber,
            BroadcastEventHandler<T> eventHandler,
            long priority = EventPriorities.Default)
            where T : notnull
        {
            if (eventHandler == null)
                throw new ArgumentNullException(nameof(eventHandler));

            var order = new OrderingData(priority, _nextEventIndex++);

            SubscribeEventCommon<T>(subscriber, (ref Unit ev) => eventHandler(Unsafe.As<Unit, T>(ref ev)),
                eventHandler, order, false);
        }

        public void SubscribeBroadcastEvent<T>(IEntityEventSubscriber subscriber, BroadcastEventRefHandler<T> eventHandler,
            long priority = EventPriorities.Default) where T : notnull
        {
            var order = new OrderingData(priority, _nextEventIndex++);

            SubscribeEventCommon<T>(subscriber, (ref Unit ev) =>
            {
                ref var tev = ref Unsafe.As<Unit, T>(ref ev);
                eventHandler(ref tev);
            }, eventHandler, order, true);
        }

        private void SubscribeEventCommon<T>(
            IEntityEventSubscriber subscriber,
            RefEventHandler handler,
            object equalityToken,
            OrderingData order,
            bool byRef)
            where T : notnull
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            var eventType = typeof(T);

            var eventReference = eventType.HasCustomAttribute<ByRefEventAttribute>();

            if (eventReference != byRef)
                throw new InvalidOperationException(
                    $"Attempted to subscribe by-ref and by-value to the same broadcast event! event={eventType} eventIsByRef={eventReference} subscriptionIsByRef={byRef}");

            var subscriptionTuple = new Registration(handler, equalityToken, order, byRef);

            var subscriptions = _eventSubscriptions.GetOrInsertNew(eventType);
            if (!subscriptions.Any(p => p.Equals(subscriptionTuple)))
                subscriptions.Add(subscriptionTuple);

            var inverseSubscription = _inverseEventSubscriptions.GetOrInsertNew(subscriber);
            if (!inverseSubscription.ContainsKey(eventType))
                inverseSubscription.Add(eventType, subscriptionTuple);

            _broadcastDirty.Add(eventType);
            _eventTables.SetCompTypeDirty(eventType);
        }

        /// <inheritdoc />
        public void UnsubscribeEvent<T>(IEntityEventSubscriber subscriber) where T : notnull
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            var eventType = typeof(T);

            if (_inverseEventSubscriptions.TryGetValue(subscriber, out var inverse)
                && inverse.TryGetValue(eventType, out var tuple))
                UnsubscribeEvent(eventType, tuple, subscriber);
        }

        /// <inheritdoc />
        public void RaiseEvent(object toRaise)
        {
            var eventType = toRaise.GetType();
            ref var unitRef = ref ExtractUnitRef(ref toRaise, eventType);

            ProcessBroadcastEvent(ref unitRef, eventType, false);
        }

        public void RaiseEvent<T>(T toRaise) where T : notnull
        {
            ProcessBroadcastEvent(ref Unsafe.As<T, Unit>(ref toRaise), typeof(T), false);
        }

        public void RaiseEvent<T>(ref T toRaise) where T : notnull
        {
            ProcessBroadcastEvent(ref Unsafe.As<T, Unit>(ref toRaise), typeof(T), true);
        }

        private void UnsubscribeEvent(Type eventType, Registration tuple, IEntityEventSubscriber subscriber)
        {
            if (_eventSubscriptions.TryGetValue(eventType, out var subscriptions) && subscriptions.Contains(tuple))
                subscriptions.Remove(tuple);

            if (_inverseEventSubscriptions.TryGetValue(subscriber, out var inverse) && inverse.ContainsKey(eventType))
                inverse.Remove(eventType);

            _broadcastDirty.Add(eventType);
            _eventTables.SetCompTypeDirty(eventType);
        }

        private void ProcessBroadcastEvent(ref Unit unitRef, Type eventType, bool byRef)
        {
            if (_eventSubscriptions.TryGetValue(eventType, out var subs))
            {
                if (_broadcastDirty.Contains(eventType))
                {
                    subs.Sort();
                    _broadcastDirty.Remove(eventType);
                }

                foreach (var handler in subs)
                {
                    if (handler.ReferenceEvent != byRef)
                        ThrowByRefMisMatch(handler.ReferenceEvent);

                    handler.Handler(ref unitRef);
                }
            }
        }
        
        private const string ValueDispatchError = "Tried to dispatch a value event to a by-reference subscription.";
        private const string RefDispatchError = "Tried to dispatch a ref event to a by-value subscription.";

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowByRefMisMatch(bool subIsByRef)
        {
            if (subIsByRef)
                throw new InvalidOperationException(ValueDispatchError);
            else
                throw new InvalidOperationException(RefDispatchError);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Unit ExtractUnitRef(ref object obj, Type objType)
        {
            // If it's a boxed value type we have to do some trickery to return the INTERIOR reference,
            // not the reference to the boxed object.
            // Otherwise the unit points to the reference to the reference type.
            return ref objType.IsValueType
                ? ref Unsafe.As<object, UnitBox>(ref obj).Value
                : ref Unsafe.As<object, Unit>(ref obj);
        }

        private readonly struct Registration : IEquatable<Registration>, IComparable<Registration>
        {
            public readonly object EqualityToken;

            public readonly RefEventHandler Handler;
            public readonly OrderingData Ordering;
            public readonly bool ReferenceEvent;

            public Registration(
                RefEventHandler handler,
                object equalityToken,
                OrderingData ordering,
                bool referenceEvent)
            {
                Handler = handler;
                EqualityToken = equalityToken;
                Ordering = ordering;
                ReferenceEvent = referenceEvent;
            }

            public bool Equals(Registration other)
            {
                return Equals(EqualityToken, other.EqualityToken);
            }

            public override bool Equals(object? obj)
            {
                return obj is Registration other && Equals(other);
            }

            public int CompareTo(Registration other)
            {
                return Ordering.CompareTo(other.Ordering);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return EqualityToken.GetHashCode();
                }
            }

            public static bool operator ==(Registration left, Registration right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Registration left, Registration right)
            {
                return !left.Equals(right);
            }
        }

        // This is not a real type. Whenever you see a "ref Unit" it means it's a ref to *some* kind of other type.
        // It should always be cast to/from with Unsafe.As<,>
        internal readonly struct Unit
        {
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class UnitBox
        {
            [UsedImplicitly] public Unit Value;
        }
    }
}
