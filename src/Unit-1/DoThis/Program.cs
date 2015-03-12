﻿using Akka.Actor;

namespace WinTail
{
    using Akka.Actor;

    #region Program
    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main(string[] args)
        {
            // initialize MyActorSystem
            MyActorSystem = ActorSystem.Create("MyActorSystem");

            // time to make your first actors!
            var consoleWriterProp = Props.Create(() => new ConsoleWriterActor());
            var consoleWriterActor = MyActorSystem.ActorOf(consoleWriterProp);
            var tailCoordinatorActor = MyActorSystem.ActorOf(Props.Create(() => new TailCoordinatorActor()), "tailCoordinatorActor");
            var validationActorProp = Props.Create(() => new FileValidationActor(consoleWriterActor));
            var validationActor = MyActorSystem.ActorOf(validationActorProp, "validationActor");
            var consoleReaderProp = Props.Create(() => new ConsoleReaderActor());
            var consoleReaderActor = MyActorSystem.ActorOf(consoleReaderProp);

            // tell console reader to begin
            consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.AwaitTermination();
        }
    }
    #endregion
}
