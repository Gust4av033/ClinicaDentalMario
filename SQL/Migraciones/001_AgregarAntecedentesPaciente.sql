-- Actualización puntual para instalaciones existentes de Clínica Dental Panchimalco.
-- Ejecutar con la aplicación cerrada y un respaldo reciente de ClinicaDentalMario.
-- No elimina ni actualiza datos. Si la tabla ya existe, la conserva.
USE [ClinicaDentalMario];
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'Pacientes.Pacientes', N'U') IS NULL
        THROW 50001, 'No se encontró Pacientes.Pacientes. Revisa la instancia y la base seleccionadas.', 1;

    IF OBJECT_ID(N'Pacientes.AntecedentesPaciente', N'U') IS NULL
    BEGIN
        CREATE TABLE Pacientes.AntecedentesPaciente
        (
            IdPaciente INT NOT NULL PRIMARY KEY,
            TieneAntecedentesMedicos BIT NOT NULL CONSTRAINT DF_AntecedentesPaciente_TieneMedicos DEFAULT 0,
            DetalleAntecedentesMedicos NVARCHAR(MAX) NULL,
            TieneAntecedentesOdontologicos BIT NOT NULL CONSTRAINT DF_AntecedentesPaciente_TieneOdontologicos DEFAULT 0,
            DetalleAntecedentesOdontologicos NVARCHAR(MAX) NULL,
            FechaRegistro DATETIME2 NOT NULL CONSTRAINT DF_AntecedentesPaciente_FechaRegistro DEFAULT SYSDATETIME(),
            FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_AntecedentesPaciente_FechaActualizacion DEFAULT SYSDATETIME(),
            CONSTRAINT FK_AntecedentesPaciente_Paciente
                FOREIGN KEY (IdPaciente)
                REFERENCES Pacientes.Pacientes(IdPaciente)
        );
        PRINT N'Se creó Pacientes.AntecedentesPaciente. Los registros existentes se conservaron.';
    END
    ELSE
        PRINT N'Pacientes.AntecedentesPaciente ya existe. No se realizaron cambios.';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
