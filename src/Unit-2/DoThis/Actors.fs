namespace ChartApp
//
//open System.Collections.Generic
//open System.Windows.Forms.DataVisualization.Charting
open System
open Akka.Actor
open Akka.FSharp


[<AutoOpen>]
module Actors =
    type Messages =
        | Ping

    let timerActor (dispatch:(Msg->unit)) (mailbox: Actor<_>) =

        mailbox.Context.System.Scheduler.ScheduleTellRepeatedly(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1.0),
            mailbox.Self, Ping ,
            ActorRefs.NoSender)

        let rec loop() = actor {
            let! message = mailbox.Receive ()
            match message with
            | Ping _ ->
                dispatch (Tick DateTimeOffset.UtcNow) |> ignore

            return! loop ()
        }
        loop ()