module Zipix.ZipFile

open System.IO
open System.Text


let IBM437 = Encoding.GetEncoding("IBM437")
let UTF8 = Encoding.UTF8

[<Literal>]
let ENCODING_FLAG_VALUE = 0x0800us  // 1 <<< 11


module LocalFileHeader =
    [<Literal>]
    let SIGNATURE = 0x04034b50u

    type t = {
        // local file header signature     4 bytes  (0x04034b50)
        signature : uint32
        // version needed to extract       2 bytes
        versionExtract : uint16
        // general purpose bit flag        2 bytes
        flags : uint16
        // compression method              2 bytes
        compressionMethod : uint16
        // last mod file time              2 bytes
        modifiedTime : uint16
        // last mod file date              2 bytes
        modifiedDate : uint16
        // crc-32                          4 bytes
        crc32 : uint32
        // compressed size                 4 bytes
        sizeCompressed : uint32
        // uncompressed size               4 bytes
        sizeUncompressed : uint32
        // file name length                2 bytes
        fnLength : uint16
        // extra field length              2 bytes
        efLength : uint16

        // file name (variable size)
        filename : byte[]
        // extra field (variable size)
        extra : byte[]
        }

    let read signature (reader: BinaryReader) =
        if signature <> SIGNATURE then failwith "Invalid header signature"
        let h = {
            signature = signature
            versionExtract = reader.ReadUInt16()
            flags = reader.ReadUInt16()
            compressionMethod = reader.ReadUInt16()
            modifiedTime = reader.ReadUInt16()
            modifiedDate = reader.ReadUInt16()
            crc32 = reader.ReadUInt32()
            sizeCompressed = reader.ReadUInt32()
            sizeUncompressed = reader.ReadUInt32()
            fnLength = reader.ReadUInt16()
            efLength = reader.ReadUInt16()
            filename = null
            extra = null
            }
        { h with filename = reader.ReadBytes(int h.fnLength)
                 extra = reader.ReadBytes(int h.efLength) }

    let write (writer: BinaryWriter) h =
        writer.Write(h.signature)
        writer.Write(h.versionExtract)
        writer.Write(h.flags)
        writer.Write(h.compressionMethod)
        writer.Write(h.modifiedTime)
        writer.Write(h.modifiedDate)
        writer.Write(h.crc32)
        writer.Write(h.sizeCompressed)
        writer.Write(h.sizeUncompressed)
        writer.Write(h.fnLength)
        writer.Write(h.efLength)
        writer.Write(h.filename)
        writer.Write(h.extra)


module CentralFileHeader =
    [<Literal>]
    let SIGNATURE = 0x02014b50u


    type t = {
        // central file header signature   4 bytes  (0x02014b50)
        signature : uint32
        // version made by                 2 bytes
        versionMadeBy : uint16
        // version needed to extract       2 bytes
        versionExtract : uint16
        // general purpose bit flag        2 bytes
        flags : uint16
        // compression method              2 bytes
        compressionMethod : uint16
        // last mod file time              2 bytes
        modifiedTime : uint16
        // last mod file date              2 bytes
        modifiedDate : uint16
        // crc-32                          4 bytes
        crc32 : uint32
        // compressed size                 4 bytes
        sizeCompressed : uint32
        // uncompressed size               4 bytes
        sizeUncompressed : uint32
        // file name length                2 bytes
        fnLength : uint16
        // extra field length              2 bytes
        efLength : uint16
        // file comment length             2 bytes
        fcLength : uint16
        // disk number start               2 bytes
        diskNumberStart : uint16
        // internal file attributes        2 bytes
        internalAttrs : uint16
        // external file attributes        4 bytes
        externalAttrs : uint32
        // relative offset of local header 4 bytes
        relativeOffset : uint32

        // file name (variable size)
        filename : byte[]
        // extra field (variable size)
        extra : byte[]
        // file comment (variable size)
        comment : byte[]
        }


    let read signature (reader: BinaryReader) =
        if signature <> SIGNATURE then failwith "Invalid header signature"
        let h = {
            signature = signature
            versionMadeBy = reader.ReadUInt16()
            versionExtract = reader.ReadUInt16()
            flags = reader.ReadUInt16()
            compressionMethod = reader.ReadUInt16()
            modifiedTime = reader.ReadUInt16()
            modifiedDate = reader.ReadUInt16()
            crc32 = reader.ReadUInt32()
            sizeCompressed = reader.ReadUInt32()
            sizeUncompressed = reader.ReadUInt32()
            fnLength = reader.ReadUInt16()
            efLength = reader.ReadUInt16()
            fcLength = reader.ReadUInt16()
            diskNumberStart = reader.ReadUInt16()
            internalAttrs = reader.ReadUInt16()
            externalAttrs = reader.ReadUInt32()
            relativeOffset = reader.ReadUInt32()

            filename = null
            extra = null
            comment = null
        }
        { h with filename = reader.ReadBytes(int h.fnLength)
                 extra = reader.ReadBytes(int h.efLength)
                 comment = reader.ReadBytes(int h.fcLength) }


    let write (writer: BinaryWriter) h =
        writer.Write(h.signature)
        writer.Write(h.versionMadeBy)
        writer.Write(h.versionExtract)
        writer.Write(h.flags)
        writer.Write(h.compressionMethod)
        writer.Write(h.modifiedTime)
        writer.Write(h.modifiedDate)
        writer.Write(h.crc32)
        writer.Write(h.sizeCompressed)
        writer.Write(h.sizeUncompressed)
        writer.Write(h.fnLength)
        writer.Write(h.efLength)
        writer.Write(h.fcLength)
        writer.Write(h.diskNumberStart)
        writer.Write(h.internalAttrs)
        writer.Write(h.externalAttrs)
        writer.Write(h.relativeOffset)
        writer.Write(h.filename)
        writer.Write(h.extra)
        writer.Write(h.comment)


module CentralEndHeader =
    [<Literal>]
    let SIGNATURE = 0x06054b50u


    type t = {
        // end of central dir signature    4 bytes  (0x06054b50)
        signature : uint32
        // number of this disk             2 bytes
        diskNumber : uint16
        // number of the disk with the
        // start of the central directory  2 bytes
        startDiskNumber : uint16
        // total number of entries in the
        // central directory on this disk  2 bytes
        entryCountDisk : uint16
        // total number of entries in
        // the central directory           2 bytes
        entryCountTotal : uint16
        // size of the central directory   4 bytes
        sizeCentralDirectory : uint32
        // offset of start of central
        // directory with respect to
        // the starting disk number        4 bytes
        offsetCentralDirectory : uint32
        // .ZIP file comment length        2 bytes
        zcLength : uint16

        // .ZIP file comment       (variable size)
        comment : byte[]
        }


    let read signature (reader: BinaryReader) =
        if signature <> SIGNATURE then failwith "Invalid header signature"
        let h = {
            signature = signature
            diskNumber = reader.ReadUInt16()
            startDiskNumber = reader.ReadUInt16()
            entryCountDisk = reader.ReadUInt16()
            entryCountTotal = reader.ReadUInt16()
            sizeCentralDirectory = reader.ReadUInt32()
            offsetCentralDirectory = reader.ReadUInt32()
            zcLength = reader.ReadUInt16()

            comment = null
        }
        { h with comment = reader.ReadBytes(int h.zcLength) }


    let write (writer: BinaryWriter) h =
        writer.Write(h.signature)
        writer.Write(h.diskNumber)
        writer.Write(h.startDiskNumber)
        writer.Write(h.entryCountDisk)
        writer.Write(h.entryCountTotal)
        writer.Write(h.sizeCentralDirectory)
        writer.Write(h.offsetCentralDirectory)
        writer.Write(h.zcLength)
        writer.Write(h.comment)


let stringOfBytes (encoding: Encoding) bytes = encoding.GetString(bytes)
let bytesOfString (encoding: Encoding) (str: string) =
    encoding.GetBytes(match str with null -> "" | _ -> str)


type t = {
    stream : Stream
    reader : BinaryReader option
    writer : BinaryWriter option
    }


type zipheader =
    | H_LocalFile of LocalFileHeader.t
    | H_CentralFile of CentralFileHeader.t
    | H_CentralEnd of CentralEndHeader.t


let ofStream (stream: Stream) = {
    stream = stream
    reader = if stream.CanRead then Some (new BinaryReader(stream)) else None
    writer = if stream.CanWrite then Some (new BinaryWriter(stream)) else None
    }

let openFile path access =
    File.Open(path, FileMode.Open, access) |> ofStream

let openRead path = openFile path FileAccess.Read
let openReadWrite path = openFile path FileAccess.ReadWrite
let openWrite path =
    File.Open(path, FileMode.Create, FileAccess.Write) |> ofStream


let skip n zipfile =
    ignore <| zipfile.stream.Seek(n, SeekOrigin.Current)


let zipReader zipfile =
    match zipfile.reader with
    | None -> failwith "ZipFile is write-only."
    | Some reader -> reader


let zipWriter zipfile =
    match zipfile.writer with
    | None -> failwith "ZipFile is read-only."
    | Some writer -> writer


let readHeader zipfile =
    let reader = zipReader zipfile
    let sg = reader.ReadUInt32()
    match sg with
    | LocalFileHeader.SIGNATURE ->
        H_LocalFile <| LocalFileHeader.read sg reader
    | CentralFileHeader.SIGNATURE ->
        H_CentralFile <| CentralFileHeader.read sg reader
    | CentralEndHeader.SIGNATURE ->
        H_CentralEnd <| CentralEndHeader.read sg reader
    | _ -> failwith <| sprintf "Unexpected signature: %08x" sg


let rec readHeaders zipfile =
    match readHeader zipfile with
    | H_LocalFile h ->
        printfn "LFH: %08x %s" h.signature (stringOfBytes IBM437 h.filename)
        skip (int64 h.sizeCompressed) zipfile
        readHeaders zipfile
    | H_CentralFile h ->
        printfn "CFH: %08x %s" h.signature (stringOfBytes IBM437 h.filename)
        readHeaders zipfile
    | H_CentralEnd h ->
        printfn "CEH: %08x %s" h.signature (stringOfBytes IBM437 h.comment)
        let eof = zipfile.stream.Position = zipfile.stream.Length
        printfn "Done. (eof: %b)" eof


let writeHeader zipfile header =
    let writer = zipWriter zipfile
    match header with
    | H_LocalFile h -> LocalFileHeader.write writer h
    | H_CentralFile h -> CentralFileHeader.write writer h
    | H_CentralEnd h -> CentralEndHeader.write writer h


let copyBytesRW (reader: BinaryReader) (writer: BinaryWriter) length =
    let buf = Array.zeroCreate<byte> 4096
    let rec copyBytes' remaining =
        match remaining with
        | 0u -> ()
        | _ ->
            let read = reader.Read(buf, 0, min (int remaining) 4096)
            writer.Write(buf, 0, read)
            copyBytes' (remaining - uint32 read)
    copyBytes' length


let copyBytes zipIn zipOut length =
    match zipIn.reader, zipOut.writer with
    | Some reader, Some writer -> copyBytesRW reader writer length
    | _ -> failwith "Can't copy bytes."


type ZipFileStream(reader: BinaryReader, length: int64) =
    inherit Stream()

    let mutable position = 0L

    let unsup () = raise (System.NotSupportedException())

    // Inherited things we need to override.

    override this.CanRead = true
    override this.CanSeek = false
    override this.CanWrite = false

    override this.Length = length

    override this.Position
        with get() = position
        and set(_) = unsup ()

    override this.Flush() = ()

    override this.Read(buffer: byte[], offset: int, count: int) =
        let count = min (int64 count) (length - position) |> int
        let read = reader.Read(buffer, offset, count)
        position <- position + int64 read
        read

    override this.Seek(offset: int64, origin: SeekOrigin) = unsup ()
    override this.SetLength(value: int64) = unsup ()
    override this.Write(buffer: byte [], offset: int, count: int) = unsup ()

    // Our own things.

    member this.CopyTo(writer) =
        copyBytesRW reader writer <| uint32 (length - position)
        position <- length

    member this.CopyTo(zipfile) =
        this.CopyTo(zipWriter zipfile)


type ziprecord =
    | LocalFile of LocalFileHeader.t * ZipFileStream
    | CentralFile of CentralFileHeader.t
    | CentralEnd of CentralEndHeader.t


let rec readRecords zipIn = seq {
    match readHeader zipIn with
    | H_LocalFile h ->
        let st = new ZipFileStream(zipReader zipIn, int64 h.sizeCompressed)
        yield LocalFile (h, st)
        yield! readRecords zipIn
    | H_CentralFile h ->
        yield CentralFile h
        yield! readRecords zipIn
    | H_CentralEnd h ->
        yield CentralEnd h
        let eof = zipIn.stream.Position = zipIn.stream.Length
        if not eof then failwith "File continues past end of zip data."
    }


let writeRecords zipOut records =
    let writeRecord =
        function
        | LocalFile (h, st) ->
            writeHeader zipOut (H_LocalFile h)
            st.CopyTo(zipOut)
        | CentralFile h ->
            writeHeader zipOut (H_CentralFile h)
        | CentralEnd h ->
            writeHeader zipOut (H_CentralEnd h)
    records |> Seq.iter writeRecord


let copyRecords zipIn zipOut =
    readRecords zipIn
    |> writeRecords zipOut
