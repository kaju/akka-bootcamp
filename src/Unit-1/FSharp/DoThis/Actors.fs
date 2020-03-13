namespace WinTail

open System
open Akka.Actor
open Akka.FSharp
open Messages

module Actors =
    type Command =
    | Start
    | Continue
    | Message of string
    | Exit

    let doPrintInstructions () =
        printfn "Write whatever you want into the console!"
        printfn "Some entries will pass validation, and some won't..."
        printfn "Type 'exit' to quit this application at any time."

    let (|Message|Exit|) (str:string) =
        match str.ToLower() with
        | "exit" -> Exit
        | _ -> Message(str)

    let (|EmptyMessage|MessageLengthIsEven|MessageLengthIsOdd|) (msg:string) =
        match msg.Length, msg.Length % 2 with
        | 0, _ -> EmptyMessage
        | _, 0 -> MessageLengthIsEven
        | _, _ -> MessageLengthIsOdd

    let consoleReaderActor (consoleWriter: IActorRef) (mailbox: Actor<_>) message =
        let getAndValidateInput () =
            let line = Console.ReadLine ()
            match line with
            | Exit -> mailbox.Context.System.Terminate() |> ignore
            | Message(input) ->
                match input with
                | EmptyMessage -> mailbox.Self <! InputError ("No input received", ErrorType.Null)
                | MessageLengthIsEven ->
                    consoleWriter <! InputSuccess ("Thank you the input is valid!")
                    mailbox.Self <! Continue
                //  | MessageLengthIsOdd ->
                | _ -> mailbox.Self <! InputError ("The message is invalid (odd number of characters)!", ErrorType.Validation)

        match box message with
        | :? Command as command ->
            match command with
            | Start -> doPrintInstructions ()
            | _ -> ()
        | :? InputResult as inputResult ->
            match inputResult with
            | InputError (_, _) as error -> consoleWriter <! error
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

