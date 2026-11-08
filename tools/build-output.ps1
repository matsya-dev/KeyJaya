function Publish-KjBuild($Work, $App, $Payload = '', $Installer = '', $Version = '') {
    $out = Join-Path $Work.Root 'out'
    $receiptPath = Join-Path $out 'build-output.json'
    $previous = $null
    $outputs = @{}
    if (Test-Path -LiteralPath $receiptPath) {
        $null = Assert-KjOutputPath $Work.Root $receiptPath
        $previous = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
        if ($previous.schema -ne 'keyjaya-build-1') { throw 'Unrecognized build receipt; existing output preserved.' }
        foreach ($property in $previous.outputs.PSObject.Properties) { $outputs[$property.Name]=$property.Value }
    }
    $items = @(@{ source=$App; name='app'; directory=$true })
    if ($Payload) {
        if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid build version.' }
        $items += @{ source=$Payload; name='payload.zip'; directory=$false }
        $items += @{ source=$Installer; name=('KeyJaya-Setup-v' + $Version + '.exe'); directory=$false }
    }
    # Validate every old output before moving anything. Unmarked, edited,
    # linked or running app trees are preserved rather than adopted for deletion.
    foreach ($item in $items) {
        $item.target = Assert-KjOutputPath $Work.Root (Join-Path $out $item.name)
        $source = Assert-KjOutputPath $Work.Root $item.source
        if (-not $source.StartsWith($Work.Path + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Build stage is not owned by this run.' }
        if ($item.directory) {
            $item.receipt = @{ directory=$true; files=(Get-KjInventory $Work.Root $source) }
        } else { $item.receipt = @{ directory=$false; sha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash } }
        if (Test-Path -LiteralPath $item.target) {
            if (-not $outputs.ContainsKey($item.name)) { throw ('Existing output has no ownership receipt; preserve or move it aside first: ' + $item.target) }
            $old = $outputs[$item.name]
            if ($item.directory) {
                if (-not $old.directory -or -not (Test-KjInventory $Work.Root $item.target $old.files)) { throw 'Previous app output was edited; it has been preserved.' }
                $image = Join-Path $item.target 'engine/keyjaya-app.exe'
                $probe = [IO.File]::Open($image, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
                $probe.Dispose()
            } elseif ($old.directory -or (Get-FileHash -LiteralPath $item.target -Algorithm SHA256).Hash -ne $old.sha256) {
                throw ('Previous build file was edited; preserved: ' + $item.target)
            }
        }
    }
    $moved = @()
    try {
        foreach ($item in $items) {
            $item.backup = Join-Path $Work.Path ('previous-' + $moved.Count)
            $item.hadPrevious = Test-Path -LiteralPath $item.target
            $item.promoted = $false
            $moved += $item
            if ($item.hadPrevious) { Move-Item -LiteralPath $item.target -Destination $item.backup -ErrorAction Stop }
            Move-Item -LiteralPath $item.source -Destination $item.target -ErrorAction Stop
            $item.promoted = $true
            $outputs[$item.name]=$item.receipt
        }
        $temporary = Join-Path $Work.Path 'new-build-output.json'
        Write-KjJson $temporary @{ schema='keyjaya-build-1'; outputs=$outputs; completed_utc=[datetime]::UtcNow.ToString('o') }
        if (Test-Path -LiteralPath $receiptPath) {
            [IO.File]::Replace($temporary, $receiptPath, (Join-Path $Work.Path 'old-build-output.json'))
        } else { [IO.File]::Move($temporary, $receiptPath) }
    } catch {
        $failure = $_
        try {
            for ($index=$moved.Count-1; $index -ge 0; $index--) {
                $item=$moved[$index]
                if ($item.promoted) { Move-Item -LiteralPath $item.target -Destination $item.source -ErrorAction Stop }
                if ($item.hadPrevious -and (Test-Path -LiteralPath $item.backup)) { Move-Item -LiteralPath $item.backup -Destination $item.target -ErrorAction Stop }
            }
        } catch {
            $Work.Keep=$true
            $ownerPath=Join-Path $Work.Path 'owner.json'
            $owner=Get-Content -LiteralPath $ownerPath -Raw | ConvertFrom-Json
            $owner.keep=$true
            Write-KjJson $ownerPath $owner
            Write-Warning ('Build rollback needs inspection; full workspace pinned: ' + $Work.Path)
        }
        throw $failure
    }
}
