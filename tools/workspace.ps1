# Development-only ownership and retention. Never imported by the resident.
function Assert-KjOutputPath($Root, $Path) {
    $rootPath = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $outPath = Join-Path $rootPath 'out'
    $full = [IO.Path]::GetFullPath($Path)
    if (-not $full.StartsWith($outPath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw ('Generated path must be beneath out: ' + $full)
    }
    $cursor = $full
    while ($cursor -and -not [string]::Equals($cursor, $rootPath, [StringComparison]::OrdinalIgnoreCase)) {
        if (Test-Path -LiteralPath $cursor) {
            if ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw ('Generated path contains a link: ' + $cursor)
            }
        }
        $cursor = Split-Path $cursor -Parent
    }
    return $full
}

function Get-KjFiles($Root, $Path) {
    $full = Assert-KjOutputPath $Root $Path
    $pending = New-Object 'Collections.Generic.Stack[string]'
    $pending.Push($full)
    while ($pending.Count) {
        foreach ($item in (Get-ChildItem -LiteralPath $pending.Pop() -Force)) {
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw ('Generated tree contains a link: ' + $item.FullName) }
            if ($item.PSIsContainer) { $pending.Push($item.FullName) } else { $item }
        }
    }
}

function Write-KjJson($Path, $Value) {
    [IO.File]::WriteAllText($Path, (ConvertTo-Json -InputObject $Value -Depth 12), (New-Object Text.UTF8Encoding($false)))
}

function Get-KjInventory($Root, $Path) {
    $items = @{}
    foreach ($file in @(Get-KjFiles $Root $Path)) {
        $name = $file.FullName.Substring($Path.Length + 1).Replace('\', '/')
        $items[$name] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    }
    return $items
}

function Test-KjInventory($Root, $Path, $Expected) {
    $actual = Get-KjInventory $Root $Path
    $names = @($Expected.PSObject.Properties.Name | Where-Object { $null -ne $_ })
    if ($Expected -is [Collections.IDictionary]) { $names = @($Expected.Keys) }
    if ($actual.Count -ne $names.Count) { return $false }
    foreach ($name in $names) {
        $value = if ($Expected -is [Collections.IDictionary]) { $Expected[$name] } else { $Expected.$name }
        if ($actual[$name] -ne $value) { return $false }
    }
    return $true
}

function Remove-KjReportHistory($Root, $Kind) {
    $parent = Join-Path $Root ('out/reports/' + $Kind)
    if (-not (Test-Path -LiteralPath $parent)) { return }
    $null = Assert-KjOutputPath $Root $parent
    $reports = @()
    foreach ($directory in (Get-ChildItem -LiteralPath $parent -Directory)) {
        if ($directory.Name -notmatch '^[a-f0-9]{32}$') { continue }
        try {
            $null = @(Get-KjFiles $Root $directory.FullName)
            if (Test-Path -LiteralPath (Join-Path $directory.FullName '.keep')) { continue }
            $markerPath = Join-Path $directory.FullName 'report.json'
            if ((Get-FileHash -LiteralPath $markerPath -Algorithm SHA256).Hash -ne (Get-Content -LiteralPath (Join-Path $directory.FullName 'report.sha256') -Raw).Trim()) { continue }
            $marker = Get-Content -LiteralPath $markerPath -Raw | ConvertFrom-Json
            if ($marker.schema -ne 'keyjaya-report-1' -or $marker.kind -ne $Kind -or $marker.id -ne $directory.Name -or -not $marker.finished -or $marker.pinned) { continue }
            $reports += @{ directory=$directory.FullName; marker=$marker }
        } catch { continue }
    }
    $ordered = @($reports | Sort-Object { $_.marker.finished_utc } -Descending)
    $keep = @($ordered | Select-Object -First 5 | ForEach-Object { $_.directory })
    $failure = $ordered | Where-Object { -not $_.marker.success } | Select-Object -First 1
    if ($failure) { $keep += $failure.directory }
    foreach ($report in $ordered) {
        if ($report.directory -in $keep) { continue }
        try {
            # A changed report or an added user file is not disposable evidence.
            $actual = Get-KjInventory $Root $report.directory
            $actual.Remove('report.json')
            $actual.Remove('report.sha256')
            $expected = $report.marker.files
            $names = @($expected.PSObject.Properties.Name | Where-Object { $null -ne $_ })
            if ($actual.Count -ne $names.Count) { continue }
            $same = $true
            foreach ($name in $names) { if ($actual[$name] -ne $expected.$name) { $same=$false; break } }
            if ($same) { Remove-Item -LiteralPath $report.directory -Recurse -Force -ErrorAction Stop }
        } catch { Write-Warning ('Older report preserved: ' + $_.Exception.Message) }
    }
}

function Save-KjFailure($Work) {
    Add-Type -AssemblyName System.IO.Compression
    $files = @(Get-KjFiles $Work.Root $Work.Path)
    $zipPath = Join-Path $Work.Evidence 'failure.zip'
    $stream = [IO.File]::Create($zipPath)
    $zip = New-Object IO.Compression.ZipArchive($stream, [IO.Compression.ZipArchiveMode]::Create)
    $index = New-Object 'Collections.Generic.List[object]'
    $bytes = [long]0
    try {
        $sources = @($Work.Inputs | ForEach-Object { Get-Item -LiteralPath $_ })
        $candidates = @($sources) + @($files | Sort-Object @{Expression={ if ($_.FullName -match '[\\/](config|state)[\\/]') { 0 } else { 1 } }}, FullName)
        $seen = @{}
        foreach ($file in $candidates) {
            if ($seen.ContainsKey($file.FullName)) { continue }
            $seen[$file.FullName] = $true
            $sourceFile = $file.FullName -in $Work.Inputs
            $name = if ($sourceFile) { 'source/' + $file.FullName.Substring($Work.Root.Length + 1).Replace('\','/') } else { 'work/' + $file.FullName.Substring($Work.Path.Length + 1).Replace('\','/') }
            $capture = $file.Length -le 1MB -and ($bytes + $file.Length) -le 16MB -and
                $file.Extension -notin @('.dat','.idx','.gz','.exe','.dll','.pdb','.zip') -and $file.Name -notin @('.lease','owner.json')
            $row = @{ path=$name; bytes=$file.Length; captured=$capture }
            if ($capture) {
                $row.sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
                $entry = $zip.CreateEntry($name)
                $input = [IO.File]::OpenRead($file.FullName)
                $output = $entry.Open()
                try { $input.CopyTo($output) } finally { $input.Dispose(); $output.Dispose() }
                $bytes += $file.Length
            }
            $index.Add($row)
        }
        $entry = $zip.CreateEntry('inventory.json')
        $writer = New-Object IO.StreamWriter($entry.Open(), (New-Object Text.UTF8Encoding($false)))
        try { $writer.Write((ConvertTo-Json -InputObject @($index.ToArray()) -Depth 5)) } finally { $writer.Dispose() }
    } finally { $zip.Dispose(); $stream.Dispose() }
    # Prove captured bytes before deleting the originals.
    $stream = [IO.File]::OpenRead($zipPath)
    $zip = New-Object IO.Compression.ZipArchive($stream, [IO.Compression.ZipArchiveMode]::Read)
    try {
        foreach ($row in $index) {
            if (-not $row.captured) { continue }
            $input = $zip.GetEntry($row.path).Open()
            $sha = [Security.Cryptography.SHA256]::Create()
            try { $hash = [BitConverter]::ToString($sha.ComputeHash($input)).Replace('-','') } finally { $sha.Dispose(); $input.Dispose() }
            if ($hash -ne $row.sha256) { throw ('Failure evidence differs: ' + $row.path) }
        }
    } finally { $zip.Dispose(); $stream.Dispose() }
}

function Complete-KjWork($Work, [bool]$Success) {
    $cleanup = 'kept'
    $problem = $null
    try {
        $null = Assert-KjOutputPath $Work.Root $Work.Path
        $expected = Join-Path $Work.Root ('out/w/' + $Work.Id.Substring(0,8))
        if ($Work.Path -ne $expected) { throw 'Workspace identity changed.' }
        $ownerPath = Assert-KjOutputPath $Work.Root (Join-Path $Work.Path 'owner.json')
        $owner = Get-Content -LiteralPath $ownerPath -Raw | ConvertFrom-Json
        if ($owner.schema -ne 'keyjaya-work-1' -or $owner.token -ne $Work.Token -or $owner.id -ne $Work.Id -or $owner.kind -ne $Work.Kind) { throw 'Workspace ownership changed.' }
        if (-not $Success) { Save-KjFailure $Work }
        $null = @(Get-KjFiles $Work.Root $Work.Path)
        if (-not $Work.Keep) {
            $Work.Lease.Dispose()
            Remove-Item -LiteralPath $Work.Path -Recurse -Force -ErrorAction Stop
            $cleanup = 'removed'
        }
    } catch {
        $problem = $_.Exception.Message
        $cleanup = 'preserved_after_error'
    } finally { $Work.Lease.Dispose() }
    $inventory = Get-KjInventory $Work.Root $Work.Evidence
    $inventory.Remove('report.json')
    $inventory.Remove('report.sha256')
    $reportPath = Join-Path $Work.Evidence 'report.json'
    Write-KjJson $reportPath @{
        schema='keyjaya-report-1'; kind=$Work.Kind; id=$Work.Id; finished=$true; success=$Success; pinned=$Work.Keep
        finished_utc=[datetime]::UtcNow.ToString('o'); cleanup=$cleanup; cleanup_error=$problem; files=$inventory
    }
    [IO.File]::WriteAllText((Join-Path $Work.Evidence 'report.sha256'), (Get-FileHash -LiteralPath $reportPath -Algorithm SHA256).Hash)
    Remove-KjReportHistory $Work.Root $Work.Kind
    if ($problem) {
        if ($Success) { throw ('Temporary workspace preserved: ' + $problem) }
        Write-Warning ('Temporary workspace preserved: ' + $problem)
    }
}

function New-KjWork($Root, $Kind, [switch]$KeepArtifacts) {
    if ($Kind -notmatch '^[a-z][a-z-]{1,30}$') { throw 'Invalid workspace kind.' }
    $Root = [IO.Path]::GetFullPath($Root).TrimEnd('\','/')
    $parent = Join-Path $Root 'out/w'
    $null = Assert-KjOutputPath $Root $parent
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
    # Recover only marked workspaces whose original owner has exited. No glob
    # over historical out/ folders, trial profiles, releases or user documents.
    foreach ($directory in (Get-ChildItem -LiteralPath $parent -Directory)) {
        if ($directory.Name -notmatch '^[a-f0-9]{8}$') { continue }
        $lease=$null
        try {
            $null = Assert-KjOutputPath $Root $directory.FullName
            $ownerPath = Assert-KjOutputPath $Root (Join-Path $directory.FullName 'owner.json')
            $owner = Get-Content -LiteralPath $ownerPath -Raw | ConvertFrom-Json
            if ($owner.schema -ne 'keyjaya-work-1' -or $owner.id -notmatch '^[a-f0-9]{32}$' -or $owner.id.Substring(0,8) -ne $directory.Name -or $owner.kind -notmatch '^[a-z][a-z-]{1,30}$' -or $owner.keep -or $owner.token -notmatch '^[a-f0-9]{32}$') { continue }
            $oldKind=$owner.kind; $oldId=$owner.id
            $process = Get-Process -Id $owner.pid -ErrorAction SilentlyContinue
            if ($process -and (-not $owner.started_ticks -or $process.StartTime.ToUniversalTime().Ticks -eq $owner.started_ticks)) { continue }
            $leasePath = Assert-KjOutputPath $Root (Join-Path $directory.FullName '.lease')
            try { $lease = [IO.File]::Open($leasePath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
            catch [IO.IOException] {
                # Process enumeration can be restricted. A held lease still
                # proves that this workspace is busy, not abandoned.
                if (($_.Exception.HResult -band 0xffff) -in @(32,33)) { continue }
                throw
            }
            $evidence = Join-Path $Root ('out/reports/' + $oldKind + '/' + $oldId)
            $null = Assert-KjOutputPath $Root $evidence
            New-Item -ItemType Directory -Path $evidence -Force | Out-Null
            $old = [pscustomobject]@{ Root=$Root; Path=$directory.FullName; Evidence=$evidence; Kind=$oldKind; Id=$oldId; Token=$owner.token; Keep=$false; Lease=$lease; Inputs=@() }
            Write-KjJson (Join-Path $evidence 'interrupted.json') @{ interrupted=$true; previous_pid=$owner.pid }
            Complete-KjWork $old $false
        } catch { Write-Warning ('Abandoned workspace preserved: ' + $_.Exception.Message) }
        finally { if ($lease) { $lease.Dispose() } }
    }
    $id=[Guid]::NewGuid().ToString('N'); $token=[Guid]::NewGuid().ToString('N')
    $path=Join-Path $parent $id.Substring(0,8)
    $evidence=Join-Path $Root ('out/reports/' + $Kind + '/' + $id)
    $null = Assert-KjOutputPath $Root $evidence
    New-Item -ItemType Directory -Path $path, $evidence | Out-Null
    $lease = [IO.File]::Open((Join-Path $path '.lease'), [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    Write-KjJson (Join-Path $path 'owner.json') @{ schema='keyjaya-work-1'; id=$id; kind=$Kind; token=$token; pid=$PID; started_ticks=(Get-Process -Id $PID).StartTime.ToUniversalTime().Ticks; keep=[bool]$KeepArtifacts }
    Write-KjJson (Join-Path $evidence 'environment.json') @{ powershell=$PSVersionTable.PSVersion.ToString(); os=[Environment]::OSVersion.ToString(); bitness=([IntPtr]::Size*8); created_utc=[datetime]::UtcNow.ToString('o') }
    return [pscustomobject]@{ Root=$Root; Path=$path; Evidence=$evidence; Kind=$Kind; Id=$id; Token=$token; Keep=[bool]$KeepArtifacts; Lease=$lease; Inputs=@() }
}

function Set-KjInputs($Work, $Paths) {
    $Paths = @($Paths) + @('tools/workspace.ps1','tools/build-output.ps1' | ForEach-Object { Join-Path $Work.Root $_ } | Where-Object { Test-Path -LiteralPath $_ })
    $Work.Inputs = @($Paths | ForEach-Object { [IO.Path]::GetFullPath($_) } | Select-Object -Unique)
    $items = @()
    foreach ($path in $Work.Inputs) {
        $full = [IO.Path]::GetFullPath($path)
        if (-not $full.StartsWith($Work.Root + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Input outside project.' }
        $items += @{ path=$full.Substring($Work.Root.Length+1).Replace('\','/'); sha256=(Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash }
    }
    Write-KjJson (Join-Path $Work.Evidence 'inputs.json') @($items)
}

function Remove-KjScratchChild($Work, $Path) {
    $full = Assert-KjOutputPath $Work.Root $Path
    if (-not $full.StartsWith($Work.Path + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Not a child of this workspace.' }
    $ownerPath = Assert-KjOutputPath $Work.Root (Join-Path $Work.Path 'owner.json')
    $owner = Get-Content -LiteralPath $ownerPath -Raw | ConvertFrom-Json
    if ($owner.token -ne $Work.Token -or $owner.id -ne $Work.Id) { throw 'Workspace ownership changed.' }
    $null = @(Get-KjFiles $Work.Root $full)
    Remove-Item -LiteralPath $full -Recurse -Force -ErrorAction Stop
}

function Invoke-KjLogged($Work, $File, $Arguments, $Name) {
    $saved = $ErrorActionPreference
    $lines=@(); $code=1
    try {
        $null = Get-Command $File -ErrorAction Stop
        $ErrorActionPreference='Continue'
        $lines = @(& $File @Arguments 2>&1)
        $code=$LASTEXITCODE
    } catch { $lines += $_; $code=1 }
    finally { $ErrorActionPreference=$saved }
    $text = ($lines | ForEach-Object { $_.ToString() }) -join "`n"
    if (-not $Work.Keep -and $text.Length -gt 262144) { $text=$text.Substring(0,131072)+"`n[output truncated; use -KeepArtifacts for investigation]`n"+$text.Substring($text.Length-131072) }
    [IO.File]::WriteAllText((Join-Path $Work.Evidence $Name), $text, (New-Object Text.UTF8Encoding($false)))
    return [pscustomobject]@{ ExitCode=$code; Output=$text }
}

function Clear-KjScratch($Work) {
    foreach ($item in (Get-ChildItem -LiteralPath $Work.Path -Force)) {
        if ($item.Name -notin @('owner.json','.lease')) { Remove-KjScratchChild $Work $item.FullName }
    }
}
