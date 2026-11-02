Add-Type -AssemblyName 'System.Data'

$c = New-Object System.Data.SqlClient.SqlConnection('Server=.;Database=master;Integrated Security=True;TrustServerCertificate=True;')
$c.Open()
$cmd = $c.CreateCommand()
$cmd.CommandText = "SELECT name FROM sys.databases WHERE name = 'FuelTrack'"
$r = $cmd.ExecuteScalar()
Write-Host "Database FuelTrack exists in SQL Server: $([bool]$r)"
if (-not $r) {
    Write-Host "Creating FuelTrack database..."
    $cmd2 = $c.CreateCommand()
    $cmd2.CommandText = "CREATE DATABASE FuelTrack"
    $cmd2.ExecuteNonQuery()
    Write-Host "FuelTrack database created successfully."
}
$c.Close()
