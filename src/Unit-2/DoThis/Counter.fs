namespace ChartApp

open System
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Microcharts

[<AutoOpen>]
module Binding =
    module MicrochartControl =

        let create (attrs: IAttr<MicrochartControl> list) : IView<MicrochartControl> =
            ViewBuilder.Create<MicrochartControl>(attrs)

    type MicrochartControl with
        static member chart<'t when 't :> MicrochartControl>(value: Chart) : IAttr<'t> =
            let getter : ('t -> Chart) = (fun control -> control.Chart)
            let setter : ('t * Chart -> unit) = (fun (control, value) -> control.Chart <- value)

            AttrBuilder<'t>.CreateProperty<Chart>("Chart", value, ValueSome getter, ValueSome setter, ValueNone)


module Counter =
    open Avalonia.Controls
    open Avalonia.Layout

    type Stats = {
        timeStamp : int64
        value : float32
    }

    type State = {
        current : DateTimeOffset
        count : int
        cpuStats : List<Stats>
    }

    let now () =
        DateTimeOffset.UtcNow.ToUnixTimeSeconds()

    let init =
        let now = now ()
        {
            current = DateTimeOffset.UtcNow
            count = 0
            cpuStats = seq { for _i in 1 .. 100 -> 0.0f } |>  Seq.toList |> List.mapi (fun i v -> { timeStamp = now - int64(i); value = v })
        }

    let update (msg: Msg) (state: State) : State =
        match msg with
        | Increment -> { state with count = state.count + 1 ; cpuStats = state.cpuStats @ [ { timeStamp = now () ; value = (float32 (state.count))} ] |> List.tail }
        | Decrement -> { state with count = state.count - 1 ; cpuStats = state.cpuStats @ [ { timeStamp = now () ; value = (float32 (state.count))} ] |> List.tail }
        | SetCount count  -> { state with count = count }
        | Tick current -> { state with current = current }
        | Reset -> init

    let view (state: State) (dispatch) =
        DockPanel.create [

            DockPanel.children [
                Button.create [
                    Button.dock Dock.Bottom
                    Button.onClick (fun _ -> dispatch Reset)
                    Button.content "reset"
                ]
                Button.create [
                    Button.dock Dock.Bottom
                    Button.onClick (fun _ -> dispatch Decrement)
                    Button.content "-"
                ]
                Button.create [
                    Button.dock Dock.Bottom
                    Button.onClick (fun _ -> dispatch Increment)
                    Button.content "+"
                ]
                Button.create [
                    Button.dock Dock.Bottom
                    Button.onClick ((fun _ -> state.count * 2 |> SetCount |> dispatch), SubPatchOptions.OnChangeOf state.count)
                    Button.content "x2"
                ]
                TextBox.create [
                    TextBox.dock Dock.Bottom
                    TextBox.onTextChanged ((fun text ->
                        let isNumber, number = System.Int32.TryParse text
                        if isNumber then
                            number |> SetCount |> dispatch)
                    )
                    TextBox.text (string state.count)
                ]
                TextBlock.create [
                    TextBlock.dock Dock.Top
                    TextBlock.fontSize 24.0
                    TextBlock.verticalAlignment VerticalAlignment.Center
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.text (string state.current)
                ]
                TextBlock.create [
                    TextBlock.dock Dock.Top
                    TextBlock.fontSize 24.0
                    TextBlock.verticalAlignment VerticalAlignment.Center
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.text (string state.count)
                ]
                MicrochartControl.create [
                    MicrochartControl.dock Dock.Top
                    MicrochartControl.chart (
                        let entries = state.cpuStats |> Seq.map(fun x -> Entry ( Value = x.value ))
                        let chart = LineChart()
                        chart.Entries <- entries
                        chart.MaxValue <- 1.0f
                        chart.MinValue <- 0.0f
                        chart.LineMode <- LineMode.Straight
                        chart.PointMode <- PointMode.None
                        chart
                    )
                ]
            ]
        ]