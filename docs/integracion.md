# Rama consolidada de Clínica Dental Mario

Continuar el desarrollo y las pruebas de conjunto en **`integration/clinica-completa`**. `main` permanece sin modificar hasta revisar el PR de integración.

## Avances incorporados

| Origen | Commit de origen | Contenido |
| --- | --- | --- |
| `main` | `c1fbf59` | Base común, pacientes, agenda, tratamientos, pagos, ajustes y bitácora |
| `dashboard` | `1400415` | Dashboard con métricas reales y navegación |
| `odontograma-pro` | `5c51218` | Herramientas, representación y persistencia mejoradas del odontograma |
| `reportes` | `28c9e0e` | Historial reconciliado; su contenido funcional ya coincidía con lo incorporado en la base |
| `chore/vscode-dev-setup` | `f23657d` | Tareas, depuración y extensiones recomendadas para VS Code |
| `feature/incapacidades-csharp` | `6025cd2` | Documentos clínicos, emisión independiente y desde consultas, PDF e historial local |

Se conservó el historial mediante merges, sin reemplazar módulos por versiones antiguas. Los merges de contenido no generaron conflictos. Las ramas de origen se conservan.

## Base instalada de la clínica

La integración no cambia ningún archivo SQL ni el inicializador. No se ejecutó ninguna conexión a la base de la clínica ni migraciones. El odontograma integrado conserva sus mejoras previas en C#: usa el esquema existente para guardar y leer evoluciones y no añade tablas ni columnas. Las nuevas incapacidades guardan su PDF e instantánea en `Documentos\CDMario\DocumentosClinicos`, una carpeta que debe incluirse en los respaldos del perfil Windows que utiliza la aplicación. Los detalles están en [incapacidades.md](incapacidades.md).

## Actualizar una copia local

Guardar o confirmar primero cualquier cambio local propio. Después:

```powershell
git fetch origin
git switch --track origin/integration/clinica-completa
```

Si la rama ya existe localmente:

```powershell
git switch integration/clinica-completa
git pull --ff-only
```

No es necesario fusionar las ramas de cada módulo manualmente: sus avances ya están reunidos aquí.

## Comprobaciones

- Confirmar que las puntas indicadas en la tabla son antecesoras de la rama integrada.
- Confirmar ausencia de cambios en scripts SQL y en `DatabaseInitializer.cs` respecto a `main`.
- Ejecutar las 31 comprobaciones de Documentos clínicos sin conexión SQL.
- Compilar WPF en Windows mediante el workflow del PR.
- Probar en Windows dashboard y sus accesos, apertura/edición/carga del odontograma, reportes, nueva consulta, emisión de incapacidad y reimpresión.

En este entorno Linux se dispone además de comprobación de tipos de las fuentes C# con Roslyn y referencias oficiales de .NET 8/WPF; las declaraciones auxiliares de miembros XAML no reemplazan la compilación de XAML ni la prueba visual de la aplicación en Windows. La preparación de esta rama no instala cambios en la laptop de la clínica.
