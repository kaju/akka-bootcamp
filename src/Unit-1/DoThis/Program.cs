﻿using Akka.Actor;

namespace WinTail
{
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
            var validationActorProp = Props.Create(() => new ValidationActor(consoleWriterActor));
            var validationActor = MyActorSystem.ActorOf(validationActorProp);
            var consoleReaderProp = Props.Create(() => new ConsoleReaderActor(validationActor));
            var consoleReaderActor = MyActorSystem.ActorOf(consoleReaderProp);

            // tell console reader to begin
            consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.AwaitTermination();
        }
    }
    #endregion
}
