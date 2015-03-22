using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;

using Akka.Actor;

namespace ChartApp.Actors
{
    public class PerformanceCounterCoordinatorActor : ReceiveActor
    {
        public class Watch
        {
            public CounterType Counter { get; private set; }

            public Watch(CounterType counter)
            {
                this.Counter = counter;
            }
        }

        public class Unwatch
        {
            public CounterType Counter { get; private set; }

            public Unwatch(CounterType counter)
            {
                this.Counter = counter;
            }
        }

        /// <summary>
        /// Methods for generating new instances of all <see cref="PerformanceCounter"/>s we want to monitor
        /// </summary>
        private static readonly Dictionary<CounterType, Func<PerformanceCounter>> CounterGenerators =
            new Dictionary<CounterType, Func<PerformanceCounter>>()
        {
            {CounterType.Cpu, () => new PerformanceCounter("Processor", "% Processor Time", "_Total", true)},
            {CounterType.Memory, () => new PerformanceCounter("Memory", "% Committed Bytes In Use", true)},
            {CounterType.Disk, () => new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true)},
        };

        /// <summary>
        /// Methods for creating new <see cref="Series"/> with distinct colors and names
        /// corresponding to each <see cref="PerformanceCounter"/>
        /// </summary>
        private static readonly Dictionary<CounterType, Func<Series>> CounterSeries =
            new Dictionary<CounterType, Func<Series>>()
        {
            {CounterType.Cpu, () =>
            new Series(CounterType.Cpu.ToString()){ ChartType = SeriesChartType.SplineArea,
             Color = Color.DarkGreen}},
            {CounterType.Memory, () =>
            new Series(CounterType.Memory.ToString()){ ChartType = SeriesChartType.FastLine,
            Color = Color.MediumBlue}},
            {CounterType.Disk, () =>
            new Series(CounterType.Disk.ToString()){ ChartType = SeriesChartType.SplineArea,
            Color = Color.DarkRed}},
        };


        private Dictionary<CounterType, IActorRef> counterActors;

        private IActorRef chartingActor;

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor) :
            this(chartingActor, new Dictionary<CounterType, IActorRef>())
        {
        }

        private PerformanceCounterCoordinatorActor(IActorRef chartingActor, Dictionary<CounterType, IActorRef> actorRefs)
        {
            this.chartingActor = chartingActor;
            this.counterActors = actorRefs;

            this.Receive<Watch>(
                watch =>
                    {
                        if (!counterActors.ContainsKey(watch.Counter))
                        {
                            // create a child actor to monitor this counter if one doesn't exist already
                            var counterActor = Context.ActorOf(Props.Create(() =>
                                new PerformanceCounterActor(watch.Counter.ToString(), CounterGenerators[watch.Counter])));

                            // add this counter actor to our index
                            counterActors[watch.Counter] = counterActor;
                        }

                        // register this series with the ChartingActor
                        chartingActor.Tell(new ChartingActor.AddSeries(CounterSeries[watch.Counter]()));

                        // tell the counter actor to begin publishing its statistics to the _chartingActor
                        counterActors[watch.Counter].Tell(new SubscribeCounter(watch.Counter, chartingActor));
                    });

            this.Receive<Unwatch>(
                unwatch =>
                {
                    if (counterActors.ContainsKey(unwatch.Counter))
                    {
                        // add this counter actor to our index
                        counterActors[unwatch.Counter].Tell(new UnsubscribeCounter(unwatch.Counter, chartingActor));
                        chartingActor.Tell(new ChartingActor.RemoveSeries(unwatch.Counter.ToString()));
                    }
                });
        }
    }
}