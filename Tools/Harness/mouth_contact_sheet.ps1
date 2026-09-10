param([Parameter(Mandatory=$true)][string]$Folder, [Parameter(Mandatory=$true)][string]$Output)
Add-Type -AssemblyName System.Drawing
$records = @(Get-Content (Join-Path $Folder 'index.txt') | Where-Object { $_ -match '^\d{3} ' })
$sheet = New-Object System.Drawing.Bitmap 1440, ([int][Math]::Ceiling($records.Count / 4) * 276)
$graphics = [System.Drawing.Graphics]::FromImage($sheet)
$font = New-Object System.Drawing.Font 'Arial', 10
try {
    $graphics.Clear([System.Drawing.Color]::White)
    for ($i = 0; $i -lt $records.Count; $i++) {
        $record = $records[$i]
        if ($record -notmatch '^(\d{3}).*heading=(\w+) open=([\d.]+) score=(\d+) x=(-?\d+) y=(-?\d+)') { continue }
        $name = $Matches[1]
        if (!(Test-Path -LiteralPath (Join-Path $Folder "$name.png"))) { continue }
        $label = "$name $($Matches[2]) open=$($Matches[3]) score=$($Matches[4])"
        $cx = [int]$Matches[5]
        $cy = [int]$Matches[6]
        $frame = [System.Drawing.Image]::FromFile([System.IO.Path]::GetFullPath((Join-Path $Folder "$name.png")))
        try {
            $x = ($i % 4) * 360
            $y = [int][Math]::Floor($i / 4) * 276
            $cropX = [Math]::Max(0, [Math]::Min($frame.Width - 360, $cx - 180))
            $cropY = [Math]::Max(0, [Math]::Min($frame.Height - 250, $cy - 145))
            $source = New-Object System.Drawing.Rectangle $cropX, $cropY, 360, 250
            $target = New-Object System.Drawing.Rectangle $x, $y, 360, 250
            $graphics.DrawImage($frame, $target, $source, [System.Drawing.GraphicsUnit]::Pixel)
            $graphics.DrawString($label, $font, [System.Drawing.Brushes]::Black, $x, ($y + 251))
        } finally { $frame.Dispose() }
    }
    $sheet.Save([System.IO.Path]::GetFullPath($Output), [System.Drawing.Imaging.ImageFormat]::Png)
} finally { $graphics.Dispose(); $font.Dispose(); $sheet.Dispose() }
