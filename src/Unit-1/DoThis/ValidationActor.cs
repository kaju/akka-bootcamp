namespace WinTail
{
    using Akka.Actor;

    public class ValidationActor: UntypedActor{
        private readonly ActorRef consoleWriterActor;

        public ValidationActor(ActorRef consoleWriterActor)
        {
            this.consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            var msg = message as string;
            if (string.IsNullOrEmpty(msg))
            {
                // signal that the user needs to supply an input
                consoleWriterActor.Tell(new Messages.NullInputError("No input received."));
            }
            else
            {
                var valid = IsValid(msg);
                if (valid)
                {
                    // send success to console writer
                    consoleWriterActor.Tell(new Messages.InputSuccess("Thank you! Message was valid."));
                }
                else
                {
                    // signal that input was bad
                    consoleWriterActor.Tell(new Messages.ValidationInputError("Invalid: input had odd number of characters."));
                }
            }

            // tell sender to continue doing its thing (whatever that may be, this actor doesn't care)
            Sender.Tell(new Messages.ContinueProcessing());
        }

        private bool IsValid(string message)
        {
            return message.Length % 2 == 0;
        }

    }
}