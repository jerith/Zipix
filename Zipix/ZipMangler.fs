module Zipix.ZipMangler


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


let setUnixPermissions = function
    | CentralFile cfh -> setUnixPermissionsCF cfh
    | record -> record
