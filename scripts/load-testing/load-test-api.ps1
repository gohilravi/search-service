# PowerShell script for load testing the search API
# Tests search endpoints with various queries and user types

param(
    [string]$ApiBaseUrl = "http://localhost:5000",
    [int]$ConcurrentRequests = 10,
    [int]$TotalRequests = 1000,
    [int]$DelayMs = 100
)

$ErrorActionPreference = "Continue"

$TestQueries = @(
    "Toyota Camry",
    "Honda Civic",
    "Ford F-150",
    "BMW",
    "2020",
    "New York",
    "Los Angeles",
    "SUV",
    "truck",
    "vehicle"
)

$UserTypes = @("Seller", "Buyer", "Carrier", "Agent")
$AccountIds = @()
1..100 | ForEach-Object { $AccountIds += "account-$_" }

function Invoke-SearchRequest {
    param(
        [string]$Query,
        [string]$UserType,
        [string]$AccountId
    )
    
    $body = @{
        query = $Query
        userType = $UserType
        accountId = $AccountId
        userId = "user-$(Get-Random -Minimum 1 -Maximum 1000)"
        page = 1
        pageSize = 20
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/search" `
            -Method Post `
            -ContentType "application/json" `
            -Body $body `
            -ErrorAction Stop
        
        return @{
            Success = $true
            TotalCount = $response.totalCount
            ItemsCount = $response.items.Count
            ResponseTime = $response.Headers.'X-Response-Time'
        }
    }
    catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

function Invoke-AutocompleteRequest {
    param(
        [string]$Query,
        [string]$UserType,
        [string]$AccountId
    )
    
    try {
        $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/search/autocomplete?query=$Query&userType=$UserType&accountId=$AccountId&limit=10" `
            -Method Get `
            -ErrorAction Stop
        
        return @{
            Success = $true
            SuggestionsCount = $response.suggestions.Count
        }
    }
    catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

function Test-HealthEndpoint {
    try {
        $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/search/health" `
            -Method Get `
            -ErrorAction Stop
        
        return $response.status -eq "healthy"
    }
    catch {
        return $false
    }
}

Write-Host "Starting load test..." -ForegroundColor Green
Write-Host "API Base URL: $ApiBaseUrl" -ForegroundColor Yellow
Write-Host "Concurrent Requests: $ConcurrentRequests" -ForegroundColor Yellow
Write-Host "Total Requests: $TotalRequests" -ForegroundColor Yellow

# Test health endpoint first
Write-Host "`nTesting health endpoint..." -ForegroundColor Cyan
$healthOk = Test-HealthEndpoint
if (-not $healthOk) {
    Write-Host "Health check failed! API may not be running." -ForegroundColor Red
    exit 1
}
Write-Host "Health check passed!" -ForegroundColor Green

# Statistics
$stats = @{
    TotalRequests = 0
    SuccessfulRequests = 0
    FailedRequests = 0
    TotalResponseTime = 0
    MinResponseTime = [double]::MaxValue
    MaxResponseTime = 0
    ResponseTimes = @()
}

# Run load test
Write-Host "`nRunning load test..." -ForegroundColor Cyan
$jobs = @()

for ($i = 1; $i -le $TotalRequests; $i++) {
    $query = $TestQueries | Get-Random
    $userType = $UserTypes | Get-Random
    $accountId = $AccountIds | Get-Random
    
    $job = Start-Job -ScriptBlock {
        param($ApiBaseUrl, $Query, $UserType, $AccountId)
        
        $body = @{
            query = $Query
            userType = $UserType
            accountId = $AccountId
            userId = "user-$(Get-Random -Minimum 1 -Maximum 1000)"
            page = 1
            pageSize = 20
        } | ConvertTo-Json
        
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/search" `
                -Method Post `
                -ContentType "application/json" `
                -Body $body `
                -ErrorAction Stop
            
            $stopwatch.Stop()
            
            return @{
                Success = $true
                ResponseTime = $stopwatch.ElapsedMilliseconds
                TotalCount = $response.totalCount
            }
        }
        catch {
            $stopwatch.Stop()
            return @{
                Success = $false
                ResponseTime = $stopwatch.ElapsedMilliseconds
                Error = $_.Exception.Message
            }
        }
    } -ArgumentList $ApiBaseUrl, $query, $userType, $accountId
    
    $jobs += $job
    
    # Wait for jobs to complete if we've reached concurrent limit
    while ($jobs.Count -ge $ConcurrentRequests) {
        $completed = $jobs | Where-Object { $_.State -eq "Completed" }
        foreach ($job in $completed) {
            $result = Receive-Job -Job $job
            Remove-Job -Job $job
            
            $stats.TotalRequests++
            if ($result.Success) {
                $stats.SuccessfulRequests++
            }
            else {
                $stats.FailedRequests++
            }
            
            $responseTime = $result.ResponseTime
            $stats.TotalResponseTime += $responseTime
            $stats.ResponseTimes += $responseTime
            
            if ($responseTime -lt $stats.MinResponseTime) {
                $stats.MinResponseTime = $responseTime
            }
            if ($responseTime -gt $stats.MaxResponseTime) {
                $stats.MaxResponseTime = $responseTime
            }
        }
        
        $jobs = $jobs | Where-Object { $_.State -ne "Completed" }
        Start-Sleep -Milliseconds $DelayMs
    }
    
    if ($i % 100 -eq 0) {
        Write-Host "Processed $i requests... (Success: $($stats.SuccessfulRequests), Failed: $($stats.FailedRequests))" -ForegroundColor Gray
    }
}

# Wait for remaining jobs
Write-Host "Waiting for remaining jobs to complete..." -ForegroundColor Cyan
while ($jobs.Count -gt 0) {
    $completed = $jobs | Where-Object { $_.State -eq "Completed" }
    foreach ($job in $completed) {
        $result = Receive-Job -Job $job
        Remove-Job -Job $job
        
        $stats.TotalRequests++
        if ($result.Success) {
            $stats.SuccessfulRequests++
        }
        else {
            $stats.FailedRequests++
        }
        
        $responseTime = $result.ResponseTime
        $stats.TotalResponseTime += $responseTime
        $stats.ResponseTimes += $responseTime
        
        if ($responseTime -lt $stats.MinResponseTime) {
            $stats.MinResponseTime = $responseTime
        }
        if ($responseTime -gt $stats.MaxResponseTime) {
            $stats.MaxResponseTime = $responseTime
        }
    }
    
    $jobs = $jobs | Where-Object { $_.State -ne "Completed" }
    Start-Sleep -Milliseconds 100
}

# Calculate statistics
$avgResponseTime = if ($stats.TotalRequests -gt 0) { $stats.TotalResponseTime / $stats.TotalRequests } else { 0 }
$sortedTimes = $stats.ResponseTimes | Sort-Object
$medianResponseTime = if ($sortedTimes.Count -gt 0) {
    $mid = [math]::Floor($sortedTimes.Count / 2)
    if ($sortedTimes.Count % 2 -eq 0) {
        ($sortedTimes[$mid - 1] + $sortedTimes[$mid]) / 2
    }
    else {
        $sortedTimes[$mid]
    }
} else { 0 }

$p95Index = [math]::Floor($sortedTimes.Count * 0.95)
$p95ResponseTime = if ($p95Index -lt $sortedTimes.Count) { $sortedTimes[$p95Index] } else { 0 }

$p99Index = [math]::Floor($sortedTimes.Count * 0.99)
$p99ResponseTime = if ($p99Index -lt $sortedTimes.Count) { $sortedTimes[$p99Index] } else { 0 }

# Print results
Write-Host "`n=== Load Test Results ===" -ForegroundColor Green
Write-Host "Total Requests: $($stats.TotalRequests)" -ForegroundColor White
Write-Host "Successful: $($stats.SuccessfulRequests) ($([math]::Round(($stats.SuccessfulRequests / $stats.TotalRequests) * 100, 2))%)" -ForegroundColor Green
Write-Host "Failed: $($stats.FailedRequests) ($([math]::Round(($stats.FailedRequests / $stats.TotalRequests) * 100, 2))%)" -ForegroundColor Red
Write-Host "`nResponse Time Statistics (ms):" -ForegroundColor Yellow
Write-Host "  Average: $([math]::Round($avgResponseTime, 2))" -ForegroundColor White
Write-Host "  Median: $([math]::Round($medianResponseTime, 2))" -ForegroundColor White
Write-Host "  Min: $([math]::Round($stats.MinResponseTime, 2))" -ForegroundColor White
Write-Host "  Max: $([math]::Round($stats.MaxResponseTime, 2))" -ForegroundColor White
Write-Host "  P95: $([math]::Round($p95ResponseTime, 2))" -ForegroundColor White
Write-Host "  P99: $([math]::Round($p99ResponseTime, 2))" -ForegroundColor White

Write-Host "`nLoad test completed!" -ForegroundColor Green

