module WinTail.Messages

open Akka.Actor

type ErrorType =
| Null
| Validation

type InputResult =
| InputSuccess of string
| InputError of reason:string * errorType:ErrorType

type TailCommand =
| StartTail of filePath:string * reporterActor:IActorRef
| StopTail of filePath:string

type FileCommand =
| FileWrite of fileName:string
| FileError of fileName:string * reason:string
| InitialRead of fileName:string * text:string