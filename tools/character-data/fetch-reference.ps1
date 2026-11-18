param(
    [string]$Revision = '8790755429c346d20020eed7826d312d484c393f'
)

$ErrorActionPreference = 'Stop'
$repository = '5etools-mirror-3/5etools-src'
$baseUrl = "https://raw.githubusercontent.com/$repository/$Revision"
$cacheRoot = Join-Path $PSScriptRoot 'cache'

# Character creation rules only: no images, audio, monsters, or item catalogues.
$files = @(
    'LICENSE.md'
    'data/class/index.json'
    'data/class/class-artificer.json'
    'data/class/class-barbarian.json'
    'data/class/class-bard.json'
    'data/class/class-cleric.json'
    'data/class/class-druid.json'
    'data/class/class-fighter.json'
    'data/class/class-monk.json'
    'data/class/class-paladin.json'
    'data/class/class-ranger.json'
    'data/class/class-rogue.json'
    'data/class/class-sorcerer.json'
    'data/class/class-warlock.json'
    'data/class/class-wizard.json'
    'data/backgrounds.json'
    'data/races.json'
    'data/feats.json'
    'data/optionalfeatures.json'
    'data/charcreationoptions.json'
    'data/skills.json'
    'data/languages.json'
    'data/spells/index.json'
    'data/spells/sources.json'
    'data/spells/spells-phb.json'
    'data/spells/spells-xphb.json'
    'data/spells/spells-tce.json'
    'data/spells/spells-efa.json'
)

$manifestFiles = [System.Collections.Generic.List[object]]::new()
foreach ($relativePath in $files) {
    $destination = Join-Path $cacheRoot ($relativePath -replace '/', [IO.Path]::DirectorySeparatorChar)
    $directory = Split-Path -Parent $destination
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $temporary = "$destination.download"
    $url = "$baseUrl/$relativePath"

    & curl.exe --fail --location --silent --show-error --retry 3 --max-time 120 --output $temporary $url
    if ($LASTEXITCODE -ne 0) {
        Remove-Item -LiteralPath $temporary -ErrorAction SilentlyContinue
        throw "Download failed: $relativePath"
    }
    if ($relativePath.EndsWith('.json')) {
        try {
            $null = Get-Content -LiteralPath $temporary -Raw -Encoding utf8 | ConvertFrom-Json
        } catch {
            Remove-Item -LiteralPath $temporary -ErrorAction SilentlyContinue
            throw "Invalid JSON: $relativePath"
        }
    }
    Move-Item -LiteralPath $temporary -Destination $destination -Force
    $item = Get-Item -LiteralPath $destination
    $manifestFiles.Add([ordered]@{
        path = $relativePath
        bytes = $item.Length
        sha256 = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
    })
    Write-Host "Downloaded $relativePath ($($item.Length) bytes)"
}

$manifest = [ordered]@{
    repository = "https://github.com/$repository"
    revision = $Revision
    scope = 'Character creation reference data; no images, audio, monsters or item catalogues'
    files = $manifestFiles
}
$manifestPath = Join-Path $cacheRoot 'manifest.json'
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
Write-Host "Ready: $($files.Count) files; manifest: $manifestPath"
