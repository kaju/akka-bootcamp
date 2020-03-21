namespace WinTail

open System
open System.IO
open Akka.Actor
open Akka.FSharp
open Messages

module Actors =
    type Command =
    | Start
    | Continue
    | Message of string
    | Exit

    let validationActor (consoleWriter:IActorRef) (mailbox:Actor<_>) message =
        let (|EmptyMessage|MessageLengthIsEven|MessageLengthIsOdd|) (msg:string) =
            match msg.Length, msg.Length % 2 with
            | 0, _ -> EmptyMessage
            | _, 0 -> MessageLengthIsEven
            | _, _ -> MessageLengthIsOdd

        match message with
            | EmptyMessage -> consoleWriter <! InputError ("No input received", ErrorType.Null)
            | MessageLengthIsEven ->
                consoleWriter <! InputSuccess ("Thank you the input is valid!")
            //  | MessageLengthIsOdd ->
            | _ -> consoleWriter <! InputError ("The message is invalid (odd number of characters)!", ErrorType.Validation)
        mailbox.Sender () <! Continue

    let consoleReaderActor (mailbox: Actor<_>) message =
        let doPrintInstructions () =
             printfn "Please provide the URI of a log file on disk."

        let (|Message|Exit|) (str:string) =
            match str.ToLower() with
            | "exit" -> Exit
            | _ -> Message(str)

        let getAndValidateInput () =
            let line = Console.ReadLine ()
            match line with
            | Exit -> mailbox.Context.System.Terminate() |> ignore
            | line -> select "/user/validationActor" mailbox.Context.System <! line

        match box message with
        | :? Command as command ->
            match command with
            | Start -> doPrintInstructions ()
            | _ -> ()
        | _ -> ()

        getAndValidateInput()

    let consoleWriterActor message =
        let printInColor color message =
            Console.ForegroundColor <- color
            Console.WriteLine (message.ToString ())
            Console.ResetColor ()

        match box message with
        | :? InputResult as inputResult ->
            match inputResult with
            | InputError (reason, _) -> printInColor ConsoleColor.Red reason
            | InputSuccess text -> printInColor ConsoleColor.Green text
        | _ -> printInColor ConsoleColor.Yellow (message.ToString())

    let fileValidatorActor (consoleWriter:IActorRef) (mailbox:Actor<_>) message =
        let (|IsFileUri|_|) path = if File.Exists path then Some path else None

        let (|EmptyMessage|Message|) (msg:string) =
            match msg.Length with
            | 0 -> EmptyMessage
            | _ -> Message msg

        match message with
        | EmptyMessage ->
            consoleWriter <! InputError("Input was blank. Please try again.\n", ErrorType.Null)
            mailbox.Sender () <! Continue
        | IsFileUri _ ->
            consoleWriter <! InputSuccess(sprintf "Starting processing for %s" message)
            select "/user/tailCoordinatorActor" mailbox.Context.System <! StartTail(message, consoleWriter)
        | _ ->
            consoleWriter <! InputError (sprintf "%s is not an existing URI on disk." message, ErrorType.Validation)
            mailbox.Sender () <! Continue

    let tailActor (filePath:string) (reporter:IActorRef) (mailbox:Actor<_>) =
        let observer = new FileObserver(mailbox.Self, Path.GetFullPath(filePath))
        do observer.Start ()
        let fileStream = new FileStream(Path.GetFullPath(filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        let fileStreamReader = new StreamReader(fileStream, Text.Encoding.UTF8)
        let text = fileStreamReader.ReadToEnd ()
        do mailbox.Self <! InitialRead(filePath, text)

        let rec loop() = actor {
            let! message = mailbox.Receive()
            match (box message) :?> FileCommand with
            | FileWrite(_) ->
                let text = fileStreamReader.ReadToEnd ()
                if not <| String.IsNullOrEmpty text then reporter <! text else ()
            | FileError(_, reason) -> reporter <! sprintf "Tail error: %s" reason
            | InitialRead(_, text) -> reporter <! text
            return! loop()
        }

        loop()

    let tailCoordinatorActor (mailbox:Actor<_>) message =
        match message with
        | StartTail(filePath,reporter) -> spawn mailbox.Context "tailActor" (tailActor filePath reporter) |> ignore
        | _ -> ()
