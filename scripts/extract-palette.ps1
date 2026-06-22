# Samples the TaskPilot master icon and reports the dominant colors (quantized).
Add-Type -AssemblyName System.Drawing
$path = "C:\projects\TaskPilot\assets\brand-icons\taskpilot-icon-2048.png"
$bmp = [System.Drawing.Bitmap]::FromFile($path)
$counts = @{}
$step = 16  # sample every 16px for speed
for ($x = 0; $x -lt $bmp.Width; $x += $step) {
  for ($y = 0; $y -lt $bmp.Height; $y += $step) {
    $px = $bmp.GetPixel($x, $y)
    if ($px.A -lt 200) { continue }                       # skip transparent
    # quantize to 24-step buckets to merge near-identical shades
    $r = [int]([math]::Round($px.R / 24.0) * 24)
    $g = [int]([math]::Round($px.G / 24.0) * 24)
    $b = [int]([math]::Round($px.B / 24.0) * 24)
    $key = "{0},{1},{2}" -f $r, $g, $b
    if ($counts.ContainsKey($key)) { $counts[$key]++ } else { $counts[$key] = 1 }
  }
}
$bmp.Dispose()
$total = ($counts.Values | Measure-Object -Sum).Sum
$counts.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 18 | ForEach-Object {
  $parts = $_.Key -split ','
  $hex = "#{0:X2}{1:X2}{2:X2}" -f [int]$parts[0], [int]$parts[1], [int]$parts[2]
  $pct = [math]::Round(100.0 * $_.Value / $total, 1)
  "{0}  rgb({1})  {2}%" -f $hex, $_.Key, $pct
}