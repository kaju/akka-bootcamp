using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading;

using Akka.Actor;

namespace ChartApp.Actors
{
    public class PerformanceCounterActor : UntypedActor
    {
        private readonly string _seriesName;
        private readonly Func<PerformanceCounter> _performanceCounterGenerator;
        private PerformanceCounter _performanceCounter;

        private readonly HashSet<IActorRef> _subscriptions;

        private readonly CancellationTokenSource _cancelPublishing;

        public PerformanceCounterActor(string seriesName, Func<PerformanceCounter> performanceCounterGenerator)
        {
            this._seriesName = seriesName;
            this._performanceCounterGenerator = performanceCounterGenerator;
            this._subscriptions = new HashSet<IActorRef>();
            this._cancelPublishing = new CancellationTokenSource();
        }

        protected override void PreStart()
        {
            _performanceCounter = _performanceCounterGenerator();
            Context.System.Scheduler.Schedule(
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                Self,
                new GatherMetrics(),
                _cancelPublishing.Token);
        }

        protected override void PostStop()
        {
            try
            {
                _cancelPublishing.Cancel();
                _performanceCounter.Dispose();
            }catch{}
            finally {base.PostStop();}
        }

        protected override void OnReceive(object message)
        {
            if (message is GatherMetrics)
            {
                // publish latest counter value to all subscribers
                var metric = new Metric(_seriesName, _performanceCounter.NextValue());
                foreach(var sub in _subscriptions)
                    sub.Tell(metric);
            }
            else if (message is SubscribeCounter)
            {
                // add a subscription for this counter
                // (it's parent's job to filter by counter types)
                var sc = message as SubscribeCounter;
                _subscriptions.Add(sc.Subscriber);
            }
            else if (message is UnsubscribeCounter)
            {
                // remove a subscription from this counter
                var uc = message as UnsubscribeCounter;
                _subscriptions.Remove(uc.Subscriber);
            }
           
        }
    }
}