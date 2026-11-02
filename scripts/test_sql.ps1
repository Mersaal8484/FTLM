Add-Type -AssemblyName 'System.Data'

$tests = @(
    "Server=localhost,8749;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;",
    "Server=127.0.0.1,8749;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;",
    "Server=np:\\.\pipe\sql\query;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;",
    "Server=.;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;",
    "Server=(local);Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;",
    "Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;"
)

foreach ($cs in $tests) {
    Write-Host "Trying: $cs"
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection($cs)
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT @@SERVERNAME, @@VERSION"
        $reader = $cmd.ExecuteReader()
        if ($reader.Read()) {
            Write-Host "--> SUCCESS! ServerName: $($reader[0])"
        }
        $conn.Close()
    } catch {
        Write-Host "--> ERROR: $($_.Exception.Message)"
    }
}
