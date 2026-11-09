param([Parameter(Mandatory=$true)][string]$Executable)
$ErrorActionPreference = 'Stop'
$bytes = [IO.File]::ReadAllBytes([IO.Path]::GetFullPath($Executable))
$pe = [BitConverter]::ToInt32($bytes,60)
if ([BitConverter]::ToUInt32($bytes,$pe) -ne 0x4550) { throw 'Invalid PE executable.' }
$optional = $pe + 24
if ([BitConverter]::ToUInt16($bytes,$optional) -ne 0x20B) { throw 'Expected Windows x64 executable.' }
if ([BitConverter]::ToUInt16($bytes,$optional+68) -ne 2) { throw 'Expected GUI subsystem without a console window.' }
if ([BitConverter]::ToUInt32($bytes,$optional+112+14*8) -ne 0) { throw 'The executable contains a .NET CLR directory.' }
$sectionCount = [BitConverter]::ToUInt16($bytes,$pe+6)
$sectionStart = $optional + [BitConverter]::ToUInt16($bytes,$pe+20)
function Convert-Rva([uint32]$Rva) {
    for ($index=0; $index -lt $sectionCount; $index++) {
        $entry = $sectionStart + $index*40
        $address = [BitConverter]::ToUInt32($bytes,$entry+12)
        $size = [math]::Max([BitConverter]::ToUInt32($bytes,$entry+8),[BitConverter]::ToUInt32($bytes,$entry+16))
        if ($Rva -ge $address -and $Rva -lt $address+$size) { return [int]($Rva-$address+[BitConverter]::ToUInt32($bytes,$entry+20)) }
    }
    throw 'Invalid import address.'
}
$imports = @()
$offset = Convert-Rva ([BitConverter]::ToUInt32($bytes,$optional+120))
while ([BitConverter]::ToUInt32($bytes,$offset+12) -ne 0) {
    $nameOffset = Convert-Rva ([BitConverter]::ToUInt32($bytes,$offset+12))
    $end = $nameOffset
    while ($bytes[$end] -ne 0) { $end++ }
    $name = [Text.Encoding]::ASCII.GetString($bytes,$nameOffset,$end-$nameOffset).ToLowerInvariant()
    # The universal CRT is part of supported Windows 10/11 systems.
    $system = @('user32.dll','gdi32.dll','comctl32.dll','shell32.dll','shlwapi.dll','ole32.dll','oleaut32.dll','crypt32.dll','winhttp.dll','gdiplus.dll','dwmapi.dll','uxtheme.dll','comdlg32.dll','ws2_32.dll','kernel32.dll','advapi32.dll','ntdll.dll','msvcrt.dll','ucrtbase.dll')
    if ($name -notin $system -and $name -notlike 'api-ms-win-crt-*.dll') { throw ('Unexpected runtime dependency: ' + $name) }
    $imports += $name
    $offset += 20
}
Write-Output ('PASS: Windows x64 GUI; no CLR; only Windows system imports (' + $imports.Count + ' DLLs).')
