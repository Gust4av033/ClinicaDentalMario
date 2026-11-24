# Falta Pacientes.AntecedentesPaciente al iniciar

Si `errors.log` indica `Faltan: Pacientes.AntecedentesPaciente`, la aplicación logró conectar a SQL pero la base existente todavía no tiene la ficha general de antecedentes requerida por esta versión. El inicializador conserva las bases existentes y no aplica automáticamente los cambios de una instalación nueva.

La solución para este error es ejecutar [001_AgregarAntecedentesPaciente.sql](../SQL/Migraciones/001_AgregarAntecedentesPaciente.sql) en la instancia que usa la aplicación. No requiere compilar otro ejecutable. El script agrega la tabla vacía con la misma estructura que usa el inicializador; no modifica pacientes, consultas ni antecedentes guardados en el historial. Las fichas generales se completan desde el expediente cuando corresponda; no se infiere información clínica automáticamente.

## Actualizador para la laptop del cliente

Sin instalar herramientas de desarrollo, copiar juntos `ActualizarAntecedentes.cmd`, `ActualizarAntecedentes.ps1`, `001_AgregarAntecedentesPaciente.sql` y `LEEME-CLIENTE.txt`. Cerrar la aplicación, abrir el `.cmd` con el mismo usuario de Windows que la utiliza y escribir `ACTUALIZAR`. El actualizador conecta a la instancia LocalDB configurada, comprueba la base y crea/verifica primero un respaldo `COPY_ONLY` con `CHECKSUM` en `%LOCALAPPDATA%\CDMario\Respaldos`. Si falla el respaldo no ejecuta la migración. Después ejecuta únicamente el script puntual y confirma que la tabla existe. No requiere otra compilación de la aplicación.

El lanzador selecciona `ExecutionPolicy Bypass` solo para su proceso; no modifica la política permanente del equipo. Si una política de la organización bloquea el actualizador, se puede usar el procedimiento de SSMS con el responsable del equipo. El paquete no se ha ejecutado contra la base del cliente desde este entorno; su ejecución debe completarse allí.

## Procedimiento con SSMS

1. Cerrar la aplicación y realizar un respaldo de la base existente.
2. En SQL Server Management Studio, conectarse a `(localdb)\MSSQLLocalDB` con autenticación de Windows usando el mismo usuario de Windows que abre la aplicación.
3. Abrir `SQL/Migraciones/001_AgregarAntecedentesPaciente.sql` y ejecutarlo completo con F5. El script selecciona `ClinicaDentalMario`, comprueba que existe la tabla de pacientes y crea únicamente `Pacientes.AntecedentesPaciente` si falta. Si ya existe, la conserva. Un fallo revierte la transacción.
4. Tras el mensaje de creación correcta, volver a abrir la aplicación y comprobar el acceso al expediente.

Si ya se dispone de `sqlcmd`, desde la raíz del repositorio también se puede ejecutar:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -b -i ".\SQL\Migraciones\001_AgregarAntecedentesPaciente.sql"
```

Para este caso no ejecutar `Script00.sql`: es el instalador de una base nueva y contiene instrucciones de eliminación/recreación. Un `git pull` actualiza archivos del proyecto, pero no actualiza la estructura de una base existente. Si aparece otro error, consultar el bloque más reciente de `%LOCALAPPDATA%\CDMario\Logs\errors.log` antes de aplicar otro cambio.
