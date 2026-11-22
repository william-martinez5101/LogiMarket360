param([string]$Destino = '')
$ErrorActionPreference = 'Stop'
$raizProyecto = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw 'Se necesita Git para preparar la revision.' }
$pendientesGit = & git -C $raizProyecto status --porcelain
if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar Git.' }
if ($pendientesGit) { throw 'Registre los avances comprobados antes de empaquetar; el ZIP debe corresponder a un commit.' }
$commitRevision = (& git -C $raizProyecto rev-parse HEAD).Trim()
$archivosGit = & git -C $raizProyecto -c core.quotepath=false ls-files
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron enumerar los archivos versionados.' }
$archivosRevision = @($archivosGit | Where-Object {
    $_ -notmatch '(^|/)(bin|obj|\.vs|\.git|tmp|artifacts|TestResults)(/|$)' -and
    $_ -notmatch '\.(db|sqlite|sqlite3)(-|$)' -and
    $_ -notmatch '(^|/)(\.env|appsettings\.Local\.json)$'
} | Sort-Object)
if (-not $Destino) { $Destino = Join-Path $raizProyecto 'artifacts\APE04_LogiMarket360_Revision.zip' }
$rutaZip = [IO.Path]::GetFullPath($Destino)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($rutaZip)) | Out-Null
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$flujoZip = [IO.File]::Open($rutaZip, [IO.FileMode]::Create)
$archivoZip = [IO.Compression.ZipArchive]::new($flujoZip, [IO.Compression.ZipArchiveMode]::Create)
$huellasRevision = @()
try {
    foreach ($archivo in $archivosRevision) {
        $rutaOrigen = [IO.Path]::GetFullPath((Join-Path $raizProyecto $archivo))
        if (-not $rutaOrigen.StartsWith($raizProyecto + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Ruta fuera del proyecto.' }
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archivoZip, $rutaOrigen, ('LogiMarket360/' + $archivo.Replace('\','/')), [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        $huellasRevision += [ordered]@{ Archivo = $archivo; SHA256 = (Get-FileHash -LiteralPath $rutaOrigen -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
    $manifiesto = [ordered]@{ Commit = $commitRevision; FechaUtc = [DateTimeOffset]::UtcNow.ToString('O'); Archivos = $huellasRevision }
    $entrada = $archivoZip.CreateEntry('LogiMarket360/MANIFIESTO_REVISION.json')
    $escritor = [IO.StreamWriter]::new($entrada.Open(), [Text.UTF8Encoding]::new($false))
    try { $escritor.Write(($manifiesto | ConvertTo-Json -Depth 6)) } finally { $escritor.Dispose() }
} finally { $archivoZip.Dispose(); $flujoZip.Dispose() }
$hashZip = (Get-FileHash -LiteralPath $rutaZip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($rutaZip + '.sha256', $hashZip + '  ' + [IO.Path]::GetFileName($rutaZip) + [Environment]::NewLine)
[ordered]@{ ZIP = $rutaZip; Archivos = $archivosRevision.Count + 1; Commit = $commitRevision; SHA256 = $hashZip } | ConvertTo-Json
