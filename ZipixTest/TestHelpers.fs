module ZipixTest.TestHelpers

open System.IO
open FsCheck
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
    "no '/' in basename" @| (String.forall ((<>) '/') fd.filename) .&.
    "valid encoded path" @| (ZF.stringOfBytes fd.encoding filename = path)


let arraylen16 = Array.length >> uint16
let arraylen32 = Array.length >> uint32


let fdMatchLFH (fd: FD.t) (lfh: LFH.t) =
    let f = sprintf "comparing field %s"
    [
        f "versionExtract" @| (lfh.versionExtract = fd.versionExtract)
        f "flags" @| (lfh.flags = fd.flags)
        f "compressionMethod" @| (lfh.compressionMethod = fd.compressionMethod)
        f "modifiedTime" @| (lfh.modifiedTime = fd.modifiedTime)
        f "modifiedDate" @| (lfh.modifiedDate = fd.modifiedDate)
        f "crc32" @| (lfh.crc32 = fd.crc32)
        f "sizeCompressed" @| (lfh.sizeCompressed = arraylen32 fd.data)
        f "sizeUncompressed" @| (lfh.sizeUncompressed = fd.sizeUncompressed)
        f "efLength" @| (lfh.efLength = arraylen16  fd.extra)
        f "extra" @| (lfh.extra = fd.extra)
        ]


let fdMatchCFH (fd: FD.t) (cfh: CFH.t) =
    let f = sprintf "comparing field %s"
    let fcBytes = ZF.bytesOfString fd.encoding fd.comment
    [
        f "versionMadeBy" @| (cfh.versionMadeBy = fd.versionMadeBy)
        f "versionExtract" @| (cfh.versionExtract = fd.versionExtract)
        f "flags" @| (cfh.flags = fd.flags)
        f "compressionMethod" @| (cfh.compressionMethod = fd.compressionMethod)
        f "modifiedTime" @| (cfh.modifiedTime = fd.modifiedTime)
        f "modifiedDate" @| (cfh.modifiedDate = fd.modifiedDate)
        f "crc32" @| (cfh.crc32 = fd.crc32)
        f "sizeCompressed" @| (cfh.sizeCompressed = arraylen32 fd.data)
        f "sizeUncompressed" @| (cfh.sizeUncompressed = fd.sizeUncompressed)
        f "efLength" @| (cfh.efLength = arraylen16 fd.extra)
        f "fcLength" @| (cfh.fcLength = arraylen16 fcBytes)
        f "internalAttrs" @| (cfh.internalAttrs = fd.internalAttrs)
        f "externalAttrs" @| (cfh.externalAttrs = fd.externalAttrs)
        f "extra" @| (cfh.extra = fd.extra)
        f "comment" @| (cfh.comment = fcBytes)
        ]


let isLFHValid ((path, fd: FD.t), lfh: LFH.t) =
    "LocalFileHeader is valid" @| [
        "valid LFH signature" @| (lfh.signature = LFH.SIGNATURE)
        "LFH fields match FD fields" @| (fdMatchLFH fd lfh)
        validEncodingFlag fd lfh.flags
        validEncodedPath path fd lfh.filename
        ]


let isCFHValid ((path, fd: FD.t), cfh: CFH.t) =
    "CentralFileHeader is valid" @| [
        "valid CFH signature" @| (cfh.signature = CFH.SIGNATURE)
        "CFH fields match FD fields" @| (fdMatchCFH fd cfh)
        validEncodingFlag fd cfh.flags
        validEncodedPath path fd cfh.filename
        ]


let ftNotEmpty (FileTree (_, subtree)) = subtree <> []


[<ZipixProperty(MaxTest=50, EndSize=50)>]
let test_mkLocalFileHeader (ft: FileTree) =
    "FileData.mkLocalFileHeader makes a valid LocalFileHeader" @|
    (ftNotEmpty ft ==>
     let flatFT = flattenWithPaths ft
     let lfhs = Seq.map (fun (p, fd) -> FD.mkLocalFileHeader p fd) flatFT
     Seq.zip flatFT lfhs |> List.ofSeq |> List.map isLFHValid)


[<ZipixProperty(MaxTest=50, EndSize=50)>]
let test_mkCentralFileHeader (ft: FileTree) =
    "FileData.mkCentralFileHeader makes a valid CentralFileHeader" @|
    (ftNotEmpty ft ==>
     let flatFT = flattenWithPaths ft
     // For now, we set the offset to zero.
     let cfhs = Seq.map (fun (p, fd) -> FD.mkCentralFileHeader p 0u fd) flatFT
     Seq.zip flatFT cfhs |> List.ofSeq |> List.map isCFHValid)
