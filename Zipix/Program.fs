module Zipix.Program



[<EntryPoint>]
let main argv =
    let [| pathIn; pathOut |] = argv
    let zipIn = ZipFile.openRead pathIn
    let zipOut = ZipFile.openWrite pathOut
    // ZipFile.readHeaders zipIn
    ZipFile.copyRecords zipIn zipOut
    0 // return an integer exit code
