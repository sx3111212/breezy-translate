param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$')]
    [string]$Repository,
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '1.0.0'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root "artifacts\release-$Version"
if (Test-Path -LiteralPath $out) { throw "Output exists: $out. Use a new version or move this directory before rebuilding." }
$app = Join-Path $out 'BreezyTranslate'
New-Item -ItemType Directory -Path $app -Force | Out-Null
Push-Location $root
try {
    dotnet publish .\EnglishHelper.csproj -c Release -r win-x64 --self-contained true -p:Version=$Version -p:DebugType=None -p:DebugSymbols=false -o $app
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }
    $links = Get-Content .\app-links.json -Raw -Encoding UTF8 | ConvertFrom-Json
    $links.GitHubRepository = $Repository
    $links.ProjectUrl = "https://github.com/$Repository"
    $links.ReleasesUrl = "https://github.com/$Repository/releases/latest"
    if ($links.DonationUrl -and $links.DonationUrl -notmatch '^https://') { throw 'DonationUrl must use HTTPS' }
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [IO.File]::WriteAllText((Join-Path $app 'app-links.json'), ($links | ConvertTo-Json), $utf8)
    Copy-Item .\README*.md $app
    if (Test-Path .\LICENSE) { Copy-Item .\LICENSE $app }
    # Test the actual bundled runtime and embedded dictionary before making the ZIP.
    foreach ($test in @('--dictionary-test', '--dictionary-packs-test')) {
        $process = Start-Process -FilePath (Join-Path $app 'Breezy.exe') -ArgumentList $test -Wait -PassThru -WindowStyle Hidden
        $log = Join-Path $app ($test.TrimStart('-') + '.txt')
        if ($process.ExitCode -ne 0 -or -not (Test-Path $log) -or (Get-Content $log -Raw) -notmatch '^PASS:') { throw "Release test failed: $test" }
        Remove-Item -LiteralPath $log
    }
    Compress-Archive -Path $app -DestinationPath (Join-Path $out "BreezyTranslate-$Version-win-x64.zip")
    Copy-Item .\DictionaryPacks\ecdict-full-en-zh.json.gz $out
    Copy-Item .\DictionaryPacks\ECDICT-LICENSE.txt $out
    $files = Get-ChildItem $out -File | Sort-Object Name
    $hashes = foreach ($file in $files) { '{0}  {1}' -f (Get-FileHash $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $file.Name }
    [IO.File]::WriteAllLines((Join-Path $out 'SHA256SUMS.txt'), [string[]]$hashes, $utf8)
    Write-Host "Release files ready: $out"
} finally { Pop-Location }
