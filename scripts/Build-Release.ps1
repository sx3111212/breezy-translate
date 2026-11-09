param(
    [ValidatePattern('^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$')]
    [string]$Repository = 'sx3111212/breezy-translate',
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '2.0.0',
    [string]$Compiler = $env:BREEZY_ZIG,
    [switch]$UseExistingBuild
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root ('artifacts\release-' + $Version)
$build = Join-Path $root 'artifacts\native'
if (Test-Path -LiteralPath $out) { throw "Output exists: $out. Choose a new version or move the existing release directory." }
if (-not $UseExistingBuild) {
    & (Join-Path $root 'native\Build.ps1') -Compiler $Compiler -Version $Version -OutputDirectory $build -Check
}
$executable = Join-Path $build 'Breezy.exe'
if (-not (Test-Path -LiteralPath $executable)) { throw 'Native Breezy.exe has not been built.' }
if ((Get-Item -LiteralPath $executable).VersionInfo.FileVersion -ne $Version) { throw 'Executable version does not match the requested release.' }
& (Join-Path $root 'native\Verify-Portable.ps1') -Executable $executable
$app = Join-Path $out 'Breezy'
New-Item -ItemType Directory -Path $app -Force | Out-Null
Copy-Item -LiteralPath $executable -Destination $app
Copy-Item -LiteralPath (Join-Path $root 'native\Portable.zh.md') -Destination (Join-Path $app 'README.md')
Copy-Item -LiteralPath (Join-Path $root 'native\Portable.en.md') -Destination (Join-Path $app 'README.en.md')
Copy-Item -Path (Join-Path $root 'CHANGELOG*.md') -Destination $app
Copy-Item -LiteralPath (Join-Path $build 'Licenses') -Destination $app -Recurse
$links = Get-Content (Join-Path $root 'app-links.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$links.GitHubRepository = $Repository
$links.ProjectUrl = 'https://github.com/' + $Repository
$links.ReleasesUrl = 'https://github.com/' + $Repository + '/releases/latest'
if ($links.DonationUrl -and $links.DonationUrl -notmatch '^https://') { throw 'DonationUrl must use HTTPS.' }
$utf8 = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $app 'app-links.json'), ($links | ConvertTo-Json), $utf8)
Compress-Archive -LiteralPath $app -DestinationPath (Join-Path $out 'Breezy-win-x64.zip')
Copy-Item -LiteralPath (Join-Path $root 'DictionaryPacks\ecdict-full-en-zh.json.gz') -Destination $out
Copy-Item -LiteralPath (Join-Path $root 'DictionaryPacks\ECDICT-LICENSE.txt') -Destination $out
$hashes = foreach ($file in (Get-ChildItem -LiteralPath $out -File | Sort-Object Name)) {
    '{0}  {1}' -f (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $file.Name
}
[IO.File]::WriteAllLines((Join-Path $out 'SHA256SUMS.txt'), [string[]]$hashes, $utf8)
Get-ChildItem -LiteralPath $out -File | Select-Object Name,Length