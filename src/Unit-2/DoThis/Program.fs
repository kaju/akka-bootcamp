namespace ChartApp

open System
open Akka.Configuration
open Avalonia
open Elmish
open Avalonia.FuncUI.Components.Hosts
open Avalonia.FuncUI
open Avalonia.FuncUI.Elmish
open Avalonia.Controls.ApplicationLifetimes
open Akka.Actor
open Akka.FSharp
open Akka.Configuration.Hocon

type MainWindow() as this =
    inherit HostWindow()
    do
        base.Title <- "Counter Example"
        base.Height <- 400.0
        base.Width <- 400.0

        let config = System.IO.File.ReadAllText("app.conf")
        let conf = ConfigurationFactory.ParseString(config)

        let myActorSystem = System.create "ChartActors" conf
                //let timerActor = spawn

        let timer initial =
            let sub dispatch =
                spawn
                    myActorSystem
                    "timerActor"
                    (Actors.timerActor dispatch) |> ignore
                ()
            Cmd.ofSub sub

        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
        Elmish.Program.mkSimple (
            fun () ->
            Counter.init)
            Counter.update
            Counter.view
        |> Program.withHost this
        |> Program.withConsoleTrace
        |> Program.withSubscription timer
        |> Program.run

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Load "resm:Avalonia.Themes.Default.DefaultTheme.xaml?assembly=Avalonia.Themes.Default"
        this.Styles.Load "resm:Avalonia.Themes.Default.Accents.BaseDark.xaml?assembly=Avalonia.Themes.Default"

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            let mainWindow = MainWindow()
            desktopLifetime.MainWindow <- mainWindow
        | _ -> ()

module Program =

    [<EntryPoint>]
    let main(args: string[]) =

        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)