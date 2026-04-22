module Itr.Tests.Communication.TestHelpers

open System
open System.IO

/// Process-wide lock for tests that capture Console.Out.
/// Prevents parallel tests from interfering with each other's console capture.
let consoleLock = obj()

let captureOutput (f: unit -> unit) : string =
    lock consoleLock (fun () ->
        let sw = new StringWriter()
        let oldOut = Console.Out
        Console.SetOut(sw)
        try
            f ()
        finally
            Console.SetOut(oldOut)
        sw.ToString())
