# Zipix

Zipix is a tool to set unix permissions in a zipfile. Specifically, it sets the
execute bits on certain files so that cross-platform software with unix
executables can be built on non-unix systems and extracted on unix systems
without having to fix permissions.

All directories get `drwxr-xr-x` permissions, files matching any of a set of
"executable patterns" get `-rwxr-xr-x` permissions, and other files get
`-rw-r--r--` permissions.

## status

Unfinished.

At the moment, I have an implementation of a subset of the
[documented zipfile format](https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT)
that successfully reads the most common records headers and copies them
unmodified to a new file.

## license

This code is covered by the MIT license. See the [LICENSE](LICENSE) file for
details.
