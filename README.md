# Zipix

[![AppVeyor status](https://img.shields.io/appveyor/ci/jerith/Zipix/master.svg)](https://ci.appveyor.com/project/jerith/zipix/history)
[![Travis status](https://img.shields.io/travis/jerith/Zipix/master.svg)](https://travis-ci.org/jerith/Zipix/builds)

Zipix is a tool to set unix permissions in a zipfile. Specifically, it sets the
execute bits on certain files so that cross-platform software with unix
executables can be built on non-unix systems and extracted on unix systems
without having to fix permissions.

All directories get `drwxr-xr-x` permissions, files matching any of a set of
"executable patterns" get `-rwxr-xr-x` permissions, and other files get
`-rw-r--r--` permissions.


## status

Works On My Machine(tm)

At the moment, I have an implementation of a subset of the
[documented zipfile format](https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT)
that successfully reads and writes the most common records headers and a set of
record processors that will set the relevant unix permission header fields.
This is wrapped up in a command line tool that will read a zipfile, process it,
and write to a new file.


## usage


    Zipix.exe -i foo.zip -o bar.zip --exec-suffix .x86 --exec-dir bin


## license

This code is covered by the MIT license. See the [LICENSE](LICENSE) file for
details.
