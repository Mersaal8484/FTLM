Add-Type -AssemblyName 'System.Data'

Write-Host "=== Checking SQL Server Connection ===" -ForegroundColor Cyan

# Test which connection string works
$connections = @(
    @{ name="dot (named pipe)"; cs="Server=.;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;" },
    @{ name="localhost,8749 TCP"; cs="Server=localhost,8749;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;" }
)

$workingCs = $null
foreach ($item in $connections) {
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection($item.cs + "Database=master;")
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT @@SERVERNAME"
        $serverName = $cmd.ExecuteScalar()
        Write-Host "  [OK] $($item.name) -> Server: $serverName" -ForegroundColor Green
        $workingCs = $item.cs
        $conn.Close()
    } catch {
        Write-Host "  [FAIL] $($item.name) -> $($_.Exception.Message)" -ForegroundColor Red
    }
}

if (-not $workingCs) {
    Write-Host "ERROR: No working connection found!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Checking FuelTrack Database ===" -ForegroundColor Cyan

$conn2 = New-Object System.Data.SqlClient.SqlConnection($workingCs + "Database=master;")
$conn2.Open()
$cmd2 = $conn2.CreateCommand()
$cmd2.CommandText = "SELECT name FROM sys.databases WHERE name = 'FuelTrack'"
$dbExists = $cmd2.ExecuteScalar()

if ($dbExists) {
    Write-Host "  [OK] FuelTrack database EXISTS" -ForegroundColor Green
} else {
    Write-Host "  [MISSING] FuelTrack database not found" -ForegroundColor Yellow
    Write-Host "  Creating FuelTrack database..." -ForegroundColor Yellow
    $cmd3 = $conn2.CreateCommand()
    $cmd3.CommandText = "CREATE DATABASE [FuelTrack]"
    $cmd3.ExecuteNonQuery() | Out-Null
    Write-Host "  [OK] FuelTrack database created!" -ForegroundColor Green
}

$conn2.Close()

Write-Host ""
Write-Host "=== Testing FuelTrack Database Connection ===" -ForegroundColor Cyan

try {
    $conn3 = New-Object System.Data.SqlClient.SqlConnection($workingCs + "Database=FuelTrack;")
    $conn3.Open()
    Write-Host "  [OK] Connected to FuelTrack database!" -ForegroundColor Green
    $conn3.Close()
} catch {
    Write-Host "  [FAIL] Cannot connect to FuelTrack: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Best connection string for appsettings ===" -ForegroundColor Cyan
Write-Host "  $($workingCs)Database=FuelTrack;MultipleActiveResultSets=true;" -ForegroundColor White
