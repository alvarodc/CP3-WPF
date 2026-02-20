#!/usr/bin/env bash
# =============================================================================
# CardPass3 — Crea los issues de Iter-1 (completados) que faltaron
# Uso: bash scripts/create-iter1-issues.sh
# =============================================================================

REPO="alvarodc/CP3-WPF"

ok()  { echo -e "\e[32m✓\e[0m $1"; }
step(){ echo -e "\e[33m→\e[0m $1"; }
err(){ echo -e "\e[31m✗\e[0m $1"; }

create_and_close() {
    local title="$1"
    local body="$2"
    local labels="$3"

    step "$title"

    # Crear sin milestone (evita problema con milestone cerrado)
    local url
    url=$(gh issue create \
        --repo "$REPO" \
        --title "$title" \
        --body "$body" \
        --label "$labels" 2>&1)

    if echo "$url" | grep -q "https://"; then
        # Extraer número del issue de la URL
        local number
        number=$(echo "$url" | grep -oP '(?<=/issues/)\d+')
        # Cerrar inmediatamente
        gh issue close "$number" --repo "$REPO" --comment "Completado en Iter-1." > /dev/null 2>&1
        # Asignar milestone manualmente vía API
        gh api "repos/$REPO/issues/$number" \
            --method PATCH \
            -f milestone="$(get_milestone_number)" > /dev/null 2>&1
        ok "Issue #$number creado y cerrado"
    else
        err "Error creando: $title"
        echo "  $url"
    fi
    sleep 1
}

# Obtener número del milestone Iter-1
get_milestone_number() {
    gh api "repos/$REPO/milestones?state=all" \
        --jq '.[] | select(.title | contains("Iter-1")) | .number' 2>/dev/null | head -1
}

echo "=== Creando issues Iter-1 completados ==="
echo "Milestone Iter-1: $(get_milestone_number)"
echo ""

# ──────────────────────────────────────────────────────────────────────────────

create_and_close \
"[Iter-1] Estructura solución y proyecto WPF .NET 8" \
"## Qué se hizo
Creación del proyecto CardPass3.WPF con .NET 8, estructura de carpetas por módulos y configuración del .csproj.

## Checklist
- [x] CardPass3.sln y CardPass3.WPF.csproj
- [x] Estructura Modules / Services / Data / Shared / Core
- [x] Dependencias: CommunityToolkit.Mvvm, Dapper, MySqlConnector, Serilog, M.E.Hosting
- [x] .gitignore, README, issue templates" \
"módulo: core,tipo: feature,prioridad: crítica,complejidad: M,done"

create_and_close \
"[Iter-1] Sistema DI con Microsoft.Extensions.Hosting" \
"## Qué se hizo
Host genérico con DI, Serilog y ciclo de vida de la aplicación.

## Checklist
- [x] App.xaml.cs con IHost y OnStartup/OnExit
- [x] ConfigureServices con todos los registros
- [x] Graceful shutdown: desconexión de lectores al cerrar" \
"módulo: core,tipo: feature,prioridad: crítica,complejidad: S,done"

create_and_close \
"[Iter-1] Modelos de datos — un fichero por modelo" \
"## Qué se hizo
Modelos C# mapeados a las tablas de CardPass3. Un fichero por clase en Data/Models/.

## Checklist
- [x] Reader, Operator, DbEvent, EventRow
- [x] User, Area, ConfigurationEntry
- [x] Document, Fingerprint, UserGroup
- [x] TimeAttendance + TimeAttendanceSlot
- [x] UserAttribute + UserAttributeGroup + UserAttributeValues
- [x] License" \
"módulo: core,tipo: feature,prioridad: crítica,complejidad: M,done"

create_and_close \
"[Iter-1] Repositorios Dapper con interfaces" \
"## Qué se hizo
Capa de acceso a datos con Dapper, interfaces inyectables y queries parametrizadas.

## Checklist
- [x] IReaderRepository / ReaderRepository
- [x] IOperatorRepository / OperatorRepository
- [x] IEventRepository / EventRepository
- [x] IConfigurationRepository / ConfigurationRepository
- [x] Paginación keyset en GetPageAsync (no OFFSET)" \
"módulo: core,tipo: feature,prioridad: crítica,complejidad: L,done"

create_and_close \
"[Iter-1] Configuración de BD en ProgramData con cifrado AES-256-GCM" \
"## Qué se hizo
Fichero JSON externo en %ProgramData%\CardPass3\Database\cp3db.config.json con contraseña cifrada.
Reemplaza el antiguo sistema basado en app.config y RijndaelManaged.

## Checklist
- [x] DatabaseConfig — modelo serializable
- [x] DatabaseEncryption — AES-256-GCM (reemplaza RijndaelManaged obsoleto)
- [x] DatabaseConfigService — carga lazy, caché, ResetToDefault()
- [x] Migración graceful desde formato de cifrado anterior
- [x] Valores por defecto: localhost / cardpass3 / cardpass3 / 3306" \
"módulo: core,tipo: feature,prioridad: crítica,complejidad: L,done"

create_and_close \
"[Iter-1] LoginWindow — diseño y autenticación SHA-256" \
"## Qué se hizo
Ventana de login borderless con verificación SHA-256 y gestión de errores de BD.

## Checklist
- [x] Diseño sin bordes, sombra, barra de acento azul superior
- [x] Logo shield en XAML puro, nombre del producto, versión
- [x] Verificación SHA-256 hex con espacios (compatible con BD original)
- [x] Grid de formulario con fila fija para error (el botón no se desplaza)
- [x] Diálogo DbConnectionErrorDialog con 3 opciones: configurar / resetear / volver
- [x] Patrón evento LoginSucceeded / DbConfigRequired (sin referencias VM→View)" \
"módulo: login,tipo: feature,prioridad: crítica,complejidad: M,done"

create_and_close \
"[Iter-1] ShellWindow — sidebar + topbar + navegación por módulos" \
"## Qué se hizo
Ventana principal con sidebar de navegación agrupado por secciones y topbar con reloj.

## Checklist
- [x] Sidebar oscuro (220px) con iconos Segoe MDL2 y secciones agrupadas
- [x] Widget de estado de lectores en footer del sidebar
- [x] Topbar con título del módulo activo, nombre del operador y reloj en tiempo real
- [x] ContentControl como host de vistas
- [x] Logout via evento LogoutRequested (sin dependencia VM→View)" \
"módulo: shell,tipo: feature,prioridad: crítica,complejidad: M,done"

create_and_close \
"[Iter-1] ReadersView — grid con badges de estado en tiempo real" \
"## Qué se hizo
Vista del módulo de lectores con DataGrid virtualizado y badges de color por estado.

## Checklist
- [x] Columnas: Estado (badge), Descripción, IP (Consolas), Puerto, Área, Último intento, Error
- [x] Badges semánticos: verde/naranja/rojo/gris según ReaderConnectionState
- [x] Toolbar: Conectar, Desconectar, Abrir relé
- [x] Virtualización activada (VirtualizationMode=Recycling)
- [x] Contadores conectados/total en header" \
"módulo: readers,tipo: feature,prioridad: crítica,complejidad: M,done"

create_and_close \
"[Iter-1] ReaderConnectionService — conexión paralela asíncrona" \
"## Qué se hizo
Servicio singleton que conecta todos los lectores habilitados en paralelo al arranque.

## Checklist
- [x] Conexión paralela con SemaphoreSlim (máx. 10 simultáneas)
- [x] ObservableCollection actualizada en UI thread via Dispatcher
- [x] Estados: Idle → Connecting → Connected | Failed | Disconnected
- [x] StubReaderDriver para desarrollo sin hardware
- [x] IReaderDriverFactory para abstraer la librería TCP/IP real" \
"módulo: readers,tipo: feature,prioridad: crítica,complejidad: L,done"

create_and_close \
"[Iter-1] Estilos base, converters y sistema de diseño" \
"## Qué se hizo
Sistema de estilos compartidos y converters de binding para toda la aplicación.

## Checklist
- [x] Colors.xaml: paleta brand, brushes semánticos, colores de estado
- [x] Controls.xaml: NavButton, Btn.Primary, Btn.Ghost, inputs, DataGrid, Card, Badge
- [x] Converters: BoolToVisibility, InverseBoolToVisibility, ReaderStateToBrush, ReaderStateToLabel, Equality" \
"módulo: core,tipo: feature,prioridad: alta,complejidad: M,done"

# Asignar milestone a todos los issues cerrados de una vez
echo ""
echo "Asignando milestone Iter-1 a todos los issues Done..."
MILESTONE=$(get_milestone_number)
if [ -n "$MILESTONE" ]; then
    gh issue list --repo "$REPO" --label "done" --state closed --json number \
        --jq '.[].number' | while read -r num; do
        gh api "repos/$REPO/issues/$num" --method PATCH \
            -F "milestone=$MILESTONE" > /dev/null 2>&1
        echo "  Milestone asignado a #$num"
    done
    ok "Milestones asignados."
else
    err "No se encontró el milestone Iter-1. Asígnalo manualmente en el board."
fi

echo ""
echo "=== Listo. Comprueba https://github.com/$REPO/issues ==="
