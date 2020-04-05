module Program

open System
open Gtk
open Akka.Actor
open Akka.FSharp
open Akka.Configuration.Hocon
open System.Configuration
open ChartApp

    [<EntryPoint>]
    let main argv =
        let chartActors = System.create "ChartActors" (Configuration.load ())

        Application.Init()

        let app = new Application("org.akkabootcamp.chartapp", GLib.ApplicationFlags.None)
        app.Register(GLib.Cancellable.Current) |> ignore;

        let win = new MainWindow()
        app.AddWindow(win)

        win.Show()
        Application.Run()
        0