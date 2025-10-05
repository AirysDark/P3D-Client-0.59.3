<#
.SYNOPSIS
Normalizes all line endings in text/script files to Windows CRLF
.DESCRIPTION
Fixes any LF/CR mismatches in .txt, .as, .script, and .dat files to prevent VB parsing errors.
#>

$root = Split-Path -Parent $MyInvocation.MyCommand.Definition
$target = Join-Path $root "..\P3D"
$patterns = @("*.txt","*.as","*.script","*.dat")

Write-Host "? Normalizing line endings under $target..."

Get-ChildItem $target -Recurse -File -Include $patterns | ForEach-Object {
  $raw = Get-Content $_.FullName -Raw
  $fixed = [System.Text.RegularExpressions.Regex]::Replace($raw, "(\r?\n)", "`r`n")
  if ($fixed -ne $raw) {
    Set-Content $_.FullName -Value $fixed -NoNewline
    Write-Host "? Fixed line endings -> $($_.FullName)"
  }
}
Write-Host "All done!"