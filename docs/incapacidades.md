# Incapacidades y reposos odontológicos

## Rama y alcance

`feature/incapacidades-csharp` parte de `dashboard` (`1400415`), el avance funcional más reciente del repositorio al revisar las ramas. `main` conserva la integración hasta Ajustes/Bitácora (`c1fbf59`). La rama posterior `chore/vscode-dev-setup` solo añade herramientas de VS Code. `odontograma-pro` mantiene mejoras propias que no están integradas en esta rama. No se mezclan esas ramas como parte de esta función.

La emisión pertenece a **Documentos clínicos**, separado de los reportes administrativos. La misma ventana permite emitir desde el menú, desde el expediente y desde una consulta. El PDF es una constancia de la clínica con espacio para firma y sello del odontólogo; no reproduce logotipos, códigos de verificación ni firmas de documentos de otras instituciones. El archivo de referencia aportado no se incluye en el repositorio ni en las pruebas.

## Uso

1. Abrir **Documentos clínicos → Nueva incapacidad / reposo**. Se puede seleccionar un paciente registrado o pulsar **Sin expediente** e ingresar sus datos para este documento. Esto no crea un paciente en SQL.
2. Opcionalmente elegir una consulta. Se carga su diagnóstico, fecha y odontólogo para revisión. **Sin consulta** permite emitir de forma independiente.
3. Revisar el odontólogo, registro JVPO, diagnóstico, fechas, indicaciones y datos de la clínica. El documento de identidad es opcional para permitir pacientes menores sin DUI.
4. **Vista previa** abre un borrador sin incorporarlo al historial. **Emitir y guardar PDF** conserva una emisión independiente con folio único; el visor PDF permite imprimirla.
5. Desde el expediente, usar **Emitir incapacidad / reposo**, **Documentos del paciente**, o **Emitir** junto a una consulta anterior. Al registrar una nueva consulta, marcar **Abrir incapacidad / reposo al guardar esta consulta** abre el mismo emisor después de guardar. Si también se solicita asignar tratamiento, ese flujo continúa al cerrar la ventana de incapacidad.

Administración y odontólogos pueden emitir. Recepción puede consultar, abrir, imprimir y guardar copias de documentos ya emitidos. La emisión no se realiza automáticamente por tener una consulta: requiere revisar los datos y pulsar **Emitir**.

## Persistencia sin modificaciones en la base

- No se añaden tablas, columnas, procedimientos, migraciones ni instrucciones SQL. No se modifica el inicializador existente. El módulo reutiliza consultas de lectura de los repositorios actuales para cargar pacientes, doctores y consultas. Guardar una nueva consulta sigue el comportamiento previo del sistema.
- Cada emisión guarda una instantánea JSON y su PDF en `Documentos\CDMario\DocumentosClinicos\<identificador>\`. Se genera primero en una carpeta temporal y se incorpora al historial solo cuando ambos archivos están completos.
- Se conserva el paciente, odontólogo, JVPO, diagnóstico, período, clínica, usuario emisor y contexto de consulta tal como estaban al emitir. Las reimpresiones usan esa instantánea y no los datos actuales del expediente. Si falta el PDF, se regenera con los datos archivados.
- Se calcula el total con ambos extremos incluidos: del 13 al 15 son tres días; el mismo día equivale a uno. Se rechazan fechas invertidas y datos obligatorios vacíos.
- El SP de inserción de consultas existente no devuelve su identificador. En la emisión inmediata se conserva paciente, odontólogo y fecha de la consulta guardada, con `IdConsulta` nulo. Al seleccionar una consulta del historial también se conserva su identificador. No se busca “la última consulta” para inferir un ID.
- Los datos de la clínica se recuerdan en `clinica.json`. Los borradores se crean en la carpeta temporal del usuario de Windows y no figuran como emitidos.
- El historial corresponde al perfil de Windows y computadora donde se emite; no se sincroniza por SQL con otros equipos o perfiles. Incluir **toda** la carpeta `DocumentosClinicos` en los respaldos, junto con la base de datos. **Abrir carpeta** muestra su ubicación efectiva, incluyendo posibles redirecciones de Documentos/OneDrive. Para restaurarlo se copia la carpeta completa al perfil que usa la aplicación.
- Las emisiones no se editan ni se reemplazan desde la interfaz. Una corrección se emite como otro documento con nuevo folio.

## Verificación

`dotnet build "C#/MAIN/ClinicaDentalMario/ClinicaDentalMario.csproj" -c Release`

`dotnet run --project tests/DocumentosClinicos.Checks -c Release -- artifacts/documentos-demo`

Las comprobaciones no requieren conexión SQL y cubren fechas, menores sin DUI, asociación a consultas, reinicio del archivo, instantáneas, recuperación de PDF, rechazo de sobrescritura, fallo parcial de emisión, registros dañados, configuración y permisos. El argumento opcional genera dos PDFs de demostración con datos ficticios para revisar el formato normal y el texto extenso. El workflow de PR ejecuta compilación WPF y comprobaciones en Windows.

Prueba manual en Windows antes de instalar: emitir sin consulta; desde una consulta previa; desde una consulta recién guardada; cancelar el emisor comprobando que la consulta permanece guardada; alternar pacientes y confirmar que no se conserva diagnóstico de otro paciente; revisar fechas escritas y seleccionadas; reiniciar y reimprimir; cambiar datos del expediente y confirmar que el PDF archivado permanece igual; respaldar/restaurar la carpeta; verificar impresión y permisos con recepción.
