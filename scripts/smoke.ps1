<#
.SYNOPSIS
    Post-deploy smoke test for TaskPilot. Verifies the correct version is deployed and all
    required health checks pass.

.PARAMETER BaseUrl
    Base URL of the deployed application.
    Defaults to $env:SMOKE_BASE_URL or http://localhost:5125 if not specified.

.PARAMETER ExpectedCommit
    Expected git commit SHA (full or short). When provided, the script asserts the deployed
    commit matches. Can also be set via $env:EXPECTED_COMMIT.

.EXAMPLE
    .\smoke.ps1 -BaseUrl http://localhost:5125

.EXAMPLE
    .\smoke.ps1 -BaseUrl https://taskpilot.azurewebsites.net -ExpectedCommit a1b2c3d
#>
param(
    [string]$BaseUrl     = $env:SMOKE_BASE_URL ?? "http://localhost:5125",
    [string]$ExpectedCommit = $env:EXPECTED_COMMIT ?? ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$failures = [System.Collections.Generic.List[string]]::new()

function Assert-Ok([string]$label, [scriptblock]$test) {
    try {
        & $test
        Write-Host "[PASS] $label" -ForegroundColor Green
    } catch {
        Write-Host "[FAIL] $label — $($_.Exception.Message)" -ForegroundColor Red
        $failures.Add($label)
    }
}

Write-Host "`n=== TaskPilot Smoke Test — $BaseUrl ===`n" -ForegroundColor Cyan

# HLTH-060: version endpoint reachable
$versionData = $null
Assert-Ok "HLTH-060: version endpoint reachable" {
    $response = Invoke-RestMethod "$BaseUrl/api/v1/health/version" -TimeoutSec 15
    if ($response.data -eq $null) { throw "No 'data' field in envelope" }
    if ([string]::IsNullOrWhiteSpace($response.data.version)) { throw "Version is empty" }
    $script:versionData = $response.data
    Write-Host "         Deployed version: $($response.data.version) @ $($response.data.gitCommitShort)" -ForegroundColor Gray
}

# HLTH-061: deployed commit matches expected (optional check)
if (-not [string]::IsNullOrWhiteSpace($ExpectedCommit) -and $versionData -ne $null) {
    Assert-Ok "HLTH-061: deployed commit matches expected" {
        $expected = $ExpectedCommit.Substring(0, [Math]::Min(7, $ExpectedCommit.Length)).ToLowerInvariant()
        $actual   = $versionData.gitCommitShort.ToLowerInvariant()
        if ($actual -ne $expected) {
            throw "Version mismatch: deployed '$actual', expected '$expected'"
        }
    }
} else {
    Write-Host "[SKIP] HLTH-061: no ExpectedCommit supplied" -ForegroundColor Yellow
}

# HLTH-062: full health check is green
Assert-Ok "HLTH-062: full health check is green" {
    $r = Invoke-WebRequest "$BaseUrl/api/v1/health/full" -SkipHttpErrorCheck -TimeoutSec 30
    if ($r.StatusCode -ne 200) { throw "Status = $($r.StatusCode) (expected 200)" }
    $body = $r.Content | ConvertFrom-Json
    if ($body.data.status -ne "healthy") { throw "Overall status = '$($body.data.status)'" }
}

# HLTH-063: no CDN caching
Assert-Ok "HLTH-063: no CDN caching (no Age header, consistent version)" {
    $buster1 = [System.DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $r1 = Invoke-WebRequest "$BaseUrl/api/v1/health/version?_=$buster1" -TimeoutSec 15
    Start-Sleep -Milliseconds 200
    $buster2 = [System.DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $r2 = Invoke-WebRequest "$BaseUrl/api/v1/health/version?_=$buster2" -TimeoutSec 15
    if ($r1.Headers["Age"] -or $r2.Headers["Age"]) { throw "Response has Age header — CDN may be caching" }
    $v1 = ($r1.Content | ConvertFrom-Json).data.gitCommitShort
    $v2 = ($r2.Content | ConvertFrom-Json).data.gitCommitShort
    if ($v1 -ne $v2) { throw "Two requests returned different commit SHAs: '$v1' vs '$v2'" }
}

# HLTH-064: asset manifest hashes match served assets
Assert-Ok "HLTH-064: asset manifest matches served assets" {
    $manifest = (Invoke-RestMethod "$BaseUrl/api/v1/health/assets" -TimeoutSec 15).data
    foreach ($entry in $manifest.assets.PSObject.Properties) {
        $assetPath  = $entry.Name
        $expected   = $entry.Value  # sha256-<base64>
        $assetBytes = (Invoke-WebRequest "$BaseUrl$assetPath" -TimeoutSec 15).Content
        if ($assetBytes -is [string]) {
            $assetBytes = [System.Text.Encoding]::UTF8.GetBytes($assetBytes)
        }
        $sha    = [System.Security.Cryptography.SHA256]::Create()
        $hash   = $sha.ComputeHash($assetBytes)
        $actual = "sha256-" + [Convert]::ToBase64String($hash)
        if ($actual -ne $expected) {
            throw "Hash mismatch for ${assetPath}: expected '$expected', got '$actual'"
        }
    }
}

Write-Host ""
if ($failures.Count -eq 0) {
    Write-Host "All smoke tests passed." -ForegroundColor Green
    if ($versionData) {
        Write-Host "OK: $($versionData.version) @ $($versionData.gitCommitShort)" -ForegroundColor Cyan
    }
    exit 0
} else {
    Write-Host "$($failures.Count) smoke test(s) failed:" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}
