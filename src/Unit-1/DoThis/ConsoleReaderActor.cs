using System;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for reading FROM the console. 
    /// Also responsible for calling <see cref="ActorSystem.Shutdown"/>.
    /// </summary>
    class ConsoleReaderActor : UntypedActor
    {
        public const string ExitCommand = "exit";
        public const string StartCommand = "start";
        private IActorRef _consoleWriterActor;

        public ConsoleReaderActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            if (message.Equals(StartCommand))
            {
                DoPrintInstructions();
            }
            else if (message is Messages.InputError)
            {
                _consoleWriterActor.Tell(message as Messages.InputError);
            }
            GetAndValidateInput();
            
            // continue reading messages from the console
            Self.Tell(new Messages.ContinueProcessing());
        }

        private void GetAndValidateInput()
        {
            var message = Console.ReadLine();

            if (string.IsNullOrEmpty(message))
            {
                Self.Tell(new Messages.NullInputError("No input recieved."));
            }
            else if (message.Equals(ExitCommand))
            {
                Context.System.Shutdown();
            }
            else
            {
                var valid = IsVaid(message);

                if (valid)
                {
                    _consoleWriterActor.Tell(new Messages.InputSuccess("Thank you, message was valid."));
                    
                }
                else
                {
                    _consoleWriterActor.Tell(new Messages.InputError("Invalid: input had an odd number of characters."));
                }
                Self.Tell(new Messages.ContinueProcessing());
            }
        }

        private bool IsVaid(string message)
        {
            return message.Length % 2 == 0;
        }

        private void DoPrintInstructions()
        {
            Console.WriteLine("Write whatever you want into the console!");
            Console.WriteLine("Some entries will pass validation, and some won't...\n\n");
            Console.WriteLine("Type 'exit' to quit this application at any time.\n");
        }
    }
}