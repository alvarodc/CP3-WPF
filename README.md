# CardPass3 WPF — v4.0

Migración de CardPass3 a .NET 8 / WPF con arquitectura MVVM limpia.

## Stack

| Capa | Tecnología |
|---|---|
| UI | WPF .NET 8, MVVM via CommunityToolkit.Mvvm |
| DI | Microsoft.Extensions.DependencyInjection + Hosting |
| Data | Dapper + MySqlConnector |
| Logging | Serilog → ficheros rotativos |
| Hardware | IReaderDriver (abstracción sobre librería TCP/IP existente) |

## Estructura del proyecto

```
src/
└── CardPass3.WPF/
    ├── Data/
    │   ├── Models/          # POCOs mapeados desde la BBDD
    │   └── Repositories/    # Dapper repositories
    ├── Modules/
    │   ├── Login/           # Splash + Login
    │   ├── Shell/           # Ventana principal + navegación
    │   ├── Readers/         # Gestión de lectores
    │   ├── Events/          # Visor de fichajes (paginación keyset)
    │   └── Users/           # Gestión de usuarios
    ├── Services/
    │   ├── Database/        # MySqlConnectionFactory
    │   ├── Navigation/      # NavigationService
    │   └── Readers/         # ReaderConnectionService + IReaderDriver
    └── Shared/
        └── Styles/          # Colors.xaml, Controls.xaml, Typography.xaml
```

## Arquitectura de readers

Los lectores enabled se conectan en paralelo al arrancar (máx. 10 simultáneos via `SemaphoreSlim`).
La Shell ya es usable mientras esto ocurre. El estado de cada lector es observable en tiempo real.

```
App start → Login OK → Shell visible → ReaderConnectionService.StartAsync() [background]
                                           ↓ SemaphoreSlim(10)
                                       [Lector 1] → Connected ✓
                                       [Lector 2] → Failed ✗
                                       [Lector N] → Connecting...
```

## Paginación de eventos

La tabla `events` puede tener cientos de miles de registros. Se usa **keyset pagination**
en lugar de OFFSET para rendimiento constante independientemente de la página:

```sql
-- En lugar de: ... LIMIT 50 OFFSET 50000  ← escanea 50000 filas
-- Usamos:
WHERE id_event > :lastIdEvent ORDER BY id_event ASC LIMIT 50
```

## Iteraciones

| Iter | Contenido | Estado |
|---|---|---|
| 1 | Core infra + Login + ReaderConnectionService | 🔨 En progreso |
| 2 | Módulo Events con paginación keyset | ⏳ Pendiente |
| 3 | Módulo Users + atributos + documentos | ⏳ Pendiente |
| 4 | Hardware: webcam, scanner, huellas | ⏳ Pendiente |
| 5 | Admin: áreas, operadores, configuración, backup | ⏳ Pendiente |

## Configuración local

Copiar `appsettings.json` y ajustar la cadena de conexión:

```json
{
  "ConnectionStrings": {
    "CardPass3": "Server=localhost;Port=3306;Database=cardpass3;User=root;Password=;"
  }
}
```
