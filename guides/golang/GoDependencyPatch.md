##### The Users Challenge
They have a 3rd party `golang` dependency that supports cross-platform but virtual file paths or in-memory file paths, are coming in the Linux format.
For some calls like `filepath.IsAbs()` it returns `false` when the user runs locally on Windows. The problem here is that this can be used as a
path. These virtual paths work on all platforms (Windows/Mac/Linux) I have tested. However, this `filepath.IsAbs()` only returns true on the Osx/Linux
platforms.

So what could the user do to be unblocked fast to be productive?

##### There Aren't Always Good Solutions
The first solution is to PR the 3rd party project with a workaround into the dependency.

However, this is not always a fast process and you are blocked now! This paticular dependency the user was having issues with was
`gsjsonschema / gojsonreference`. They are highly used `golang` open sourced libraries but they aren't really updated too frequently.
There was, in fact, actually a PR that would also sort of fix the issue with a flag, but the repo seems to have been idle for months.

You could fork it, store it, maintain it, but thats quite the handful. You are the only user with the issue, even then, its only when
running locally AND when you are on Windows. Does anyone deploy `golang` with Windows containers? I will demonstrate a variation of this
in a second guide.

You could edit all your schema paths, including reference paths, to Windows based and hope that it still works on *nix when deployed.
That is a terrible option though.

##### Introducing Go Dependency Patching/Hacking
In this narrow use case, I think the best/easiest bet is to patch the local version oc the code and call it a day. Now, for this to run locally
on Windows we need the dependency to receive a `true` value on a line where we we are currently getting a `false`.

So what if we could just hard code `true`?

One of the beautys of `golang` is that it does pull dependency sources locally and it will compile it with your application. That means we
_**can**_ edit the file in our dependency package and our application will use that change. You also will not be at risk checking in
the change either. You do have to remember to re-run the script when that package is replaced (repulled or new version is acquired).

###### The Plan
1. Open `go.mod` and find the dependency's commit/version and store it as a string.
2. Convert the string to a dependency directory path.
2. Create the full FileNamePath.
   * Convert the file name path to Windows (or Linux).
     * The idea is that you can re-use this example but adapt it to other situations on both platforms.
3. Make the file writeable.
4. Find the code we wish to patch (a unique string) and replace it with our desired code (in this case just the value `true`).

We are going to write it as `bash` script for extra fun and because in theory they can run on both Operating Systems. On Windows,
most users can execute this because of `git` being installed (this is because Git usually has `Git Bash` installed as well). The converse is
not necessarily true, `PowerShell` isn't usually found on most Linux setups (but [it is actually possible](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-linux?view=powershell-7.1)!)

Start a script that is using `/bin/bash` as `shell` that also will exit on error with the error message.

```bash
#!/bin/bash
set -ex
```

Add an author and date so you can look back and wonder that the hell this jackass was doing.

```bash
#!/bin/bash
set -ex

# Author: cat@houseofcat.io
# Date: 04/20/2021 and no I unfortunately wasn't stoned.
# Pkg: github.com/xeipuuv/gojsonreference
# PkgVersion: Script is dynamic, should support all versions.
```

Set our known variables upfront to keep the script readable. Add comments.

```bash
#!/bin/bash
set -ex

# Author: cat@houseofcat.io
# Date:   04/20/2021 and no I unfortunately wasn't stoned.
# Pkg:    github.com/xeipuuv/gojsonreference
# PkgV:   Script is dynamic, should support all versions.

# GoPkg we wish to edit (still needs a version/commithash)
goPkg=github.com/xeipuuv/gojsonreference
goModPath="$GOPATH\\pkg\\mod\\" # If you have Golang installed under your user and not globally prepare to tweak this. Find your mods directory.

# Target file we wish to edit.
fileName=reference.go
```

Grep out specific package line, pipe it into sed to remove the leading whitespace (tabs/spaces) infront of it.

```bash
# Get our package line from go.mod without the leading whitespace.
goPkgVersion=$(grep $goPkg go.mod | sed -e 's/^[ \t]*//')
```

Replace the one remaining space to the @ symbol like the file paths have it.

```bash
# Replace every space with @ symbol
goPkgVersion="${goPkgVersion// /@}\\"
```

~~Mitch~~ Stitch all together _**snicker**_.

```bash
# Build the FileNamePath
fileNamePath=$goModPath$goPkgVersion$fileName
```

Flip the file paths for your OS.

```bash
# Replace every forward slash with a back slash because Windows. Adjust this for your platform etc.
fileNamePath="${fileNamePath////\\}"
```

Change read/write/execute permissions on the target file.

```bash
# Make sure we can do the next step.
chmod +xwr "$fileNamePath"
```

Use `sed` to find the unique `string` inside a file then replace it with `true`.

```bash
# Replace the golang statement with true.
sed -i 's/filepath.IsAbs(refUrl.Host + refUrl.Path)/true/' "$fileNamePath"
```

The full script.

```bash
#!/bin/bash
set -ex

# Author: cat@houseofcat.io
# Date:   04/20/2021 and no I unfortunately wasn't stoned.
# Pkg:    github.com/xeipuuv/gojsonreference
# PkgV:   Script is dynamic, should support all versions.

# GoPkg we wish to edit (still needs a version/commithash)
goPkg=github.com/xeipuuv/gojsonreference
goModPath="$GOPATH\\pkg\\mod\\" # If you have Golang installed under your user and not globally prepare to tweak this. Find your mods directory.

# Target file we wish to edit.
fileName=reference.go

# Get our package line from go.mod without the leading whitespace.
goPkgVersion=$(grep $goPkg go.mod | sed -e 's/^[ \t]*//')

# Replace every space with @ symbol
goPkgVersion="${goPkgVersion// /@}\\"

# Build the FileNamePath
fileNamePath=$goModPath$goPkgVersion$fileName

# Replace every forward slash with a back slash because Windows. Adjust this for your platform etc.
fileNamePath="${fileNamePath////\\}"

# Make sure we can do the next step.
chmod +xwr "$fileNamePath"

# Replace the golang statement with true.
sed -i 's/filepath.IsAbs(refUrl.Host + refUrl.Path)/true/' "$fileNamePath"
```

##### Final Thoughts
This is totally a crap solve and hackery, but it definitely unblocked the user who then was then able to use JSON reference schemas that had both absolute and relative *nix filepaths
stored in memory while debugging on Windows.
