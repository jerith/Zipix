module Zipix.Program

open Argu
open ZipFile
open ZipMangler


/// Define our command line args.
type Args =
    | [<AltCommandLine("-i")>] Input_File of string
    | [<AltCommandLine("-o")>] Output_File of string
    | [<AltCommandLine("-p")>] Exec_Pattern of string
    | [<AltCommandLine("-s")>] Exec_Suffix of string
    | [<AltCommandLine("-d")>] Exec_Dir of string
    | [<AltCommandLine("-v")>] Verbose
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Input_File _ -> "existing zipfile to read from."
            | Output_File _ -> "new zipfile to write to."
            | Exec_Pattern _ -> "any file matching this regex is exectuable."
            | Exec_Suffix _ -> "any file with this suffix is exectuable."
            | Exec_Dir _ -> "any file in this (sub)directory is executable."
            | Verbose _ -> "emit all sorts of verbosity."


let parser = ArgumentParser.Create<Args>()


[<EntryPoint>]
let main argv =
    let args = parser.Parse(argv, errorHandler=ProcessExiter())
    let zipIn = ZipFile.openRead <| args.GetResult <@ Input_File @>
    let zipOut = ZipFile.openWrite <| args.GetResult <@ Output_File @>
    let patterns =
        List.collect id [
            List.map matchesPattern <| args.GetResults <@ Exec_Pattern @>
            List.map hasSuffix <| args.GetResults <@ Exec_Suffix @>
            List.map hasParentDir <| args.GetResults <@ Exec_Dir @>
            ]
    let processor = setUnixPermissions >> (setExecutable patterns)
    let processor =
        if (args.Contains <@ Verbose @>) then verboseWrapper processor else processor
    readRecords zipIn
    |> Seq.map processor
    |> writeRecords zipOut
    0 // return an integer exit code
