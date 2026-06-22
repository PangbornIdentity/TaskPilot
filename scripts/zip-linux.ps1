# Creates a Linux-compatible zip from src/publish-linux.
# PowerShell's Compress-Archive and ZipFile.CreateFromDirectory both write
# entry paths with backslashes; Linux unzip then creates files named
# literally "wwwroot\css\app.css" instead of nested directories.
# This script writes forward-slash entries explicitly.

param(
    [string]$Src = (Join-Path $PSScriptRoot '..\src\publish-linux'),
    [string]$Dst = (Join-Path $PSScriptRoot '..\src\deploy-linux.zip')
)

# Normalize away any '..' segments so the Substring-based relative paths below
# line up with the resolved FullName of each file (Join-Path does not collapse '..').
$Src = [System.IO.Path]::GetFullPath($Src)
$Dst = [System.IO.Path]::GetFullPath($Dst)

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (Test-Path $Dst) { Remove-Item $Dst -Force }

$zip = [System.IO.Compression.ZipFile]::Open($Dst, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem -Path $Src -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring($Src.Length + 1).Replace('\', '/')
        $entry = $zip.CreateEntry($rel, [System.IO.Compression.CompressionLevel]::Optimal)
        $es = $entry.Open()
        $fs = [System.IO.File]::OpenRead($_.FullName)
        try { $fs.CopyTo($es) }
        finally { $fs.Dispose(); $es.Dispose() }
    }
}
finally {
    $zip.Dispose()
}

$info = Get-Item $Dst
Write-Host ("Created: " + $info.FullName)
Write-Host ("Size: " + ($info.Length / 1MB).ToString('F2') + " MB")

$reader = [System.IO.Compression.ZipFile]::OpenRead($Dst)
try {
    Write-Host ("Entries: " + $reader.Entries.Count)
    # Completeness check: every source file must be present in the zip.
    $srcCount = (Get-ChildItem -Path $Src -Recurse -File).Count
    if ($reader.Entries.Count -ne $srcCount) {
        throw "Entry count $($reader.Entries.Count) does not match source file count $srcCount"
    }
    # Sanity-check the entry paths use forward slashes
    $bad = $reader.Entries | Where-Object { $_.FullName -match '\\' } | Select-Object -First 1
    if ($bad) {
        throw "Backslash entry detected: $($bad.FullName)"
    }
    Write-Host "All entry paths use forward slashes; entry count matches source ($srcCount files)."
}
finally {
    $reader.Dispose()
}
