# ClinicaDentalMario

Aplicación de escritorio para la gestión de una clínica dental, desarrollada en **C# con WPF sobre .NET 8**.

El proyecto busca centralizar la operación de la clínica en una sola aplicación: pacientes, agenda de citas, historial clínico, tratamientos, odontograma, pagos, archivos, personal, usuarios y reportes.

> Estado actual: proyecto en desarrollo y etapa de estabilización/pulido.

## Tecnologías

- C# / .NET 8
- WPF
- MVVM
- SQL Server / LocalDB
- Dapper
- Microsoft.Data.SqlClient
- MahApps.Metro.IconPacks.Material
- QuestPDF

## Módulos

Actualmente el proyecto contiene, entre otros, los siguientes módulos:

- Inicio de sesión
- Pacientes
- Agenda de citas
- Historial clínico
- Tratamientos
- Odontograma
- Pagos
- Archivos del paciente
- Personal
- Usuarios
- Configuración
- Reportes
- Dashboard

Algunos módulos todavía se encuentran en proceso de validación, endurecimiento de reglas de negocio y mejoras de experiencia de usuario.

## Arquitectura actual

El proyecto utiliza una estructura MVVM sencilla y orientada a mantener el código separado por responsabilidad:

```text
View
  ↓
ViewModel
  ↓
Repository
  ↓
Dapper
  ↓
SQL Server
```

Estructura principal del proyecto:

```text
ClinicaDentalMario/
├── Common/
├── Config/
├── Data/
├── Models/
├── Repositories/
├── ViewModel/
├── Views/
├── Properties/
├── App.xaml
└── ClinicaDentalMario.csproj
```

## Requisitos para desarrollo

- Windows 10/11
- Visual Studio 2022
- .NET 8 SDK
- SQL Server LocalDB o una instancia compatible de SQL Server

## Ejecutar el proyecto

1. Clonar el repositorio.
2. Abrir `C#/MAIN/ClinicaDentalMario/ClinicaDentalMario.sln` en Visual Studio.
3. Restaurar los paquetes NuGet si Visual Studio no lo hace automáticamente.
4. Verificar que la instancia de SQL Server/LocalDB configurada esté disponible.
5. Compilar y ejecutar el proyecto.

La configuración de conexión a la base de datos se encuentra actualmente dentro del proyecto y será revisada durante la etapa de estabilización.

## Base de datos

La aplicación trabaja con SQL Server y utiliza Dapper para acceso a datos. La base de datos está organizada por áreas funcionales como seguridad, pacientes, personal, agenda, odontología, archivos y facturación.

Se utilizan tablas, vistas, funciones, procedimientos almacenados y otras reglas de base de datos para soportar la lógica del sistema.

## Estado del desarrollo

En esta etapa el objetivo principal no es reconstruir la aplicación, sino revisar lo que ya funciona y llevar cada módulo a un estado más sólido mediante:

- validaciones de entrada;
- manejo consistente de errores;
- reglas de negocio;
- mejoras de navegación;
- control de operaciones duplicadas;
- permisos y seguridad;
- limpieza técnica y mantenimiento del código.

## Control de versiones

Los archivos generados por Visual Studio, compilaciones locales y configuraciones específicas de cada equipo no se incluyen en Git.

La rama principal del proyecto es:

```text
main
```

## Autor

Proyecto desarrollado por **Gust4av033**.

---

Este repositorio se encuentra en desarrollo activo. La documentación se irá actualizando junto con la estabilización de los módulos.
