namespace WinTail
{
    using System.IO;
    using System.Text;

    using Akka.Actor;

    public class TailActor : UntypedActor
    {
        private readonly IActorRef reporterActor;

        private readonly string filePath;

        private FileObserver observer;

        private FileStream fileStream;

        private StreamReader fileStreamReader;

        public class FileWrite
        {
            private readonly string filePath;

            public FileWrite(string filePath)
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

        public class FileError
        {
            private readonly string filePath;

            private readonly string reason;

            public FileError(string filePath, string reason)
            {
                this.filePath = filePath;
                this.reason = reason;
            }

            public string FilePath
            {
                get
                {
                    return this.filePath;
                }
            }

            public string Reason
            {
                get
                {
                    return this.reason;
                }
            }
        }

        public class InitialRead
        {
            private readonly string filePath;

            private readonly string text;

            public InitialRead(string filePath, string text)
            {
                this.filePath = filePath;
                this.text = text;
            }

            public string FilePath
            {
                get
                {
                    return this.filePath;
                }
            }

            public string Text
            {
                get
                {
                    return this.text;
                }
            }
        }

        public TailActor(IActorRef reporterActor, string filePath)
        {
            this.reporterActor = reporterActor;
            this.filePath = filePath;

        }

        protected override void PostStop()
        {
            observer.Dispose();
            observer = null;
            fileStreamReader.Close();
            fileStreamReader.Dispose();
            base.PostStop();
        }

        protected override void PreStart()
        {

            this.observer = new FileObserver(this.Self, filePath);
            observer.Start();

            // open the file stream with shared read/write permissions (so file can be written to while open)
            this.fileStream = new FileStream(Path.GetFullPath(filePath), FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite);
            this.fileStreamReader = new StreamReader(this.fileStream, Encoding.UTF8);

            // read the initial contents of the file and send it to console as first message
            var text = this.fileStreamReader.ReadToEnd();

            this.Self.Tell(new InitialRead(filePath, text));
        }

        protected override void OnReceive(object message)
        {
            if (message is FileWrite)
            {
                // move file cursor forward
                // pull results from cursor to end of file and write to output
                // (tis is assuming a log file type format that is append-only)
                var text = this.fileStreamReader.ReadToEnd();
                if (!string.IsNullOrEmpty(text))
                {
                    this.reporterActor.Tell(text);
                }

            }
            else if (message is FileError)
            {
                var fe = message as FileError;
                this.reporterActor.Tell(string.Format("Tail error: {0}", fe.Reason));
            }
            else if (message is InitialRead)
            {
                var ir = message as InitialRead;
                this.reporterActor.Tell(ir.Text);
            }
        }
    }
}