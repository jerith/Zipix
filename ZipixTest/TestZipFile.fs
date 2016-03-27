module ZipixTest.TestZipFile

open System.IO
open FsCheck
open FsCheck.NUnit
open Zipix.ZipFile


module FileData =
    module LFH = LocalFileHeader
    module CFH = CentralFileHeader

    type t = {
        encoding : System.Text.Encoding
        filename : string
        data : byte[]
        modifiedTime : uint16
        modifiedDate : uint16
        crc32 : uint32
        sizeUncompressed : uint32
        externalAttrs : uint32
        versionMadeBy : uint16

        internalAttrs : uint16
        versionExtract : uint16
        flags : uint16
        compressionMethod : uint16
        extra : byte[]
        comment : string
        }

    let mkLocalFileHeader path fd =
        let filename =
            String.concat "/" [path; fd.filename]
            |> bytesOfString fd.encoding
        {
            LFH.signature = LFH.SIGNATURE
            LFH.versionExtract = fd.versionExtract
            LFH.flags = fd.flags
            LFH.compressionMethod = fd.compressionMethod
            LFH.modifiedTime = fd.modifiedTime
            LFH.modifiedDate = fd.modifiedDate
            LFH.crc32 = fd.crc32
            LFH.sizeCompressed = Array.length fd.data |> uint32
            LFH.sizeUncompressed = fd.sizeUncompressed
            LFH.fnLength = Array.length filename |> uint16
            LFH.efLength = Array.length fd.extra |> uint16
            LFH.filename = filename
            LFH.extra = fd.extra
            }

    let mkCentralFileHeader path offset fd =
        let filename =
            String.concat "/" [path; fd.filename]
            |> bytesOfString fd.encoding
        {
            CFH.signature = CFH.SIGNATURE
            CFH.versionMadeBy = fd.versionMadeBy
            CFH.versionExtract = fd.versionExtract
            CFH.flags = fd.flags
            CFH.compressionMethod = fd.compressionMethod
            CFH.modifiedTime = fd.modifiedTime
            CFH.modifiedDate = fd.modifiedDate
            CFH.crc32 = fd.crc32
            CFH.sizeCompressed = Array.length fd.data |> uint32
            CFH.sizeUncompressed = fd.sizeUncompressed
            CFH.fnLength = Array.length filename |> uint16
            CFH.efLength = Array.length fd.extra |> uint16
            CFH.fcLength = String.length fd.comment |> uint16
            CFH.diskNumberStart = 0us
            CFH.internalAttrs = fd.internalAttrs
            CFH.externalAttrs = fd.externalAttrs
            CFH.relativeOffset = offset
            CFH.filename = filename
            CFH.extra = fd.extra
            CFH.comment = bytesOfString fd.encoding fd.comment
            }


type FTEncoding =
    | FTE_UTF8
    | FTE_IBM437

type FileSubTree =
    | FT_Directory of FileData.t * FileSubTree list
    | FT_File of FileData.t

type FileTree = FileTree of FTEncoding * FileSubTree list

type MaybeString = MaybeString of string

module ZipGen =

    let genEncodedString =
        function
        | FTE_UTF8 -> Arb.generate<string>
        | FTE_IBM437 ->
            Gen.choose (1, 255)
            |> Gen.map byte
            |> Gen.arrayOf
            |> Gen.map (stringOfBytes IBM437)

    let genFilename enc =
        Gen.choose (1, 255)
        |> Gen.map byte
        |> Gen.suchThat ((<>) 0x2fuy)
        |> Gen.arrayOf
        |> Gen.suchThat (Array.length >> (<) 0)
        |> Gen.map (stringOfBytes IBM437)

    let genMaybeString enc =
        Gen.oneof [Gen.constant ""; genEncodedString enc]

    let genMaybeBytes =
        Gen.oneof [Gen.constant Array.empty<byte>; Arb.generate<byte[]>]

    let genSharedData enc = gen {
        let encoding, flags =
            match enc with
            | FTE_UTF8 -> (UTF8, uint16 (1 <<< 11))
            | FTE_IBM437 -> (IBM437, 0x0000us)
        let! modifiedTime = Arb.generate<uint16>
        let! modifiedDate = Arb.generate<uint16>
        let! filename = genFilename enc
        let! extra = genMaybeBytes
        let! dosAttrs = Gen.elements [0x00uy; 0x20uy]
        let externalAttrs = uint32 dosAttrs
        let! comment = genMaybeString enc
        return {
            FileData.encoding = encoding
            FileData.filename = filename
            FileData.data = Array.empty<byte>
            FileData.modifiedTime = modifiedTime
            FileData.modifiedDate = modifiedDate
            FileData.crc32 = 0u
            FileData.sizeUncompressed = 0u
            FileData.externalAttrs = externalAttrs
            FileData.versionMadeBy = 0x0000us

            FileData.internalAttrs = 0x0000us
            FileData.versionExtract = 0x0014us
            FileData.flags = flags
            FileData.compressionMethod = 0x0008us
            FileData.extra = extra
            FileData.comment = comment
            }
        }

    let genFileData enc = gen {
        let! shared = genSharedData enc
        let! crc32 = Arb.generate<uint32>
        let! data = Arb.generate<byte[]>
        let sizeCompressed = Array.length data
        let! sizeDiff = Gen.choose (-sizeCompressed/10, sizeCompressed)
        let sizeUncompressed = sizeCompressed + sizeDiff
        return {
            shared with
                data = data
                crc32 = crc32
                sizeUncompressed = sizeUncompressed |> uint32
            }
        }

    let genDirData enc = gen {
        let! shared = genSharedData enc
        let externalAttrs = shared.externalAttrs ||| 0x20u
        return { shared with externalAttrs = externalAttrs }
        }

    let genFileTree enc =
        let mkdir dd st =
            FT_Directory (dd, st)
        let genF enc =
            genFileData enc |> Gen.map FT_File
        let rec genST enc s =
            genFT enc (s/2) |> Gen.listOf |> Gen.resize (s/2)
        and genFT enc s =
            match s with
            | 0 -> genF enc
            | s -> Gen.oneof [ genF enc;
                               Gen.map2 mkdir (genDirData enc) (genST enc s) ]
        let mkGenFT basegen =
            Gen.sized (basegen enc) |> Gen.map (fun ftl -> FileTree (enc, ftl))
        Gen.oneof [ mkGenFT genST
                    mkGenFT (fun enc -> genFT enc >> Gen.map (fun x -> [x])) ]

    // Some boilerplate so we can use this in attributes.
    type Arb =
        static member MaybeString =
            Arb.fromGen <| Gen.map MaybeString (genMaybeString FTE_IBM437)
        static member FileTree =
            Arb.fromGen (Arb.generate<FTEncoding> >>= genFileTree)


let rec walkFST fFile fDir fst =
    let recurse = walkFST fFile fDir
    match fst with
    | FT_File fd -> fFile fd
    | FT_Directory (dd, fsts) -> fDir (dd, fsts |> Seq.map recurse)

let walkFT fFile fDir fRoot (FileTree (enc, fsts)) =
    fsts |> Seq.map (walkFST fFile fDir) |> fRoot


let flattenWithPaths ft =
    let fFile (fd: FileData.t) = Seq.singleton (fd.filename, fd)
    let fDir (fd: FileData.t, sts) =
        let sts = Seq.append (Seq.singleton ("", fd)) (Seq.concat sts)
        Seq.map (fun (p, x) -> (String.concat "/" [fd.filename; p]), x) sts
    let fRoot = Seq.concat
    walkFT fFile fDir fRoot ft


let listPaths ft =
    let fFile (fd: FileData.t) = Seq.singleton fd.filename
    let fDir (fd: FileData.t, childPaths) =
        let childPaths = Seq.append (Seq.singleton "") (Seq.concat childPaths)
        Seq.map (fun s -> String.concat "/" [fd.filename; s]) childPaths
    let fRoot = Seq.concat
    walkFT fFile fDir fRoot ft


let dataSize ft =
    let extrasize (fd: FileData.t) =
        30 + 46 + (String.length fd.filename * 2) + (Array.length fd.extra * 2)
    let fFile (fd: FileData.t) = Array.length fd.data + (extrasize fd)
    let fDir (fd: FileData.t, childSums) =
        Seq.append (Seq.singleton (extrasize fd)) childSums |> Seq.sum
    let fRoot = Seq.sum
    walkFT fFile fDir fRoot ft


let mkZip ft comment =
    use ms = new MemoryStream()
    use writer = new BinaryWriter(ms)
    // Write the local file data and return the associated central header. We
    // do it this way because the central header needs to know the offset of
    // the local header in the zipfile.
    let writeLocal (path, fd) =
        let offset = writer.BaseStream.Position |> uint32
        LocalFileHeader.write writer <| FileData.mkLocalFileHeader path fd
        writer.Write(fd.data)
        FileData.mkCentralFileHeader path offset fd

    let centralHeaders = flattenWithPaths ft |> Seq.map writeLocal
    let centralOffset = writer.BaseStream.Position |> uint32
    let entryCount = Seq.length centralHeaders |> uint16
    centralHeaders |> Seq.iter (CentralFileHeader.write writer)
    let centralSize = (writer.BaseStream.Position |> uint32) - centralOffset
    let centralEnd = {
        CentralEndHeader.signature = CentralEndHeader.SIGNATURE
        CentralEndHeader.diskNumber = 0us
        CentralEndHeader.startDiskNumber = 0us
        CentralEndHeader.entryCountDisk = entryCount
        CentralEndHeader.entryCountTotal = entryCount
        CentralEndHeader.sizeCentralDirectory = centralSize
        CentralEndHeader.offsetCentralDirectory = centralOffset
        CentralEndHeader.zcLength = String.length comment |> uint16
        CentralEndHeader.comment = bytesOfString IBM437 comment
        }
    CentralEndHeader.write writer centralEnd
    writer.Close()
    ms.ToArray()


// [<Property(Arbitrary=[|typeof<ZipGen.Arb>|], MaxTest=2, EndSize=10)>]
[<Property(Arbitrary=[|typeof<ZipGen.Arb>|])>]
let testLocalFile (ft: FileTree) (MaybeString zipComment) =
    let zipdata = mkZip ft zipComment
    let zipIn = ofStream <| new MemoryStream(zipdata, false)
    use ms = new MemoryStream()
    let zipOut = ofStream ms

    copyRecords zipIn zipOut
    ms.Close()
    let copiedZip = ms.ToArray()

    zipdata = copiedZip
