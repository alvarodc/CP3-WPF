#!/usr/bin/env bash
# =============================================================================
# CardPass3 — Creación masiva de issues en GitHub
# Requisito: gh CLI autenticado → gh auth login
# Uso:       bash scripts/create-github-issues.sh
# =============================================================================

REPO="alvarodc/CP3-WPF"

GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${BLUE}=== CardPass3 — Creando issues en $REPO ===${NC}\n"

create_issue() {
    local title="$1"
    local body="$2"
    local labels="$3"
    local milestone="$4"

    echo -e "${YELLOW}→${NC} $title"
    if [ -n "$milestone" ]; then
        gh issue create --repo "$REPO" --title "$title" --body "$body" \
            --label "$labels" --milestone "$milestone"
    else
        gh issue create --repo "$REPO" --title "$title" --body "$body" \
            --label "$labels"
    fi
    sleep 0.6
}

# =============================================================================
# MILESTONES
# =============================================================================
echo -e "\n${BLUE}--- Milestones ---${NC}"
gh api repos/$REPO/milestones --method POST \
    -f title="Iter-1: Esqueleto + Login + Lectores" -f state="closed" 2>/dev/null || true
gh api repos/$REPO/milestones --method POST \
    -f title="Iter-2: Módulo Eventos" 2>/dev/null || true
gh api repos/$REPO/milestones --method POST \
    -f title="Iter-3: Módulo Usuarios" 2>/dev/null || true
gh api repos/$REPO/milestones --method POST \
    -f title="Iter-4: Hardware periférico" 2>/dev/null || true
gh api repos/$REPO/milestones --method POST \
    -f title="Iter-5: Admin y Configuración" 2>/dev/null || true

# =============================================================================
# LABELS
# =============================================================================
echo -e "\n${BLUE}--- Labels ---${NC}"
# Módulos
gh label create "módulo: core"     --color "0D1117" --description "Infraestructura y base"        --repo $REPO --force
gh label create "módulo: login"    --color "1565C0" --description "Autenticación y login"          --repo $REPO --force
gh label create "módulo: shell"    --color "1A237E" --description "Ventana principal y navegación" --repo $REPO --force
gh label create "módulo: readers"  --color "1B5E20" --description "Lectores de acceso"             --repo $REPO --force
gh label create "módulo: events"   --color "E65100" --description "Fichajes y eventos"             --repo $REPO --force
gh label create "módulo: users"    --color "4A148C" --description "Módulo de usuarios"             --repo $REPO --force
gh label create "módulo: hardware" --color "880E4F" --description "Hardware periférico"            --repo $REPO --force
gh label create "módulo: admin"    --color "37474F" --description "Administración y configuración" --repo $REPO --force
# Prioridades
gh label create "prioridad: crítica"      --color "B71C1C" --repo $REPO --force
gh label create "prioridad: alta"         --color "E53935" --repo $REPO --force
gh label create "prioridad: normal"       --color "FB8C00" --repo $REPO --force
gh label create "prioridad: nice-to-have" --color "81C784" --repo $REPO --force
# Complejidad
gh label create "complejidad: S"  --color "C8E6C9" --repo $REPO --force
gh label create "complejidad: M"  --color "FFF9C4" --repo $REPO --force
gh label create "complejidad: L"  --color "FFE0B2" --repo $REPO --force
gh label create "complejidad: XL" --color "FFCDD2" --repo $REPO --force
# Tipo
gh label create "tipo: feature"   --color "2196F3" --repo $REPO --force
gh label create "tipo: bug"       --color "F44336" --repo $REPO --force
gh label create "tipo: tech-debt" --color "9E9E9E" --repo $REPO --force
gh label create "done"            --color "388E3C" --description "Completado" --repo $REPO --force

echo -e "${GREEN}Labels OK${NC}\n"

# =============================================================================
# ITER-1 — COMPLETADOS (se crean con label "done" y se cierran)
# =============================================================================
echo -e "${BLUE}--- Iter-1: Completados ---${NC}"

create_issue "[Iter-1] Estructura solución y proyecto WPF .NET 8" \
"## Qué se hizo
Creación del proyecto CardPass3.WPF con .NET 8, estructura de carpetas por módulos y configuración del .csproj.

## Checklist
- [x] CardPass3.sln y CardPass3.WPF.csproj
- [x] Estructura Modules / Services / Data / Shared / Core
- [x] Dependencias: CommunityToolkit.Mvvm, Dapper, MySqlConnector, Serilog, M.E.Hosting
- [x] .gitignore, README, issue templates" \
"módulo: core,tipo: feature,prioridad: crítica,complejidad: M,done" \
"Iter-1: Esqueleto + Login + Lectores"

create_issue "[Iter-1] Sistema DI con Microsoft.Extensions.Hosting" \
"## Qué se hizo
Host genérico con DI, Serilog y ciclo de vida de la aplicación.

## Checklist
- [x] App.xaml.cs con IHost y OnStartup/OnExit
- [x] ConfigureServices con todos los registros
- [x] Graceful shutdown: desconexión de lectores al cerrar" \
"módulo: core,tipo: feature,prioridad: crítica,complejidad: S,done" \
"Iter-1: Esqueleto + Login + Lectores"

create_issue "[Iter-1] Modelos de datos — un fichero por modelo" \
"## Qué se hizo
Modelos C# mapeados a las tablas de CardPass3. Un fichero por clase en Data/Models/.

## Checklist
- [x] Reader, Operator, DbEvent, EventRow
- [x] User, Area, ConfigurationEntry
- [x] Document, Fingerprint, UserGroup
- [x] TimeAttendance + TimeAttendanceSlot
- [x] UserAttribute + UserAttributeGroup + UserAttributeValues
- [x] License" \
"módulo: core,tipo: feature,prioridad: crítica,complejidad: M,done" \
"Iter-1: Esqueleto + Login + Lectores"

create_issue "[Iter-1] Repositorios Dapper con interfaces" \
"## Qué se hizo
Capa de acceso a datos con Dapper, interfaces inyectables y queries parametrizadas.

## Checklist
- [x] IReaderRepository / ReaderRepository
- [x] IOperatorRepository / OperatorRepository
- [x] IEventRepository / EventRepository
- [x] IConfigurationRepository / ConfigurationRepository
- [x] Paginación keyset en GetPageAsync (no OFFSET)" \
"módulo: core,tipo: feature,prioridad: crítica,complejidad: L,done" \
"Iter-1: Esqueleto + Login + Lectores"

create_issue "[Iter-1] Configuración de BD en ProgramData con cifrado AES-256-GCM" \
"## Qué se hizo
Fichero JSON externo en %ProgramData%\\CardPass3\\Database\\cp3db.config.json con contraseña cifrada.
Reemplaza el antiguo sistema basado en app.config y RijndaelManaged.

## Checklist
- [x] DatabaseConfig — modelo serializable
- [x] DatabaseEncryption — AES-256-GCM (reemplaza RijndaelManaged obsoleto)
- [x] DatabaseConfigService — carga lazy, caché, ResetToDefault()
- [x] Migración graceful desde formato de cifrado anterior (no explota, avisa)
- [x] Valores por defecto de instalación: localhost/cardpass3/cardpass3/3306" \
"módulo: core,tipo: feature,prioridad: crítica,complejidad: L,done" \
"Iter-1: Esqueleto + Login + Lectores"

create_issue "[Iter-1] LoginWindow — diseño y autenticación SHA-256" \
"## Qué se hizo
Ventana de login borderless con verificación SHA-256 y gestión de errores de BD.

## Checklist
- [x] Diseño sin bordes, sombra, barra de acento azul superior
- [x] Logo shield en XAML puro, nombre del producto, versión
- [x] Verificación SHA-256 hex con espacios (compatible con BD original)
- [x] Grid de formulario con fila fija para error (el botón no se desplaza)
- [x] Diálogo DbConnectionErrorDialog con 3 opciones: configurar / resetear / volver
- [x] Patrón evento LoginSucceeded / DbConfigRequired (sin referencias VM→View)" \
"módulo: login,tipo: feature,prioridad: crítica,complejidad: M,done" \
"Iter-1: Esqueleto + Login + Lectores"

create_issue "[Iter-1] ShellWindow — sidebar + topbar + navegación por módulos" \
"## Qué se hizo
Ventana principal con sidebar de navegación agrupado por secciones y topbar con reloj.

## Checklist
- [x] Sidebar oscuro (220px) con iconos Segoe MDL2 y secciones agrupadas
- [x] Widget de estado de lectores en footer del sidebar
- [x] Topbar con título del módulo activo, nombre del operador y reloj en tiempo real
- [x] ContentControl como host de vistas
- [x] Logout via evento LogoutRequested (sin dependencia VM→View)" \
"módulo: shell,tipo: feature,prioridad: crítica,complejidad: M,done" \
"Iter-1: Esqueleto + Login + Lectores"

create_issue "[Iter-1] ReadersView — grid con badges de estado en tiempo real" \
"## Qué se hizo
Vista del módulo de lectores con DataGrid virtualizado y badges de color por estado de conexión.

## Checklist
- [x] Columnas: Estado (badge), Descripción, IP (Consolas), Puerto, Área, Último intento, Error
- [x] Badges semánticos: verde/naranja/rojo/gris según ReaderConnectionState
- [x] Toolbar: Conectar, Desconectar, Abrir relé
- [x] Virtualización activada (VirtualizationMode=Recycling)
- [x] Contadores conectados/total en header" \
"módulo: readers,tipo: feature,prioridad: crítica,complejidad: M,done" \
"Iter-1: Esqueleto + Login + Lectores"

create_issue "[Iter-1] ReaderConnectionService — conexión paralela asíncrona" \
"## Qué se hizo
Servicio singleton que conecta todos los lectores habilitados en paralelo al arranque.

## Checklist
- [x] Conexión paralela con SemaphoreSlim (máx. 10 simultáneas)
- [x] ObservableCollection<ReaderConnectionInfo> actualizada en UI thread via Dispatcher
- [x] Estados: Idle → Connecting → Connected | Failed | Disconnected
- [x] StubReaderDriver para desarrollo sin hardware (80% éxito simulado)
- [x] IReaderDriverFactory para abstraer la librería TCP/IP real" \
"módulo: readers,tipo: feature,prioridad: crítica,complejidad: L,done" \
"Iter-1: Esqueleto + Login + Lectores"

create_issue "[Iter-1] Estilos base, converters y sistema de diseño" \
"## Qué se hizo
Sistema de estilos compartidos (Controls.xaml, Colors.xaml) y converters de binding.

## Checklist
- [x] Colors.xaml: paleta brand, brushes semánticos, colores de estado de lectores
- [x] Controls.xaml: NavButton, NavButton.Active, Btn.Primary, Btn.Ghost
- [x] Controls.xaml: Input.Text, Input.Password, Grid.Base, Card, Badge.State
- [x] Converters: BoolToVisibility, InverseBoolToVisibility
- [x] Converters: ReaderStateToBrush, ReaderStateToLabel, EqualityConverter" \
"módulo: core,tipo: feature,prioridad: alta,complejidad: M,done" \
"Iter-1: Esqueleto + Login + Lectores"

# Cerrar todos los issues de done automáticamente
echo -e "\n${YELLOW}Cerrando issues completados...${NC}"
gh issue list --repo $REPO --label "done" --json number --jq '.[].number' | \
    xargs -I{} gh issue close {} --repo $REPO 2>/dev/null
echo -e "${GREEN}Issues Done cerrados.${NC}"

# =============================================================================
# ITER-1 — EN CURSO
# =============================================================================
echo -e "\n${BLUE}--- Iter-1: En curso ---${NC}"

create_issue "[Iter-1] Integración driver TCP/IP lectores reales" \
"## Descripción
Reemplazar el StubReaderDriver por la librería TCP/IP real de los lectores de acceso.

## Pendiente
- [ ] Implementar IReaderDriver sobre la librería existente del sistema anterior
- [ ] Mapear eventos hardware → software (acceso, denegado, alarma...)
- [ ] Registrar driver real en DefaultReaderDriverFactory según Reader.Driver enum
- [ ] Probar conexión con lectores físicos en instalación de prueba

## Notas
El StubReaderDriver simula con 80% de éxito. La abstracción IReaderDriver ya está definida, solo hay que implementarla con la librería real." \
"módulo: readers,tipo: feature,prioridad: crítica,complejidad: L" \
"Iter-1: Esqueleto + Login + Lectores"

# =============================================================================
# ITER-1 — PENDIENTE PARA CERRAR ITERACIÓN
# =============================================================================
echo -e "\n${BLUE}--- Iter-1: Pendiente ---${NC}"

create_issue "[Iter-1] Pantalla de configuración de BD" \
"## Descripción
Vista para modificar los parámetros de conexión a la BD sin editar el JSON manualmente.
Sustituirá el botón 'Configurar conexión (próximamente)' del diálogo de error.

## Pendiente
- [ ] DatabaseConfigView.xaml — formulario host/puerto/usuario/contraseña/bd
- [ ] Botón 'Probar conexión' con feedback visual inmediato
- [ ] Guardar solo si la conexión es exitosa
- [ ] Integrar en ShellWindow bajo Configuración → Base de datos
- [ ] Reemplazar el placeholder en DbConnectionErrorDialog

## Notas
DatabaseConfigViewModel ya implementado con TestConnectionAsync y SaveAsync." \
"módulo: admin,tipo: feature,prioridad: alta,complejidad: M" \
"Iter-1: Esqueleto + Login + Lectores"

create_issue "[Iter-1] Reset de contraseñas de operadores" \
"## Descripción
Las contraseñas de operadores de instalaciones existentes están en formato antiguo.
Necesitamos una forma de resetearlas al formato SHA-256 actual.

## Pendiente
- [ ] Formulario: seleccionar operador + nueva contraseña + confirmación
- [ ] Guardar como SHA-256 hex con espacios (formato compatible con verificación actual)
- [ ] Integrar en módulo de operadores (Iter-5) o como utilidad standalone de migración

## Decisión
No se parsea el formato antiguo. Reset manual obligatorio durante la migración." \
"módulo: admin,tipo: feature,prioridad: alta,complejidad: S" \
"Iter-1: Esqueleto + Login + Lectores"

# =============================================================================
# ITER-2
# =============================================================================
echo -e "\n${BLUE}--- Iter-2: Módulo Eventos ---${NC}"

create_issue "[Iter-2] EventsView — grid paginado con keyset pagination" \
"## Descripción
Vista principal del módulo de fichajes. Debe rendir bien con cientos de miles de registros.

## Pendiente
- [ ] DataGrid virtualizado: Fecha/hora, Usuario, Apellido, Lector, Incidencia
- [ ] Paginación keyset usando idx_events_cover (no OFFSET)
- [ ] Botones Anterior / Siguiente con indicador de página
- [ ] EventsViewModel con estado de paginación" \
"módulo: events,tipo: feature,prioridad: crítica,complejidad: L" \
"Iter-2: Módulo Eventos"

create_issue "[Iter-2] Filtros del módulo de eventos" \
"## Descripción
Panel de filtros para el grid de fichajes.

## Pendiente
- [ ] Rango de fechas (DatePicker desde/hasta)
- [ ] Filtro por lector (ComboBox)
- [ ] Filtro por tipo de incidencia
- [ ] Búsqueda por nombre/apellido
- [ ] Limpiar filtros — resetea paginación a la primera página" \
"módulo: events,tipo: feature,prioridad: alta,complejidad: M" \
"Iter-2: Módulo Eventos"

create_issue "[Iter-2] Exportación de eventos a CSV/Excel" \
"## Descripción
Exportar el resultado filtrado actual a fichero.

## Pendiente
- [ ] Exportar página actual vs. exportar todo el rango filtrado
- [ ] CSV: UTF-8 con BOM, separador configurable
- [ ] Excel: cabeceras, formato fecha, autoajuste columnas
- [ ] Diálogo de progreso para exportaciones grandes (>10.000 filas)" \
"módulo: events,tipo: feature,prioridad: normal,complejidad: M" \
"Iter-2: Módulo Eventos"

# =============================================================================
# ITER-3
# =============================================================================
echo -e "\n${BLUE}--- Iter-3: Módulo Usuarios ---${NC}"

create_issue "[Iter-3] UsersView — grid con búsqueda y filtros" \
"## Descripción
Vista principal del módulo de usuarios.

## Pendiente
- [ ] Grid: Nº registro, Nombre, Apellido, Nº tarjeta, Grupo, Estado, Área actual
- [ ] Búsqueda en tiempo real por nombre/apellido/tarjeta
- [ ] Filtros por grupo y estado (activo/inactivo/eliminado)
- [ ] Toolbar: Nuevo, Editar, Dar de baja" \
"módulo: users,tipo: feature,prioridad: crítica,complejidad: L" \
"Iter-3: Módulo Usuarios"

create_issue "[Iter-3] Ficha de usuario — datos, foto y atributos" \
"## Descripción
Formulario completo de alta/edición de usuario.

## Pendiente
- [ ] Datos básicos: nombre, apellido, nº registro, tarjeta, grupo, fechas validez
- [ ] Foto: captura webcam o carga desde fichero, lazy-load en grid
- [ ] Atributos personalizados dinámicos (user_column_N)
- [ ] Documentos adjuntos
- [ ] Historial de últimos accesos del usuario" \
"módulo: users,tipo: feature,prioridad: crítica,complejidad: XL" \
"Iter-3: Módulo Usuarios"

create_issue "[Iter-3] Gestión de grupos de usuarios" \
"## Descripción
CRUD de grupos con configuración de métodos de autenticación y horarios.

## Pendiente
- [ ] Grid de grupos: nombre, descripción, métodos auth, por defecto
- [ ] Alta/edición: card/pin/barcode/fingerprint auth
- [ ] Asignación de horarios de acceso (TimeAttendance)
- [ ] Bloquear eliminación si hay usuarios asignados" \
"módulo: users,tipo: feature,prioridad: alta,complejidad: M" \
"Iter-3: Módulo Usuarios"

# =============================================================================
# ITER-4
# =============================================================================
echo -e "\n${BLUE}--- Iter-4: Hardware periférico ---${NC}"

create_issue "[Iter-4] Integración webcam para foto de usuario" \
"## Descripción
Captura de foto desde webcam dentro de la ficha de usuario.

## Pendiente
- [ ] Selección de cámara disponible
- [ ] Preview en tiempo real en el formulario
- [ ] Captura, recorte y guardado como JPEG en BD (campo photo)" \
"módulo: hardware,tipo: feature,prioridad: normal,complejidad: M" \
"Iter-4: Hardware periférico"

create_issue "[Iter-4] Integración lector de huellas dactilares" \
"## Descripción
Captura y almacenamiento de huellas desde dispositivo USB.

## Pendiente
- [ ] Abstracción IFingerprintDevice para diferentes modelos
- [ ] Captura en ficha de usuario (hasta N dedos)
- [ ] Almacenamiento en tabla fingerprints con template format
- [ ] Envío de plantillas a lectores con soporte biométrico" \
"módulo: hardware,tipo: feature,prioridad: normal,complejidad: XL" \
"Iter-4: Hardware periférico"

# =============================================================================
# ITER-5
# =============================================================================
echo -e "\n${BLUE}--- Iter-5: Admin y Configuración ---${NC}"

create_issue "[Iter-5] Módulo Áreas — jerarquía y antipassback" \
"## Pendiente
- [ ] TreeView con jerarquía de áreas (parent_id_area)
- [ ] Alta/edición: nombre, descripción, área padre, antipassback, free_exit
- [ ] Asignación de lectores a áreas
- [ ] Visualización de usuarios actualmente en el área" \
"módulo: admin,tipo: feature,prioridad: alta,complejidad: L" \
"Iter-5: Admin y Configuración"

create_issue "[Iter-5] Módulo Operadores — CRUD y gestión de funciones" \
"## Pendiente
- [ ] Grid de operadores: nombre, descripción, estado
- [ ] Alta/edición con reset de contraseña (SHA-256)
- [ ] Asignación de funciones (operator_function_assoc)
- [ ] Bloquear eliminación del operador con sesión activa" \
"módulo: admin,tipo: feature,prioridad: alta,complejidad: M" \
"Iter-5: Admin y Configuración"

create_issue "[Iter-5] Módulo Configuración — parámetros del sistema y BD" \
"## Pendiente
- [ ] DatabaseConfigView — sustituir el 'próximamente' del diálogo de error
- [ ] Parámetros generales desde tabla configuration (key-value)
- [ ] Rutas de ficheros: fotos, documentos, backups
- [ ] Configuración de log: nivel mínimo y retención" \
"módulo: admin,tipo: feature,prioridad: alta,complejidad: L" \
"Iter-5: Admin y Configuración"

create_issue "[Iter-5] Backup y restore de base de datos" \
"## Pendiente
- [ ] Backup a fichero .sql con timestamp en el nombre
- [ ] Restore desde fichero con confirmación explícita
- [ ] Backup automático programado (hora y retención configurables)
- [ ] Usar MySqlBackup.NET (ya presente en el sistema original)" \
"módulo: admin,tipo: feature,prioridad: normal,complejidad: M" \
"Iter-5: Admin y Configuración"

# =============================================================================
# TECH DEBT
# =============================================================================
echo -e "\n${BLUE}--- Tech Debt ---${NC}"

create_issue "[Tech Debt] Tests unitarios — capa crítica" \
"## Pendiente
- [ ] Tests de DatabaseEncryption (encrypt/decrypt round-trip, formato corrupto)
- [ ] Tests de LoginViewModel (credenciales ok/ko, DbConfigRequired)
- [ ] Tests de EventRepository (keyset pagination, filtros)
- [ ] Mock de IDatabaseConnectionFactory para tests sin BD real" \
"módulo: core,tipo: tech-debt,prioridad: normal,complejidad: L" \
""

create_issue "[Tech Debt] Internacionalización (i18n) ES/EN" \
"## Pendiente
- [ ] Extraer todos los literales de UI a ficheros de recursos (.resx)
- [ ] Soporte inicial ES (por defecto) y EN
- [ ] Selector de idioma en login o configuración" \
"módulo: core,tipo: tech-debt,prioridad: nice-to-have,complejidad: L" \
""

# =============================================================================
# RESUMEN
# =============================================================================
echo -e "\n${GREEN}=== ¡Hecho! Issues creados en $REPO ===${NC}"
echo ""
echo -e "${YELLOW}Pasos manuales en el Project board:${NC}"
echo "  1. Los issues 'done' ya están cerrados — arrástralos a la columna Done"
echo "  2. El issue de integración TCP/IP → columna In Progress"
echo "  3. Asigna los custom fields (Iteración, Módulo, Prioridad, Complejidad)"
echo "     desde el board — gh CLI no soporta ProjectV2 custom fields directamente"
echo ""
echo -e "${YELLOW}Truco para asignar fields en lote:${NC}"
echo "  En el board, agrupa por Iteración y arrastra todos los issues de cada grupo"
echo "  al field correspondiente. Es más rápido que uno a uno."
