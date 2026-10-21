$ErrorActionPreference = 'Stop'
$conexion = $null

function Consultar-Valor([string]$sql) {
    $comando = $conexion.CreateCommand()
    try {
        $comando.CommandText = $sql
        return $comando.ExecuteScalar()
    }
    finally { $comando.Dispose() }
}

try {
    Write-Host 'Actualizador de antecedentes - Clinica Dental Panchimalco'
    Write-Host 'Use el mismo usuario de Windows que abre la aplicacion.'
    if ((Read-Host 'Cierre la aplicacion. Escriba ACTUALIZAR para continuar') -cne 'ACTUALIZAR') {
        Write-Host 'Cancelado. No se realizaron cambios.'
        exit 0
    }

    $rutaScript = Join-Path $PSScriptRoot '001_AgregarAntecedentesPaciente.sql'
    $script = [System.IO.File]::ReadAllText($rutaScript, [System.Text.Encoding]::UTF8)
    Add-Type -AssemblyName System.Data
    $conexion = New-Object System.Data.SqlClient.SqlConnection
    $conexion.ConnectionString = 'Server=(localdb)\MSSQLLocalDB;Database=ClinicaDentalMario;Integrated Security=true;TrustServerCertificate=True;Encrypt=False;Connect Timeout=30;'
    $conexion.Open()

    if ((Consultar-Valor "SELECT COUNT(*) FROM sys.tables WHERE object_id = OBJECT_ID(N'Pacientes.AntecedentesPaciente', N'U')") -gt 0) {
        Write-Host 'La tabla ya existe. No se realizaron cambios. Si el error continua, revise errors.log.'
        exit 0
    }
    if ((Consultar-Valor "SELECT COUNT(*) FROM sys.tables WHERE object_id = OBJECT_ID(N'Pacientes.Pacientes', N'U')") -ne 1) {
        throw 'No se encontro la tabla de pacientes. No se modificara esta base.'
    }

    $carpetaRespaldo = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'CDMario\Respaldos'
    [System.IO.Directory]::CreateDirectory($carpetaRespaldo) | Out-Null
    $rutaRespaldo = Join-Path $carpetaRespaldo ('ClinicaDentalMario_antes_antecedentes_' + [Guid]::NewGuid().ToString('N') + '.bak')
    Write-Host 'Creando respaldo antes de actualizar...'
    $respaldo = $conexion.CreateCommand()
    try {
        $respaldo.CommandTimeout = 600
        $respaldo.CommandText = 'BACKUP DATABASE [ClinicaDentalMario] TO DISK = @ruta WITH COPY_ONLY, CHECKSUM;'
        $respaldo.Parameters.Add('@ruta', [System.Data.SqlDbType]::NVarChar, 4000).Value = $rutaRespaldo
        [void]$respaldo.ExecuteNonQuery()
        $respaldo.CommandText = 'RESTORE VERIFYONLY FROM DISK = @ruta WITH CHECKSUM;'
        [void]$respaldo.ExecuteNonQuery()
    }
    finally { $respaldo.Dispose() }
    Write-Host "Respaldo verificado: $rutaRespaldo"

    foreach ($lote in [regex]::Split($script, '(?im)^\s*GO\s*$')) {
        if ([string]::IsNullOrWhiteSpace($lote)) { continue }
        $comando = $conexion.CreateCommand()
        try {
            $comando.CommandTimeout = 120
            $comando.CommandText = $lote
            [void]$comando.ExecuteNonQuery()
        }
        finally { $comando.Dispose() }
    }
    if ((Consultar-Valor "SELECT COUNT(*) FROM sys.tables WHERE object_id = OBJECT_ID(N'Pacientes.AntecedentesPaciente', N'U')") -ne 1) {
        throw 'No se pudo confirmar la creacion de la tabla.'
    }
    Write-Host 'Actualizacion completada. Puede abrir la aplicacion.'
    Write-Host 'Se conservaron los registros existentes. Guarde el respaldo indicado arriba.'
}
catch {
    Write-Host ('ERROR: ' + $_.Exception.Message) -ForegroundColor Red
    Write-Host 'Si fallo el respaldo, la actualizacion no se ejecuto. Conserve este mensaje para soporte.'
    exit 1
}
finally {
    if ($null -ne $conexion) { $conexion.Dispose() }
}
