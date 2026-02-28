param(
    [string[]]$Files
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $Files -or $Files.Count -eq 0) {
    $Files = @(git ls-files "*appsettings*.json")
}

$violations = @()

foreach ($file in $Files) {
    if (-not (Test-Path -LiteralPath $file)) {
        continue
    }

    $content = Get-Content -LiteralPath $file -Raw
    $connectionStringMatches = [regex]::Matches($content, '"NetstrDatabase"\s*:\s*"([^"]*)"')

    foreach ($match in $connectionStringMatches) {
        $connectionString = $match.Groups[1].Value
        if ([string]::IsNullOrWhiteSpace($connectionString)) {
            continue
        }

        $passwordMatch = [regex]::Match($connectionString, '(?i)(?:^|;)Password\s*=\s*([^;"]*)')
        if (-not $passwordMatch.Success) {
            continue
        }

        $passwordValue = $passwordMatch.Groups[1].Value.Trim()
        if ([string]::IsNullOrWhiteSpace($passwordValue)) {
            continue
        }

        $isPlaceholder =
            $passwordValue -match '^\[YOUR-PASSWORD\]$' -or
            $passwordValue -match '^<[^>]+>$' -or
            $passwordValue -match '^\$\{[A-Z0-9_]+\}$' -or
            $passwordValue -match '^__[^_]+__$'

        if (-not $isPlaceholder) {
            $violations += "${file}: ConnectionStrings:NetstrDatabase contains a non-placeholder Password value."
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "Hardcoded database password values were found:"
    foreach ($violation in $violations) {
        Write-Host " - $violation"
    }

    Write-Host ""
    Write-Host "Move secrets to appsettings.local.json (gitignored) or environment variables."
    exit 1
}

Write-Host "No hardcoded database passwords found in tracked appsettings files."
