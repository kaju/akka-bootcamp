namespace WinTail
{
    using System.IO;

    using Akka.Actor;

    public class FileValidationActor: UntypedActor{
        private readonly ActorRef consoleWriterActor;

        private readonly ActorRef tailCoordinatorActor;

        public FileValidationActor(ActorRef consoleWriterActor, ActorRef tailCoordinatorActor)
        {
            this.consoleWriterActor = consoleWriterActor;
            this.tailCoordinatorActor = tailCoordinatorActor;
        }

        protected override void OnReceive(object message)
        {
            var msg = message as string;
            if (string.IsNullOrEmpty(msg))
            {
                // signal that the user needs to supply an input
                consoleWriterActor.Tell(new Messages.NullInputError("No input received."));
                // tell sender to continue doing its thing (whatever that may be, this actor doesn't care)
                Sender.Tell(new Messages.ContinueProcessing());
            }
            else
            {
                var valid = IsFileUri(msg);
                if (valid)
                {
                    consoleWriterActor.Tell(new Messages.InputSuccess(string.Format("Starting processing for {0}", msg)));

                    // start coordinator
                    tailCoordinatorActor.Tell(new TailCoordinatorActor.StartTail(msg, consoleWriterActor));
                }
                else
                {
                    // signal that input was bad
                    consoleWriterActor.Tell(new Messages.ValidationInputError(string.Format("{0} is not an existing URI on disk.", msg)));
                    // tell sender to continue doing its thing (whatever that may be, this actor doesn't care)
                    Sender.Tell(new Messages.ContinueProcessing());
                }
            }
        }

        private bool IsFileUri(string msg)
        {
            return File.Exists(msg);
        }
    }
}