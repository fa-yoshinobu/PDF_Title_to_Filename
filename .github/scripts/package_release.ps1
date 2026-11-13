param([string]$OutputDirectory = 'artifacts/release', [string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not [IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory = Join-Path $root $OutputDirectory }
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $OutputDirectory 'LICENSE') -Force
$files = @('PDF_Title_to_Filename.exe','LICENSE')
foreach ($file in $files) {
    if (-not (Test-Path -LiteralPath (Join-Path $OutputDirectory $file) -PathType Leaf)) { throw "Missing distribution file: $file" }
}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = Join-Path $OutputDirectory ('PDF_Title_to_Filename-' + $Runtime + '.zip')
$stream = [IO.File]::Open($zip, [IO.FileMode]::Create)
try {
    $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $files) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $OutputDirectory $file), $file, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $archive.Dispose() }
} finally { $stream.Dispose() }
$algorithm = [Security.Cryptography.SHA256]::Create()
$inputStream = [IO.File]::OpenRead($zip)
try { $hash = ([BitConverter]::ToString($algorithm.ComputeHash($inputStream))).Replace('-', '').ToLowerInvariant() }
finally { $inputStream.Dispose(); $algorithm.Dispose() }
[IO.File]::WriteAllText($zip + '.sha256', $hash + '  ' + [IO.Path]::GetFileName($zip) + [char]10, [Text.UTF8Encoding]::new($false))
