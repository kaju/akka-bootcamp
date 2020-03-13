open System
open Akka.FSharp
open WinTail

[<EntryPoint>]
let main argv =
    // initialize an actor system
    let myActorSystem = System.create "MyActorSystem" (Configuration.load ())

    // make your first actors using the 'spawn' function
    let consoleWriterActor = spawn myActorSystem "consoleWriterActor" (actorOf Actors.consoleWriterActor)
    let consoleReaderActor = spawn myActorSystem "consoleReaderActor" (actorOf2 (Actors.consoleReaderActor consoleWriterActor))

    // tell the consoleReader actor to begin
    consoleReaderActor <! Actors.Start

    myActorSystem.WhenTerminated.Wait ()
    0
