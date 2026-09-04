param(
    [Parameter(Mandatory=$true)]
    [string]$ErrorLog
)

# Read captured build output
if (-not (Test-Path $ErrorLog)) {
    Write-Error ('Error log not found: ' + $ErrorLog)
    exit 1
}
$errorContent = Get-Content $ErrorLog -Raw

# Parse source URL from NU1301/401 error
$urlMatch = [regex]::Match($errorContent, 'https://nuget\.pkg\.github\.com/[^\s''"\r\n]+')
$parsedUrl = $null
if ($urlMatch.Success) { $parsedUrl = $urlMatch.Value.TrimEnd('.') }

# Normalize GitHub package download endpoint to feed index endpoint
if ($parsedUrl) {
    $orgFromParsed = [regex]::Match($parsedUrl, 'https://nuget\.pkg\.github\.com/([^/]+)/')
    if ($orgFromParsed.Success) {
        $parsedUrl = 'https://nuget.pkg.github.com/' + $orgFromParsed.Groups[1].Value + '/index.json'
    }
}

# Detect error type
$isNU1100 = $errorContent -match 'NU1100'

# List existing NuGet sources
$nugetSources = @()
$sourceListOutput = & dotnet nuget list source 2>&1
$currentName = $null
foreach ($line in $sourceListOutput) {
    if ($line -match '^\s+\d+\.\s+(.+?)\s+\[') {
        $currentName = $Matches[1].Trim()
    } elseif ($line -match '^\s+(https?://\S+)' -and $currentName) {
        $nugetSources += [PSCustomObject]@{ Name = $currentName; Url = $Matches[1].Trim() }
        $currentName = $null
    }
}

# Try to match existing source by exact URL or org name
$matchedSource = $null
if ($parsedUrl) {
    $matchedSource = $nugetSources | Where-Object { $_.Url -eq $parsedUrl } | Select-Object -First 1
    if (-not $matchedSource) {
        $orgMatch = [regex]::Match($parsedUrl, 'nuget\.pkg\.github\.com/([^/]+)')
        if ($orgMatch.Success) {
            $org = $orgMatch.Groups[1].Value
            $matchedSource = $nugetSources | Where-Object { $_.Url -like ('*' + $org + '*') } | Select-Object -First 1
        }
    }
}

# Build prompt defaults
$defaultUrl = if ($parsedUrl) { $parsedUrl } else { 'https://nuget.pkg.github.com/Official-EwE/index.json' }
$defaultName = if ($matchedSource) {
    $matchedSource.Name
} else {
    $orgMatch2 = [regex]::Match($defaultUrl, 'nuget\.pkg\.github\.com/([^/]+)')
    if ($orgMatch2.Success) { 'github-' + $orgMatch2.Groups[1].Value } else { 'github-packages' }
}

# Show header
Write-Host ''
Write-Host '=== NuGet Package Resolution Error - Fix Credentials / Source ===' -ForegroundColor Yellow

if ($isNU1100) {
    Write-Host 'Source is missing or excluded by PackageSourceMapping.' -ForegroundColor DarkYellow
}
Write-Host ''

# Prompt user
$inputUrl = Read-Host ('Source URL [' + $defaultUrl + ']')
if ([string]::IsNullOrWhiteSpace($inputUrl)) { $inputUrl = $defaultUrl }

$inputName = Read-Host ('Source name [' + $defaultName + ']')
if ([string]::IsNullOrWhiteSpace($inputName)) { $inputName = $defaultName }

$inputUser = Read-Host 'GitHub username'
if ([string]::IsNullOrWhiteSpace($inputUser)) {
    Write-Host 'Username cannot be empty.' -ForegroundColor Red
    exit 1
}

$secPat = Read-Host 'GitHub PAT' -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secPat)
$inputPat = [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)

if ([string]::IsNullOrWhiteSpace($inputPat)) {
    Write-Host 'PAT cannot be empty.' -ForegroundColor Red
    exit 1
}

# Update or add source
$sourceExists = $nugetSources | Where-Object { $_.Name -eq $inputName } | Select-Object -First 1

if ($sourceExists) {
    Write-Host ''
    Write-Host ('Updating existing source "' + $inputName + '" (' + $inputUrl + ')...') -ForegroundColor Cyan
    & dotnet nuget update source $inputName `
        --source $inputUrl `
        --username $inputUser `
        --password $inputPat `
        --store-password-in-clear-text
} else {
    Write-Host ''
    Write-Host ('Adding new source "' + $inputName + '" (' + $inputUrl + ')...') -ForegroundColor Cyan
    & dotnet nuget add source $inputUrl `
        --name $inputName `
        --username $inputUser `
        --password $inputPat `
        --store-password-in-clear-text
}

$result = $LASTEXITCODE
if ($result -ne 0) {
    Write-Host ('Failed to configure NuGet source. Exit code: ' + $result) -ForegroundColor Red
}
exit $result
