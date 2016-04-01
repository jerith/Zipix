module Zipix.ZipMangler


open System.Text.RegularExpressions
open Zipix.ZipFile

module CFH = CentralFileHeader


let DIR_PERMS = 0o040755u
let FILE_PERMS = 0o100644u
let EXEC_PERMS = 0o000755u

let HOST_UNIX = 0x0300us


let isDOSDir (cfh: CFH.t) =
    cfh.externalAttrs &&& 0x10u = 0x10u


let setUnixPermissionsCF (cfh: CFH.t) =
    let perms = if isDOSDir cfh then DIR_PERMS else FILE_PERMS
    let ea = cfh.externalAttrs ||| (perms <<< 16)
    let vmb = cfh.versionMadeBy &&& 0x00ffus ||| HOST_UNIX
    CentralFile { cfh with externalAttrs = ea; versionMadeBy = vmb }


let setExecutableCF matchers (cfh: CFH.t) =
    match Seq.exists (fun m -> m cfh) matchers with
    | false -> CentralFile cfh
    | true ->
        let ea = cfh.externalAttrs ||| (EXEC_PERMS <<< 16)
        CentralFile { cfh with externalAttrs = ea }


let processCF f = function
    | CentralFile cfh -> f cfh
    | record -> record


let setUnixPermissions = processCF setUnixPermissionsCF

let setExecutable matchers = processCF (setExecutableCF matchers)


let verboseCF processor (cfh: CFH.t) =
    let filename = CFH.getFilename cfh
    let dosAttrs = cfh.externalAttrs &&& 0xffu
    let oldPerms = (cfh.externalAttrs &&& 0xffff0000u) >>> 16
    let newcfh =
        match processor <| CentralFile cfh with
        | CentralFile cfh -> cfh
        | _ -> failwith "Bad processor result."
    let newPerms = (newcfh.externalAttrs &&& 0xffff0000u) >>> 16
    printfn "%06o->%06o (%02x) %s" oldPerms newPerms dosAttrs filename
    CentralFile newcfh

let verboseWrapper processor = processCF (verboseCF processor)


let filenameRegexMatch (pattern: Regex) cfh =
    pattern.IsMatch(CFH.getFilename cfh)


let matchesPattern pattern =
    filenameRegexMatch <| Regex(pattern)

let hasSuffix suffix =
    filenameRegexMatch <| Regex(sprintf @"%s$" <| Regex.Escape(suffix))

let hasParentDir dir =
    filenameRegexMatch <| Regex(sprintf @"/%s/[^/]+$" <| Regex.Escape(dir))
