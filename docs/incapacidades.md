# Incapacidades y reposos odontológicos

## Rama y alcance

La rama consolidada para continuar el trabajo es **`integration/clinica-completa`**. Reúne `main`, `dashboard`, `odontograma-pro`, el historial de `reportes`, `chore/vscode-dev-setup` y las incapacidades de `feature/incapacidades-csharp`. La rama original de incapacidades partió de `dashboard`; la rama consolidada ya incorpora también el odontograma mejorado. Véase [integracion.md](integracion.md) para procedencia, comprobaciones y forma de actualizar la copia local.

La emisión pertenece a **Documentos clínicos**, separado de los reportes administrativos. La misma ventana permite emitir desde el menú, desde el expediente y desde una consulta. El PDF es una constancia de la clínica con espacio para firma y sello del odontólogo; no reproduce logotipos, códigos de verificación ni firmas de documentos de otras instituciones. El archivo de referencia aportado no se incluye en el repositorio ni en las pruebas.

## Uso

Antes de emitir, abrir **Configuración → Datos de la clínica**, completar nombre, dirección y teléfono, y pulsar **Guardar datos de la clínica**. También se puede usar el mismo botón dentro del formulario de incapacidad, sin seleccionar un paciente ni emitir. Las ventanas nuevas cargan automáticamente el perfil guardado, incluso después de reiniciar la aplicación. **Recargar guardados** recupera el perfil si se cambió desde otra ventana; reemplaza los valores que se están editando en ese formulario. Los datos corresponden al usuario de Windows y computadora actuales.

1. Abrir **Documentos clínicos → Nueva incapacidad / reposo**. En **Buscar paciente por nombre** se puede escribir el inicio del nombre para autocompletar, sin distinguir mayúsculas, y seleccionar un paciente registrado. Para una persona no registrada, pulsar **Sin expediente** e ingresar sus datos para este documento. Esto no crea un paciente en SQL.
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
- Los datos de la clínica se recuerdan en `clinica.json`, compatible con los perfiles guardados por versiones anteriores. Se pueden guardar de forma independiente desde Configuración o el emisor. También se mantiene el guardado después de una emisión exitosa; una vista previa no guarda el perfil por sí sola. Un perfil vacío o con longitudes inválidas no reemplaza el anterior. Cambiar el perfil afecta a emisiones nuevas; las anteriores conservan su instantánea. Los borradores se crean en la carpeta temporal del usuario de Windows y no figuran como emitidos.
- El historial corresponde al perfil de Windows y computadora donde se emite; no se sincroniza por SQL con otros equipos o perfiles. Incluir **toda** la carpeta `DocumentosClinicos` en los respaldos, junto con la base de datos. **Abrir carpeta** muestra su ubicación efectiva, incluyendo posibles redirecciones de Documentos/OneDrive. Para restaurarlo se copia la carpeta completa al perfil que usa la aplicación.
- Las emisiones no se editan ni se reemplazan desde la interfaz. Una corrección se emite como otro documento con nuevo folio.

## Verificación

`dotnet build "C#/MAIN/ClinicaDentalMario/ClinicaDentalMario.csproj" -c Release`

`dotnet run --project tests/DocumentosClinicos.Checks -c Release -- artifacts/documentos-demo`

Las comprobaciones no requieren conexión SQL y cubren fechas, menores sin DUI, asociación a consultas, reinicio del archivo, instantáneas, recuperación de PDF, rechazo de sobrescritura, fallo parcial de emisión, registros dañados, configuración y permisos. El argumento opcional genera dos PDFs de demostración con datos ficticios para revisar el formato normal y el texto extenso. El workflow de PR ejecuta compilación WPF y comprobaciones en Windows.

Prueba manual en Windows antes de instalar: emitir sin consulta; desde una consulta previa; desde una consulta recién guardada; cancelar el emisor comprobando que la consulta permanece guardada; alternar pacientes y confirmar que no se conserva diagnóstico de otro paciente; revisar fechas escritas y seleccionadas; reiniciar y reimprimir; cambiar datos del expediente y confirmar que el PDF archivado permanece igual; respaldar/restaurar la carpeta; verificar impresión y permisos con recepción.

Resultado local: 31 comprobaciones aprobadas; PDF normal y de texto extenso generados con QuestPDF y revisados visualmente. Las fuentes C# se comprobaron con Roslyn y referencias oficiales de .NET 8/WPF, usando declaraciones auxiliares para los miembros generados de XAML. Se verificó que los archivos XAML son XML válido. Esta comprobación no sustituye la compilación completa de XAML ni la prueba interactiva en Windows. MSBuild no pudo ejecutarse en el entorno actual por un problema al consultar los procesos del entorno Linux. La compilación WPF completa y las comprobaciones se ejecutaron satisfactoriamente en Windows mediante [GitHub Actions](https://github.com/Gust4av033/ClinicaDentalMario/actions/runs/38669247124), sobre la revisión de código `3f66daf`. Sigue pendiente la prueba interactiva e impresión en la laptop de destino.

## Diseño de formulario (actualización)

La plantilla usa recuadros de líneas negras, títulos grises y bloques de período, datos de incapacidad, diagnóstico, datos del paciente, patrono/destinatario, observaciones, fechas y firma. El total de días aparece en números y letras. Se conserva el folio original de cada emisión. El formato normal de demostración ocupa una página Carta y el texto extenso continúa en páginas adicionales.

Se agregaron campos opcionales del documento para tipo de incapacidad, riesgo, afiliación, datos de contacto/dirección y fechas complementarias. Sexo, dirección, teléfono y documento se precargan desde el paciente seleccionado; los datos editados aquí no actualizan el expediente ni SQL. Los campos nuevos se guardan exclusivamente en la instantánea JSON, y los registros anteriores siguen siendo válidos aunque no los incluyan. Están agrupados en **Datos complementarios del formato (opcionales)**.

## Referencia interna y registro profesional

La propiedad histórica `Folio` combina la fecha de creación con el identificador UUID de la emisión. Solo identifica el archivo interno de CDMario; no se presenta como número de certificado oficial ni deriva del ISSS. En el PDF se rotula **Referencia interna** y el título es **Certificado de incapacidad temporal**, acompañado de **Documento emitido por la clínica tratante**, para firma y sello del odontólogo. El título identifica el propósito del documento; no acredita su homologación por el ISSS. La aplicación no emite certificados oficiales del ISSS ni valida automáticamente el reconocimiento o efectos laborales del documento.

El JVPO se toma de la ficha del odontólogo seleccionado y es de solo lectura durante la emisión. Si la ficha no lo contiene, la emisión se bloquea hasta registrar el número real en Configuración. La clínica debe comprobar su correspondencia con el profesional; el sistema no tiene integración con el registro del CSSP. No se cambian los identificadores ni los archivos de emisiones anteriores.
