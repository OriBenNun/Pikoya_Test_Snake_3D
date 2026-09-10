param([Parameter(Mandatory=$true)][string]$Pattern, [Parameter(Mandatory=$true)][string]$Output)
Add-Type -AssemblyName System.Drawing
$frames = @(Get-ChildItem -Path $Pattern | Sort-Object Name)
if ($frames.Count -eq 0) { throw 'No capture frames matched.' }
$columns = 4
$width = 480
$height = 292
$sheet = New-Object System.Drawing.Bitmap ($columns * $width), ([int][Math]::Ceiling($frames.Count / $columns) * $height)
$graphics = [System.Drawing.Graphics]::FromImage($sheet)
$font = New-Object System.Drawing.Font 'Arial', 11
try {
    $graphics.Clear([System.Drawing.Color]::White)
    for ($i = 0; $i -lt $frames.Count; $i++) {
        $frame = [System.Drawing.Image]::FromFile($frames[$i].FullName)
        try {
            $x = ($i % $columns) * $width
            $y = [int][Math]::Floor($i / $columns) * $height
            $graphics.DrawImage($frame, $x, $y, $width, 270)
            $graphics.DrawString($frames[$i].BaseName, $font, [System.Drawing.Brushes]::Black, $x, ($y + 270))
        } finally { $frame.Dispose() }
    }
    $sheet.Save([System.IO.Path]::GetFullPath($Output), [System.Drawing.Imaging.ImageFormat]::Png)
} finally { $graphics.Dispose(); $font.Dispose(); $sheet.Dispose() }
