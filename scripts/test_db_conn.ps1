Add-Type -AssemblyName 'System.Data'

$connectionStrings = @(
    'Server=localhost,8749;Database=master;Integrated Security=True;TrustServerCertificate=True;',
    'Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True;',
    'Server=.;Database=master;Integrated Security=True;TrustServerCertificate=True;',
    'Server=127.0.0.1;Database=master;Integrated Security=True;TrustServerCertificate=True;',
    'Server=(localdb)\mssqllocaldb;Database=master;Integrated Security=True;TrustServerCertificate=True;'
)

foreach ($cs in $connectionStrings) {
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection($cs)
        $conn.Open()
        Write-Host "SUCCESS: $cs"
        $conn.Close()
    } catch {
        Write-Host "FAILED: $cs -> $($_.Exception.Message)"
    }
}
