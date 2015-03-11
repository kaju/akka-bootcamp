﻿namespace WinTail
{
    using System;

    using Akka.Actor;

    public class TailCoordinatorActor : UntypedActor{
        protected override void OnReceive(object message)
        {
            if (message is StartTail)
            {
                var msg = message as StartTail;
                //propably create actor!
                Context.ActorOf(Props.Create(() => new TailActor(msg.Reporter, msg.FilePath)));
            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            int? retries = 10;
            TimeSpan? ts = TimeSpan.FromSeconds(30);

            return new OneForOneStrategy(retries, ts, x =>
                {
                    //Maybe we consider ArithmeticException to not be application critical
                    //so we just ignore the error and keep going.
                    if (x is ArithmeticException) return Directive.Resume;

                    //Error that we cannot recover from, stop the failing actor
                    else if (x is NotSupportedException) return Directive.Stop;

                    //In all other cases, just restart the failing actor
                    else return Directive.Restart;
                });
        }

        public class StopTail
        {
            private readonly string filePath;

            public StopTail(string filePath)
            {
                this.filePath = filePath;
            }

            public string FilePath
            {
                get
                {
                    return this.filePath;
                }
            }
        }

        public class StartTail
        {
            private readonly string filePath;

            private readonly IActorRef reporter;

            public StartTail(string filePath, IActorRef reporter)
            {
                this.filePath = filePath;
                this.reporter = reporter;
            }

            public string FilePath
            {
                get
                {
                    return this.filePath;
                }
            }

            public IActorRef Reporter
            {
                get
                {
                    return this.reporter;
                }
            }
        }
    }
}