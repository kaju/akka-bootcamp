open System
open Akka.Actor
open Akka.FSharp
open WinTail
open WinTail

[<EntryPoint>]
let main argv =
    // initialize an actor system
    let myActorSystem = System.create "MyActorSystem" (Configuration.load ())

    // make your first actors using the 'spawn' function
    let consoleWriterActor = spawn myActorSystem "consoleWriterActor" (actorOf Actors.consoleWriterActor)

    let strategy () = Strategy.OneForOne((fun ex ->
        match ex with
        | :? ArithmeticException  -> Directive.Resume
        | :? NotSupportedException -> Directive.Stop
        | _ -> Directive.Restart), 10, TimeSpan.FromSeconds(30.))

    let tailCoordinatorActor = spawnOpt myActorSystem "tailCoordinatorActor" (actorOf2 Actors.tailCoordinatorActor) [ SpawnOption.SupervisorStrategy(strategy ()) ]
    let fileValidatorActor = spawn myActorSystem "validationActor" (actorOf2 (Actors.fileValidatorActor consoleWriterActor))

    //let validationActor = spawn myActorSystem "validationActor" (actorOf2 (Actors.validationActor consoleWriterActor))
    let consoleReaderActor = spawn myActorSystem "consoleReaderActor" (actorOf2 Actors.consoleReaderActor)


    // tell the consoleReader actor to begin
    consoleReaderActor <! Actors.Start

    myActorSystem.WhenTerminated.Wait ()
    0
