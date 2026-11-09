param(
    [string]$Compiler = $env:BREEZY_ZIG,
    [string]$OutputDirectory = '',
    [ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version = '2.0.1',
    [switch]$Check
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $root 'artifacts\native' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (-not $Compiler) {
    $command = Get-Command zig -ErrorAction SilentlyContinue
    if ($command) { $Compiler = $command.Source }
}
if (-not $Compiler -or -not (Test-Path -LiteralPath $Compiler)) { throw 'Set BREEZY_ZIG or pass -Compiler with the path to zig.exe.' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$env:ZIG_GLOBAL_CACHE_DIR = Join-Path $root 'artifacts\zig-global-cache'
$env:ZIG_LOCAL_CACHE_DIR = Join-Path $root 'artifacts\zig-local-cache'
$utf8 = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'version.hpp'), ('#pragma once' + "`n" + '#define BREEZY_VERSION "' + $Version + '"' + "`n"), $utf8)
$manifest = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'app.manifest'), [Text.Encoding]::UTF8).Replace('version="2.0.0.0"', ('version="' + $Version + '.0"'))
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'app.manifest'), $manifest, $utf8)
$strings = [ordered]@{}
$source = [IO.File]::ReadAllText((Join-Path $root 'UiText.cs'), [Text.Encoding]::UTF8)
foreach ($match in [regex]::Matches($source, '\["([^"]+)"\]\s*=\s*"((?:\\.|[^"\\])*)"')) {
    $value = $match.Groups[2].Value.Replace('\n', "`n").Replace('\r', "`r").Replace('\"', '"').Replace('\\', '\')
    $values = $value.Split('|')
    if ($values.Count -eq 8) { $strings[$match.Groups[1].Value] = $values }
}
if ($strings.Count -lt 100) { throw 'Cannot extract the existing eight-language UI catalog.' }
$extra = Get-Content (Join-Path $PSScriptRoot 'strings.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($entry in $extra.PSObject.Properties) {
    if ($entry.Value.Count -ne 8) { throw ('Missing translation: ' + $entry.Name) }
    $strings[$entry.Name] = $entry.Value
}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'ui-strings.json'), ($strings | ConvertTo-Json -Depth 5 -Compress), $utf8)
$languages = @()
$languageSource = [IO.File]::ReadAllText((Join-Path $root 'Languages.cs'), [Text.Encoding]::UTF8)
foreach ($match in [regex]::Matches($languageSource, 'new\("([^"]+)", "([^"]+)", "([^"]+)", "([^"]+)"\)')) {
    $languages += ,@($match.Groups[1].Value,$match.Groups[2].Value,$match.Groups[3].Value,$match.Groups[4].Value)
}
if ($languages.Count -ne 20) { throw 'Cannot extract the twenty translation languages.' }
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'languages.json'), (ConvertTo-Json -InputObject $languages -Depth 5 -Compress), $utf8)
function ResourcePath([string]$Path) { return ([IO.Path]::GetFullPath($Path)).Replace('\', '/') }
$rc = '1 ICON "' + (ResourcePath (Join-Path $root 'Assets\translation-orb.ico')) + '"' + "`n"
$rc += '1 24 "' + (ResourcePath (Join-Path $OutputDirectory 'app.manifest')) + '"' + "`n"
$rc += '10 RCDATA "' + (ResourcePath (Join-Path $root 'Dictionary\english-zh.json.gz')) + '"' + "`n"
$rc += '11 RCDATA "' + (ResourcePath (Join-Path $root 'Assets\translation-orb.png')) + '"' + "`n"
$rc += '12 RCDATA "' + (ResourcePath (Join-Path $OutputDirectory 'ui-strings.json')) + '"' + "`n"
$rc += '13 RCDATA "' + (ResourcePath (Join-Path $OutputDirectory 'languages.json')) + '"' + "`n"
$rc += '14 RCDATA "' + (ResourcePath (Join-Path $PSScriptRoot 'resources\html-entities.json')) + '"' + "`n"
$versionNumbers = ($Version.Replace('.', ',') + ',0')
$rc += @"
1 VERSIONINFO
 FILEVERSION $versionNumbers
 PRODUCTVERSION $versionNumbers
 FILEOS 0x40004
 FILETYPE 0x1
BEGIN
 BLOCK "StringFileInfo"
 BEGIN
  BLOCK "040904B0"
  BEGIN
   VALUE "FileDescription", "Breezy Translate\0"
   VALUE "FileVersion", "$Version\0"
   VALUE "InternalName", "Breezy\0"
   VALUE "OriginalFilename", "Breezy.exe\0"
   VALUE "ProductName", "Breezy\0"
   VALUE "ProductVersion", "$Version\0"
  END
 END
 BLOCK "VarFileInfo"
 BEGIN
  VALUE "Translation", 0x0409, 1200
 END
END
"@
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'Breezy.rc'), $rc, $utf8)
Push-Location $PSScriptRoot
try {
    & $Compiler rc /fo (Join-Path $OutputDirectory 'Breezy.res') (Join-Path $OutputDirectory 'Breezy.rc')
    if ($LASTEXITCODE -ne 0) { throw 'Native resource compilation failed.' }
    $sources = @(Get-ChildItem $PSScriptRoot -Filter *.cpp | Select-Object -ExpandProperty FullName)
    $objects = @()
    foreach ($unit in @('miniz','miniz_tinfl','miniz_tdef')) {
        $object = Join-Path $OutputDirectory ($unit + '.o')
        & $Compiler cc -target x86_64-windows-gnu -O2 -DMINIZ_NO_ARCHIVE_APIS -DMINIZ_NO_STDIO -DMINIZ_NO_TIME -c (Join-Path $PSScriptRoot ('vendor\' + $unit + '.c')) -o $object
        if ($LASTEXITCODE -ne 0) { throw ('Native compression compilation failed: ' + $unit) }
        $objects += $object
    }
    $arguments = @('c++','-target','x86_64-windows-gnu','-std=c++17','-O2','-static','-municode','-Wl,--subsystem,windows','-fms-extensions','-DUNICODE','-D_UNICODE','-DNOMINMAX','-DMINIZ_NO_ARCHIVE_APIS','-DMINIZ_NO_STDIO','-DMINIZ_NO_TIME')
    $arguments += $sources
    $arguments += @('-I',$OutputDirectory)
    $arguments += $objects
    $arguments += @((Join-Path $OutputDirectory 'Breezy.res'),'-o',(Join-Path $OutputDirectory 'Breezy.exe'),'-luser32','-lgdi32','-lcomctl32','-lshell32','-lshlwapi','-lole32','-loleaut32','-lcrypt32','-lwinhttp','-luuid','-lgdiplus','-ldwmapi','-lsapi','-lcomdlg32','-lws2_32','-luxtheme')
    & $Compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Native compilation failed.' }
    & (Join-Path $PSScriptRoot 'Verify-Portable.ps1') -Executable (Join-Path $OutputDirectory 'Breezy.exe')
    Copy-Item (Join-Path $root 'app-links.json'),(Join-Path $root 'CHANGELOG*.md'),(Join-Path $root 'README*.md') $OutputDirectory -Force
    New-Item -ItemType Directory -Force -Path (Join-Path $OutputDirectory 'Licenses') | Out-Null
    Copy-Item (Join-Path $root 'Dictionary\ECDICT-LICENSE.txt'),(Join-Path $PSScriptRoot 'vendor\*-LICENSE.txt') (Join-Path $OutputDirectory 'Licenses') -Force
    if ($Check) {
        $process = Start-Process -FilePath (Join-Path $OutputDirectory 'Breezy.exe') -ArgumentList '--native-test' -Wait -PassThru -WindowStyle Hidden
        if ($process.ExitCode -ne 0) { throw 'Native self-test failed; inspect native-test.txt.' }
    }
    Get-Item (Join-Path $OutputDirectory 'Breezy.exe') | Select-Object FullName,Length
} finally { Pop-Location }
