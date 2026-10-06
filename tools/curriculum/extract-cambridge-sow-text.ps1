param(
  [string]$Repo = "C:\Users\Public\Edulytics-work"
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get-DocxText([string]$Path) {
  $zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
  try {
    $entry = $zip.GetEntry("word/document.xml")
    if (-not $entry) { throw "word/document.xml not found: $Path" }
    $reader = New-Object System.IO.StreamReader($entry.Open())
    try { $xml = $reader.ReadToEnd() } finally { $reader.Dispose() }
  } finally {
    $zip.Dispose()
  }
  $text = [regex]::Replace($xml, '<w:tab[^>]*/>', ' ')
  $text = [regex]::Replace($text, '</w:p>', [Environment]::NewLine)
  $text = [regex]::Replace($text, '<[^>]+>', '')
  return [System.Net.WebUtility]::HtmlDecode($text)
}

$inputRoot = Join-Path $Repo "artifacts\curriculum-sources\cambridge\teaching-sources"
$outRoot = Join-Path $Repo "artifacts\cambridge-official-audit\sow-text"
New-Item -ItemType Directory -Force $outRoot | Out-Null

1..6 | ForEach-Object {
  $src = Join-Path $inputRoot ("primary\0096_STAGE{0}_SCHEME_OF_WORK.docx" -f $_)
  $dst = Join-Path $outRoot ("0096_STAGE{0}_SOW.txt" -f $_)
  Get-DocxText $src | Set-Content -Encoding UTF8 $dst
  Write-Output $dst
}
7..9 | ForEach-Object {
  $src = Join-Path $inputRoot ("lower-secondary\0862_STAGE{0}_SCHEME_OF_WORK.docx" -f $_)
  $dst = Join-Path $outRoot ("0862_STAGE{0}_SOW.txt" -f $_)
  Get-DocxText $src | Set-Content -Encoding UTF8 $dst
  Write-Output $dst
}
