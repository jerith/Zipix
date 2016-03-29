module ZipixTest.TestZipMangler

open System.IO
open FsCheck
open Zipix
open ZipixTest.Helpers


module ZM = ZipMangler


let zipRecords zipdata =
    new MemoryStream(zipdata, false)
    |> ZipFile.ofStream
    |> ZipFile.readRecords


let filterCFH records =
    let filterCFH' = function
        | ZipFile.LocalFile (_, st) ->
            st.Consume()
            false
        | ZipFile.CentralFile _ -> true
        | ZipFile.CentralEnd _ -> false
    records |> Seq.filter filterCFH'


let decodeFN flags filename =
    (match flags &&& ZipFile.ENCODING_FLAG_VALUE with
     | 0us -> ZipFile.stringOfBytes ZipFile.IBM437 filename
     | ZipFile.ENCODING_FLAG_VALUE ->
         ZipFile.stringOfBytes ZipFile.UTF8 filename
     | _ -> failwith "Impossible case.")


let printRecords records =
    let printRecord = function
        | ZipFile.LocalFile (h, st) ->
            printfn "LFH: %A" <| decodeFN h.flags h.filename
            st.Consume()
        | ZipFile.CentralFile h ->
            printfn "CFH: %x %x %A" h.versionMadeBy h.externalAttrs <| decodeFN h.flags h.filename
        | ZipFile.CentralEnd h -> printfn "CEH: %A" h
    records |> Seq.iter printRecord


let checkAttrs fperms dperms attrs =
    let perms attrs = (attrs &&& 0xffff0000u) >>> 16
    let isdir = attrs &&& 0x10u = 0x10u
    "file perms" @| (perms attrs = fperms && not isdir) .|.
    "dir perms" @| (perms attrs = dperms && isdir)


let checkCFHPermissions fperms dperms hostid = function
    | ZipFile.CentralFile h ->
        [checkAttrs fperms dperms h.externalAttrs
         sprintf "hostid %x" hostid @| (h.versionMadeBy &&& 0xff00us = hostid)]
    | _ -> failwith "Impossible case."


let checkNoPermissions =
    let check = checkCFHPermissions 0u 0u 0us
    filterCFH >> (Seq.map check) >> List.ofSeq


let checkPermissions =
    let check = checkCFHPermissions ZM.FILE_PERMS ZM.DIR_PERMS ZM.HOST_UNIX
    filterCFH >> (Seq.map check) >> List.ofSeq


[<ZipixProperty>]
let test_setUnixPermissions (ft: FileTree) (MaybeString zipComment) =
     "setUnixPermissions sets the unix permission flags" @|
     let zipdata = mkZip ft zipComment
     let before = zipRecords zipdata |> checkNoPermissions
     let after =
         zipRecords zipdata
         |> Seq.map ZM.setUnixPermissions
         |> checkPermissions
     if List.isEmpty before then ("empty zipfile" @| true)
     else ("before: no perms" @| before .&.
           "after: unix perms" @| after)
