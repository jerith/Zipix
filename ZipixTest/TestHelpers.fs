module ZipixTest.TestHelpers

open System.IO
open FsCheck
open FsCheck.NUnit
open Zipix
open ZipixTest.Helpers


module ZF = ZipFile
module LFH = ZF.LocalFileHeader
module CFH = ZF.CentralFileHeader

module FD = FileData



let validEncodingFlag (fd: FD.t) flags =
    "valid encoding flag" @|
    (match flags &&& ZF.ENCODING_FLAG_VALUE with
     | 0us -> fd.encoding = ZF.IBM437
     | ZF.ENCODING_FLAG_VALUE -> fd.encoding = ZF.UTF8
     | _ -> failwith "Impossible case.")


let validEncodedPath path (fd: FD.t) filename =
    "filename not empty" @| (String.length fd.filename > 0) .&.
    "valid encoded path" @| (ZF.stringOfBytes fd.encoding filename = path)


let isLFHValid ((path, fd: FD.t), lfh: LFH.t) =
    "LocalFileHeader is valid" @| [
        validEncodingFlag fd lfh.flags
        validEncodedPath path fd lfh.filename
        ]


let isCFHValid ((path, fd: FD.t), cfh: CFH.t) =
    "CentralFileHeader is valid" @| [
        validEncodingFlag fd cfh.flags
        validEncodedPath path fd cfh.filename
        ]


let ftNotEmpty (FileTree (_, subtree)) = subtree <> []


[<Property(Arbitrary=[|typeof<ZipGen.Arb>|], MaxTest=50)>]
let test_mkLocalFileHeader (ft: FileTree) =
    "FileData.mkLocalFileHeader makes a valid LocalFileHeader" @|
    (ftNotEmpty ft ==>
     let flatFT = flattenWithPaths ft
     let lfhs = Seq.map (fun (p, fd) -> FD.mkLocalFileHeader p fd) flatFT
     Seq.zip flatFT lfhs |> List.ofSeq |> List.map isLFHValid)


[<Property(Arbitrary=[|typeof<ZipGen.Arb>|], MaxTest=50)>]
let test_mkCentralFileHeader (ft: FileTree) =
    "FileData.mkCentralFileHeader makes a valid CentralFileHeader" @|
    (ftNotEmpty ft ==>
     let flatFT = flattenWithPaths ft
     // For now, we set the offset to zero.
     let cfhs = Seq.map (fun (p, fd) -> FD.mkCentralFileHeader p 0u fd) flatFT
     Seq.zip flatFT cfhs |> List.ofSeq |> List.map isCFHValid)
