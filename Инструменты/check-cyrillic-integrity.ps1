param(
    [string]$Root = "",
    [switch]$IncludeBackups,
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = (Resolve-Path -Path (Join-Path $PSScriptRoot "..")).Path
}
else {
    $Root = (Resolve-Path -Path $Root).Path
}

$utf8Strict = New-Object System.Text.UTF8Encoding($false, $true)
$textExtensions = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
@(
    ".md", ".txt", ".ps1", ".psm1", ".psd1",
    ".cs", ".xaml", ".csproj", ".sln",
    ".json", ".yaml", ".yml", ".xml",
    ".props", ".targets", ".config", ".editorconfig"
) | ForEach-Object { [void]$textExtensions.Add($_) }

$excludedDirs = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
@(
    ".git",
    ".vs",
    "bin",
    "obj",
    "node_modules",
    ".cache",
    ".venv",
    "Runtime",
    "Модели"
) | ForEach-Object { [void]$excludedDirs.Add($_) }
if (-not $IncludeBackups) {
    [void]$excludedDirs.Add("Backups")
}

function New-Pattern {
    param(
        [string]$Name,
        [string]$Regex
    )

    [pscustomobject]@{
        Name = $Name
        Regex = [regex]::new($Regex)
    }
}

$replacementChar = [string][char]0xFFFD
$latinMojibakeChars = (
    [string][char]0x00C2 +
    [string][char]0x00C3 +
    [string][char]0x00D0 +
    [string][char]0x00D1
)

$win1251Fragments = @(
    [string]([char]0x0420) + [string]([char]0x045F),
    [string]([char]0x0420) + [string]([char]0x0455),
    [string]([char]0x0420) + [string]([char]0x0454),
    [string]([char]0x0420) + [string]([char]0x0456),
    [string]([char]0x0420) + [string]([char]0x0451),
    [string]([char]0x0421) + [string]([char]0x0402),
    [string]([char]0x0421) + [string]([char]0x201A),
    [string]([char]0x0421) + [string]([char]0x201E)
)

$patterns = @(
    (New-Pattern -Name "replacement character" -Regex ([regex]::Escape($replacementChar))),
    (New-Pattern -Name "latin mojibake marker" -Regex ("[" + [regex]::Escape($latinMojibakeChars) + "]")),
    (New-Pattern -Name "utf8 read as windows-1251 marker" -Regex (($win1251Fragments | ForEach-Object { [regex]::Escape($_) }) -join "|"))
)

function Get-TextFiles {
    param([string]$StartPath)

    $stack = New-Object "System.Collections.Generic.Stack[string]"
    $stack.Push($StartPath)

    while ($stack.Count -gt 0) {
        $current = $stack.Pop()

        foreach ($dir in [System.IO.Directory]::EnumerateDirectories($current)) {
            $name = [System.IO.Path]::GetFileName($dir)
            if (-not $excludedDirs.Contains($name)) {
                $stack.Push($dir)
            }
        }

        foreach ($file in [System.IO.Directory]::EnumerateFiles($current)) {
            $extension = [System.IO.Path]::GetExtension($file)
            if ($textExtensions.Contains($extension) -or [System.IO.Path]::GetFileName($file) -eq "LICENSE") {
                $file
            }
        }
    }
}

$issues = New-Object "System.Collections.Generic.List[object]"
$filesChecked = 0

foreach ($file in Get-TextFiles -StartPath $Root) {
    $filesChecked++
    $bytes = [System.IO.File]::ReadAllBytes($file)

    try {
        $text = $utf8Strict.GetString($bytes)
    }
    catch {
        $issues.Add([pscustomobject]@{
            File = $file
            Line = 0
            Column = 0
            Type = "invalid UTF-8"
            Text = $_.Exception.Message
        })
        continue
    }

    $lines = $text -split "`r?`n"
    for ($lineIndex = 0; $lineIndex -lt $lines.Count; $lineIndex++) {
        $line = $lines[$lineIndex]
        foreach ($pattern in $patterns) {
            $match = $pattern.Regex.Match($line)
            if ($match.Success) {
                $excerptStart = [Math]::Max(0, $match.Index - 20)
                $excerptLength = [Math]::Min($line.Length - $excerptStart, 80)
                $issues.Add([pscustomobject]@{
                    File = $file
                    Line = $lineIndex + 1
                    Column = $match.Index + 1
                    Type = $pattern.Name
                    Text = $line.Substring($excerptStart, $excerptLength)
                })
            }
        }
    }
}

if ($issues.Count -eq 0) {
    if (-not $Quiet) {
        Write-Host "OK: checked $filesChecked text files. No UTF-8 or Cyrillic mojibake issues found."
    }
    exit 0
}

Write-Host "Found $($issues.Count) possible UTF-8/Cyrillic issue(s) in $filesChecked text files."
foreach ($issue in $issues) {
    Write-Host ""
    Write-Host "$($issue.File):$($issue.Line):$($issue.Column)"
    Write-Host "Type: $($issue.Type)"
    Write-Host "Text: $($issue.Text)"
}

exit 1
