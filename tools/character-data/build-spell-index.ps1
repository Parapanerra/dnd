param(
    [string]$CachePath = (Join-Path $PSScriptRoot 'cache'),
    [string]$OutputPath = (Join-Path $PSScriptRoot 'spell-index-level-0-1.json')
)

$ErrorActionPreference = 'Stop'
$lookup = Get-Content -LiteralPath (Join-Path $CachePath 'data/spells/sources.json') -Raw | ConvertFrom-Json
$records = foreach ($edition in @(
    @{ Year = 2014; Source = 'PHB' },
    @{ Year = 2024; Source = 'XPHB' },
    @{ Year = 2014; Source = 'TCE' },
    @{ Year = 2024; Source = 'EFA' }
)) {
    $source = $edition.Source
    $path = Join-Path $CachePath "data/spells/spells-$($source.ToLowerInvariant()).json"
    $data = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    foreach ($spell in $data.spell) {
        $entry = $lookup.$source.PSObject.Properties[[string]$spell.name]
        $classes = if ($null -eq $entry) { @() } else {
            @($entry.Value.class | Where-Object {
                (($source -ne 'TCE') -and ($_.source -eq $source)) -or
                (($_.name -eq 'Artificer') -and ($_.source -eq $(if ($edition.Year -eq 2014) { 'TCE' } else { 'EFA' })))
            } | ForEach-Object { [string]$_.name })
        }
        if ($source -eq 'TCE' -and @($classes).Count -eq 0) { continue }
        if ($spell.level -gt 9) { continue }
        [pscustomobject]@{
            edition = $edition.Year
            source = $source
            name = [string]$spell.name
            level = [int]$spell.level
            school = [string]$spell.school
            classes = @($classes)
            castingUnit = [string]$spell.time[0].unit
            castingAmount = [int]$spell.time[0].number
            durationUnit = if ($spell.duration[0].type -eq 'timed') { [string]$spell.duration[0].duration.type } else { [string]$spell.duration[0].type }
            durationAmount = [int]$spell.duration[0].duration.amount
            concentration = [bool]$spell.duration[0].concentration
            ritual = [bool]$spell.meta.ritual
            rangeUnit = [string]$spell.range.distance.type
            rangeAmount = [int]$spell.range.distance.amount
            materialCost = [int]$spell.components.m.cost
            materialConsumed = [bool]$spell.components.m.consume
            scaling5 = (@($spell.scalingLevelDice) | ForEach-Object { [string]$_.scaling.'5' } | Where-Object { $_ }) -join ' / '
            scaling11 = (@($spell.scalingLevelDice) | ForEach-Object { [string]$_.scaling.'11' } | Where-Object { $_ }) -join ' / '
            scaling17 = (@($spell.scalingLevelDice) | ForEach-Object { [string]$_.scaling.'17' } | Where-Object { $_ }) -join ' / '
            scalingExtra = @($spell.scalingLevelDice).Count -eq 1 -and [string]$spell.scalingLevelDice.label -like 'extra*'
        }
    }
}

if (@($records).Count -lt 150) { throw "Expected at least 150 first-level and cantrip records, found $(@($records).Count)." }
foreach ($year in @(2014, 2024)) {
    foreach ($className in @('Artificer', 'Bard', 'Cleric', 'Druid', 'Sorcerer', 'Warlock', 'Wizard')) {
        foreach ($level in @(0, 1)) {
            if (-not ($records | Where-Object { $_.edition -eq $year -and $_.level -eq $level -and $_.classes -contains $className })) {
                throw "Missing level $level spells for $className ($year). Refusing to overwrite the runtime index."
            }
        }
    }
}
$records = @($records | Sort-Object edition, level, name)
@{ entries = $records } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $OutputPath -Encoding utf8
Write-Output "Wrote $(@($records).Count) PHB/XPHB and Artificer reference spell records to $OutputPath"
