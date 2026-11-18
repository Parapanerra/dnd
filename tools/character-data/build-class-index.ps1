param(
    [string]$CachePath = (Join-Path $PSScriptRoot 'cache'),
    [string]$OutputPath = (Join-Path $PSScriptRoot 'class-index.json')
)

$ErrorActionPreference = 'Stop'
$classPath = Join-Path $CachePath 'data/class'
if (-not (Test-Path -LiteralPath $classPath)) {
    throw "Class reference data is missing: $classPath"
}

$records = foreach ($file in Get-ChildItem -LiteralPath $classPath -Filter 'class-*.json' -File) {
    $data = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    foreach ($entry in $data.class) {
        $source = [string]$entry.source
        if ($source -notin @('PHB', 'XPHB', 'TCE', 'EFA')) { continue }
        $features = @($data.classFeature | Where-Object {
            $_.level -eq 1 -and $_.classSource -eq $source
        } | ForEach-Object { [string]$_.name })
        $skillChoices = @($entry.startingProficiencies.skills)
        [pscustomobject]@{
            id = $entry.name.ToLowerInvariant()
            name = [string]$entry.name
            edition = if ($entry.edition -eq 'one') { 2024 } else { 2014 }
            source = $source
            hitDie = [int]$entry.hd.faces
            savingThrows = @($entry.proficiency)
            skillChoices = $skillChoices
            spellcastingAbility = [string]$entry.spellcastingAbility
            firstLevelFeatures = $features
        }
    }
}

if (@($records).Count -ne 26) { throw "Expected 26 class/edition records, found $(@($records).Count)." }
$records = @($records | Sort-Object id, edition)
$records | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $OutputPath -Encoding utf8
Write-Output "Wrote $(@($records).Count) class/edition records to $OutputPath"
