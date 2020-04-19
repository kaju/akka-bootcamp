namespace ChartApp

open System

[<AutoOpen>]
module Messages =

    type Msg =
        | Increment
        | Decrement
        | SetCount of int
        | Tick of DateTimeOffset
        | Reset
