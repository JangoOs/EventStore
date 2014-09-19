﻿using System;
using EventStore.Core.Data;
using EventStore.Core.Services.PersistentSubscription;
using EventStore.Core.Tests.Services.Replication;
using EventStore.Core.TransactionLog.LogRecords;
using NUnit.Framework;

namespace EventStore.Core.Tests.Services.PersistentSubscriptionTests
{
    [TestFixture]
    public class when_creating_persistent_subscription
    {
        private EventStore.Core.Services.PersistentSubscription.PersistentSubscription _sub;

        [TestFixtureSetUp]
        public void Setup()
        {
            _sub = new EventStore.Core.Services.PersistentSubscription.PersistentSubscription(new PersistentSubscriptionParams(true, "subId", "streamName", "groupName", true, true, TimeSpan.FromSeconds(5), false, new FakeEventLoader(x => { }), new FakeCheckpointReader(), new FakeCheckpointWriter(x => { })));
        }

        [Test]
        public void subscription_id_is_set()
        {
            Assert.AreEqual("subId", _sub.SubscriptionId);
        }

        [Test]
        public void stream_name_is_set()
        {
            Assert.AreEqual("streamName", _sub.EventStreamId);
        }

        [Test]
        public void group_name_is_set()
        {
            Assert.AreEqual("groupName", _sub.GroupName);
        }

        [Test]
        public void there_are_no_clients()
        {
            Assert.IsFalse(_sub.HasClients);
            Assert.AreEqual(0, _sub.ClientCount);
        }

        [Test]
        public void null_checkpoint_reader_throws_argument_null()
        {
            Assert.Throws<ArgumentNullException>(() => new Core.Services.PersistentSubscription.PersistentSubscription(new PersistentSubscriptionParams(true, "subId", "streamName", "groupName", true, true, TimeSpan.FromSeconds(5), false, new FakeEventLoader(x => { }), null, new FakeCheckpointWriter(x => { }))));
        }

        [Test]
        public void null_checkpoint_writer_throws_argument_null()
        {
            Assert.Throws<ArgumentNullException>(() => new Core.Services.PersistentSubscription.PersistentSubscription(new PersistentSubscriptionParams(true, "subId", "streamName", "groupName", true, true, TimeSpan.FromSeconds(5), false, new FakeEventLoader(x => { }), new FakeCheckpointReader(), null)));
        }


        [Test]
        public void null_event_reader_throws_argument_null()
        {
            Assert.Throws<ArgumentNullException>(() => new Core.Services.PersistentSubscription.PersistentSubscription(new PersistentSubscriptionParams(true, "subId", "streamName", "groupName", true, true, TimeSpan.FromSeconds(5), false, null, new FakeCheckpointReader(), new FakeCheckpointWriter(X => { }))));
        }

        [Test]
        public void null_subid_throws_argument_null()
        {
            Assert.Throws<ArgumentNullException>(() =>  new Core.Services.PersistentSubscription.PersistentSubscription(new PersistentSubscriptionParams(true, null, "streamName", "groupName", true, true, TimeSpan.FromSeconds(5), false, new FakeEventLoader(x => { }), new FakeCheckpointReader(), new FakeCheckpointWriter(X => { }))));
        }


        [Test]
        public void null_stream_throws_argument_null()
        {
            Assert.Throws<ArgumentNullException>(() => new Core.Services.PersistentSubscription.PersistentSubscription(new PersistentSubscriptionParams(true, "subid", null, "groupName", true, true, TimeSpan.FromSeconds(5), false, new FakeEventLoader(x => { }), new FakeCheckpointReader(), new FakeCheckpointWriter(X => { }))));
        }

        [Test]
        public void null_groupname_throws_argument_null()
        {
            Assert.Throws<ArgumentNullException>(() => new Core.Services.PersistentSubscription.PersistentSubscription(new PersistentSubscriptionParams(true, "subid", "stream", null, true, true, TimeSpan.FromSeconds(5), false, new FakeEventLoader(x => { }), new FakeCheckpointReader(), new FakeCheckpointWriter(X => { }))));
        }

    }

    [TestFixture]
    public class LiveTests
    {
        [Test]
        public void live_subscription_pushes_events_to_client()
        {
            var envelope = new FakeEnvelope();
            var sub = new EventStore.Core.Services.PersistentSubscription.PersistentSubscription(new PersistentSubscriptionParams(true, "subId", "streamName", "groupName", false, true, TimeSpan.FromSeconds(5), false, new FakeEventLoader(x => { }), new FakeCheckpointReader(), new FakeCheckpointWriter(x => { })));
            sub.AddClient(Guid.NewGuid(), Guid.NewGuid(),envelope, 10, "foo", "bar");
            sub.NotifyLiveSubscriptionMessage(Helper.BuildFakeEvent(Guid.NewGuid(), "type", "streamName", 0));
            Assert.AreEqual(1,envelope.Replies.Count);
        }

        [Test]
        public void live_subscription_with_round_robin_two_pushes_events_to_both()
        {
            var envelope1 = new FakeEnvelope();
            var envelope2 = new FakeEnvelope();
            var sub = new EventStore.Core.Services.PersistentSubscription.PersistentSubscription(new PersistentSubscriptionParams(true, "subId", "streamName", "groupName", false, true, TimeSpan.FromSeconds(5), false, new FakeEventLoader(x => { }), new FakeCheckpointReader(), new FakeCheckpointWriter(x => { })));
            sub.AddClient(Guid.NewGuid(), Guid.NewGuid(), envelope1, 10, "foo", "bar");
            sub.AddClient(Guid.NewGuid(), Guid.NewGuid(), envelope2, 10, "foo", "bar");
            sub.NotifyLiveSubscriptionMessage(Helper.BuildFakeEvent(Guid.NewGuid(), "type", "streamName", 0));
            sub.NotifyLiveSubscriptionMessage(Helper.BuildFakeEvent(Guid.NewGuid(), "type", "streamName", 1));
            Assert.AreEqual(1, envelope1.Replies.Count);
            Assert.AreEqual(1, envelope2.Replies.Count);
        }

        [Test]
        public void live_subscription_with_prefer_one_and_two_pushes_events_to_both()
        {
            var envelope1 = new FakeEnvelope();
            var envelope2 = new FakeEnvelope();
            var sub = new EventStore.Core.Services.PersistentSubscription.PersistentSubscription(new PersistentSubscriptionParams(true, "subId", "streamName", "groupName", false, true, TimeSpan.FromSeconds(5), true, new FakeEventLoader(x => { }), new FakeCheckpointReader(), new FakeCheckpointWriter(x => { })));
            sub.AddClient(Guid.NewGuid(), Guid.NewGuid(), envelope1, 10, "foo", "bar");
            sub.AddClient(Guid.NewGuid(), Guid.NewGuid(), envelope2, 10, "foo", "bar");
            sub.NotifyLiveSubscriptionMessage(Helper.BuildFakeEvent(Guid.NewGuid(), "type", "streamName", 0));
            sub.NotifyLiveSubscriptionMessage(Helper.BuildFakeEvent(Guid.NewGuid(), "type", "streamName", 1));
            Assert.AreEqual(2, envelope1.Replies.Count);
        }


        [Test]
        public void subscription_with_pull_sends_data_to_client()
        {
            var envelope1 = new FakeEnvelope();
            var sub = new EventStore.Core.Services.PersistentSubscription.PersistentSubscription(new PersistentSubscriptionParams(true, "subId", "streamName", "groupName", true, true, TimeSpan.FromSeconds(5), false, new FakeEventLoader(x => { }), new FakeCheckpointReader(), new FakeCheckpointWriter(x => { })));
            sub.AddClient(Guid.NewGuid(), Guid.NewGuid(), envelope1, 10, "foo", "bar");
            sub.HandleReadCompleted(new [] {Helper.BuildFakeEvent(Guid.NewGuid(), "type", "streamName", 0)}, 1);
            Assert.AreEqual(1, envelope1.Replies.Count);
        }


        [Test]
        public void subscription_with_pull_and_round_robin_set_and_two_clients_sends_data_to_client()
        {
            var envelope1 = new FakeEnvelope();
            var envelope2 = new FakeEnvelope(); 
            var sub = new EventStore.Core.Services.PersistentSubscription.PersistentSubscription(new PersistentSubscriptionParams(true, "subId", "streamName", "groupName", true, true, TimeSpan.FromSeconds(5), false, new FakeEventLoader(x => { }), new FakeCheckpointReader(), new FakeCheckpointWriter(x => { })));
            sub.AddClient(Guid.NewGuid(), Guid.NewGuid(), envelope1, 10, "foo", "bar");
            sub.AddClient(Guid.NewGuid(), Guid.NewGuid(), envelope2, 10, "foo", "bar");
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            sub.HandleReadCompleted(new[]
            {
                Helper.BuildFakeEvent(id1, "type", "streamName", 0),
                Helper.BuildFakeEvent(id2, "type", "streamName", 1)
            }, 1);
            Assert.AreEqual(1, envelope1.Replies.Count);
            Assert.AreEqual(1,envelope2.Replies.Count);
        }


        [Test]
        public void subscription_with_pull_and_prefer_on_set_and_two_clients_sends_data_to_one_client()
        {
            var envelope1 = new FakeEnvelope();
            var envelope2 = new FakeEnvelope();
            var sub = new Core.Services.PersistentSubscription.PersistentSubscription(new PersistentSubscriptionParams(true, "subId", "streamName", "groupName", true, true, TimeSpan.FromSeconds(5), true, new FakeEventLoader(x => { }), new FakeCheckpointReader(), new FakeCheckpointWriter(x => { })));
            sub.AddClient(Guid.NewGuid(), Guid.NewGuid(), envelope1, 10, "foo", "bar");
            sub.AddClient(Guid.NewGuid(), Guid.NewGuid(), envelope2, 10, "foo", "bar");
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            sub.HandleReadCompleted(new[]
            {
                Helper.BuildFakeEvent(id1, "type", "streamName", 0),
                Helper.BuildFakeEvent(id2, "type", "streamName", 1)
            }, 1);
            Assert.AreEqual(2, envelope1.Replies.Count);
        }
    }

    public class Helper
    {
        public static ResolvedEvent BuildFakeEvent(Guid id, string type, string stream, int version)
        {
            return
                new ResolvedEvent(new EventRecord(version, 1234567, Guid.NewGuid(), id, 1234567, 1234, stream, version,
                    DateTime.Now, PrepareFlags.SingleWrite, type, new byte[0], new byte[0]));
        }
    }

    [TestFixture]
    public class AddingClientTests
    {
        [Test]
        public void adding_a_client_adds_the_client()
        {
            var sub = new Core.Services.PersistentSubscription.PersistentSubscription(new PersistentSubscriptionParams(true, "subId", "streamName", "groupName", true, true, TimeSpan.FromSeconds(5), false, new FakeEventLoader(x => { }), new FakeCheckpointReader(), new FakeCheckpointWriter(x => { })));
            sub.AddClient(Guid.NewGuid(), Guid.NewGuid(), new FakeEnvelope(), 1, "foo", "bar");
            Assert.IsTrue(sub.HasClients);
            Assert.AreEqual(1, sub.ClientCount);
        }
    }


    class FakeEventLoader : IPersistentSubscriptionEventLoader
    {
        private readonly Action<int> _action;

        public FakeEventLoader(Action<int> action)
        {
            _action = action;
        }

        public void BeginLoadState(Core.Services.PersistentSubscription.PersistentSubscription subscription, int startEventNumber, int countToLoad, Action<ResolvedEvent[], int> onFetchCompleted)
        {
            _action(startEventNumber);
        }
    }

    class FakeCheckpointReader : IPersistentSubscriptionCheckpointReader
    {
        private Action<int?> _onStateLoaded;

        public void BeginLoadState(string subscriptionId, Action<int?> onStateLoaded)
        {
            _onStateLoaded = onStateLoaded;
        }

        public void Load(int? state)
        {
            _onStateLoaded(state);
        }
    }

    class FakeCheckpointWriter : IPersistentSubscriptionCheckpointWriter
    {
        private readonly Action<int> _action;

        public FakeCheckpointWriter(Action<int> action)
        {
            _action = action;
        }

        public void BeginWriteState(int state)
        {
            _action(state);
        }
    }
}