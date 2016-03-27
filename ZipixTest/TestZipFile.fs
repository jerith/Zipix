module ZipixTest.TestZipFile

open System.IO
open FsCheck
open FsCheck.NUnit
open Zipix.ZipFile
open ZipixTest.Helpers


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
