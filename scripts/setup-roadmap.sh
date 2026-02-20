#!/usr/bin/env bash
# =============================================================================
# CardPass3 — Configura la vista Roadmap en el GitHub Project
#
# Qué hace:
#   1. Asigna fechas de inicio y fin a los 5 milestones
#   2. Añade los date fields "Fecha inicio" y "Fecha fin" al proyecto vía GraphQL
#   3. Crea la vista Roadmap (si la API lo permite)
#
# Requisitos:
#   - gh CLI autenticado con scope "project": gh auth login --scopes project
#   - Tener el número de tu proyecto GitHub (ver instrucciones abajo)
#
# Uso:
#   bash scripts/setup-roadmap.sh
#
# Para encontrar tu PROJECT_NUMBER:
#   Ve a https://github.com/users/alvarodc/projects  (o /orgs/... si es de org)
#   El número aparece en la URL del proyecto: /projects/1  → PROJECT_NUMBER=1
# =============================================================================

REPO="alvarodc/CP3-WPF"
REPO_OWNER="alvarodc"
REPO_NAME="CP3-WPF"
PROJECT_NUMBER=6          # ← Cambia esto por el número de tu proyecto

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

ok()   { echo -e "${GREEN}✓${NC} $1"; }
step() { echo -e "${YELLOW}→${NC} $1"; }
err()  { echo -e "${RED}✗${NC} $1"; }
info() { echo -e "${CYAN}ℹ${NC} $1"; }

echo -e "${CYAN}=== CardPass3 — Configurando Roadmap ===${NC}\n"

# =============================================================================
# PASO 1 — Verificar autenticación con scope "project"
# =============================================================================
info "Verificando autenticación..."
if ! gh auth status 2>&1 | grep -q "project"; then
    echo ""
    err "El token de gh no tiene el scope 'project'."
    echo "  Ejecuta: gh auth login --scopes project"
    echo "  y vuelve a ejecutar este script."
    exit 1
fi
ok "Autenticación con scope project OK"

# =============================================================================
# PASO 2 — Asignar fechas a los milestones
# =============================================================================
echo -e "\n${CYAN}--- Fechas de milestones ---${NC}"

set_milestone_dates() {
    local title_pattern="$1"
    local due_on="$2"      # GitHub solo soporta due_date en milestones, no start date

    local number
    number=$(gh api "repos/$REPO/milestones?state=all&per_page=50" \
        --jq ".[] | select(.title | contains(\"$title_pattern\")) | .number" 2>/dev/null | head -1)

    if [ -z "$number" ]; then
        err "Milestone no encontrado: $title_pattern"
        return
    fi

    gh api "repos/$REPO/milestones/$number" \
        --method PATCH \
        -f due_on="${due_on}T23:59:59Z" > /dev/null 2>&1

    ok "Milestone '$title_pattern' → due date $due_on"
}

# Fechas orientativas — ajústalas a tu ritmo real
set_milestone_dates "Iter-1" "2026-03-31"
set_milestone_dates "Iter-2" "2026-04-30"
set_milestone_dates "Iter-3" "2026-06-30"
set_milestone_dates "Iter-4" "2026-07-31"
set_milestone_dates "Iter-5" "2026-09-30"

# =============================================================================
# PASO 3 — Obtener el Project ID (GraphQL node ID)
# =============================================================================
echo -e "\n${CYAN}--- Obteniendo Project ID ---${NC}"

PROJECT_ID=$(gh api graphql -f query='
  query($owner: String!, $number: Int!) {
    user(login: $owner) {
      projectV2(number: $number) {
        id
        title
      }
    }
  }' -f owner="$REPO_OWNER" -F number="$PROJECT_NUMBER" \
  --jq '.data.user.projectV2.id' 2>/dev/null)

# Si es de organización, intenta con organization en vez de user
if [ -z "$PROJECT_ID" ]; then
    PROJECT_ID=$(gh api graphql -f query='
      query($owner: String!, $number: Int!) {
        organization(login: $owner) {
          projectV2(number: $number) {
            id
            title
          }
        }
      }' -f owner="$REPO_OWNER" -F number="$PROJECT_NUMBER" \
      --jq '.data.organization.projectV2.id' 2>/dev/null)
fi

if [ -z "$PROJECT_ID" ]; then
    err "No se pudo obtener el Project ID."
    echo ""
    echo "  Comprueba que PROJECT_NUMBER=$PROJECT_NUMBER es correcto."
    echo "  Ve a: https://github.com/users/$REPO_OWNER/projects"
    echo "  El número está en la URL del proyecto."
    exit 1
fi

ok "Project ID: $PROJECT_ID"

# =============================================================================
# PASO 4 — Crear date fields "Fecha inicio" y "Fecha fin"
# =============================================================================
echo -e "\n${CYAN}--- Creando date fields ---${NC}"

create_date_field() {
    local name="$1"

    step "Creando field '$name'..."

    local result
    result=$(gh api graphql -f query='
      mutation($projectId: ID!, $name: String!) {
        createProjectV2Field(input: {
          projectId: $projectId
          dataType: DATE
          name: $name
        }) {
          projectV2Field {
            ... on ProjectV2Field {
              id
              name
            }
          }
        }
      }' -f projectId="$PROJECT_ID" -f name="$name" 2>&1)

    if echo "$result" | grep -q '"id"'; then
        local field_id
        field_id=$(echo "$result" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
        ok "Field '$name' creado (ID: $field_id)"
        echo "$field_id"
    elif echo "$result" | grep -qi "already exists\|duplicate\|existing"; then
        info "Field '$name' ya existe — omitido"
        # Obtener el ID del field existente
        gh api graphql -f query='
          query($projectId: ID!) {
            node(id: $projectId) {
              ... on ProjectV2 {
                fields(first: 30) {
                  nodes {
                    ... on ProjectV2Field {
                      id
                      name
                    }
                  }
                }
              }
            }
          }' -f projectId="$PROJECT_ID" \
          --jq ".data.node.fields.nodes[] | select(.name == \"$name\") | .id" 2>/dev/null
    else
        err "Error creando field '$name': $result"
        echo ""
    fi
}

FIELD_START_ID=$(create_date_field "Fecha inicio")
FIELD_END_ID=$(create_date_field "Fecha fin")

echo ""
info "Field 'Fecha inicio' ID: ${FIELD_START_ID:-no obtenido}"
info "Field 'Fecha fin'    ID: ${FIELD_END_ID:-no obtenido}"

# =============================================================================
# PASO 5 — Asignar fechas a los items del proyecto por iteración
# =============================================================================
echo -e "\n${CYAN}--- Asignando fechas a los issues del proyecto ---${NC}"

# Obtener todos los items del proyecto con su título y node ID
step "Obteniendo items del proyecto..."

ITEMS_JSON=$(gh api graphql -f query='
  query($projectId: ID!) {
    node(id: $projectId) {
      ... on ProjectV2 {
        items(first: 100) {
          nodes {
            id
            content {
              ... on Issue {
                title
                number
              }
            }
          }
        }
      }
    }
  }' -f projectId="$PROJECT_ID" 2>/dev/null)

if [ -z "$ITEMS_JSON" ]; then
    err "No se pudieron obtener los items del proyecto."
    echo "  Asigna las fechas manualmente en el Roadmap view."
    exit 0
fi

ok "Items obtenidos"

# Función para asignar fecha a un item
set_item_date() {
    local item_id="$1"
    local field_id="$2"
    local date_value="$3"

    [ -z "$item_id" ] || [ -z "$field_id" ] || [ -z "$date_value" ] && return

    gh api graphql -f query='
      mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $value: Date!) {
        updateProjectV2ItemFieldValue(input: {
          projectId: $projectId
          itemId: $itemId
          fieldId: $fieldId
          value: { date: $value }
        }) {
          projectV2Item { id }
        }
      }' \
      -f projectId="$PROJECT_ID" \
      -f itemId="$item_id" \
      -f fieldId="$field_id" \
      -f value="$date_value" > /dev/null 2>&1
}

# Asignar fechas según el prefijo [Iter-N] del título
assign_dates_by_iter() {
    local iter_prefix="$1"
    local start_date="$2"
    local end_date="$3"

    local count=0
    while IFS= read -r item_id; do
        [ -z "$item_id" ] && continue
        [ -n "$FIELD_START_ID" ] && set_item_date "$item_id" "$FIELD_START_ID" "$start_date"
        [ -n "$FIELD_END_ID"   ] && set_item_date "$item_id" "$FIELD_END_ID"   "$end_date"
        count=$((count + 1))
    done < <(echo "$ITEMS_JSON" |         jq -r --arg prefix "$iter_prefix"         '.data.node.items.nodes[] | select(.content.title // "" | startswith($prefix)) | .id'         2>/dev/null)

    ok "$iter_prefix → $start_date a $end_date ($count issues)"
}

if [ -n "$FIELD_START_ID" ] && [ -n "$FIELD_END_ID" ]; then
    assign_dates_by_iter "[Iter-1]"     "2026-02-01" "2026-03-31"
    assign_dates_by_iter "[Iter-2]"     "2026-04-01" "2026-04-30"
    assign_dates_by_iter "[Iter-3]"     "2026-05-01" "2026-06-30"
    assign_dates_by_iter "[Iter-4]"     "2026-07-01" "2026-07-31"
    assign_dates_by_iter "[Iter-5]"     "2026-08-01" "2026-09-30"
    assign_dates_by_iter "[Tech Debt]"  "2026-02-01" "2026-09-30"
else
    err "No se pudieron asignar fechas a los items (fields no creados)."
    echo "  Asigna las fechas de inicio y fin manualmente desde el Roadmap."
fi

# =============================================================================
# PASO 6 — Instrucciones para activar la vista Roadmap
# =============================================================================
echo ""
echo -e "${CYAN}=== Último paso: activar la vista Roadmap ===${NC}"
echo ""
echo "La API de GitHub no permite crear vistas programáticamente."
echo "Hazlo en 30 segundos desde el navegador:"
echo ""
echo -e "  1. Ve a ${YELLOW}https://github.com/users/$REPO_OWNER/projects/$PROJECT_NUMBER${NC}"
echo "  2. Clic en el botón  '+ New view'  (junto a las pestañas Board/Table)"
echo "  3. Selecciona  'Roadmap'"
echo "  4. En la nueva vista, haz clic en  'Group by' → Milestone"
echo "  5. En  'Date field' → selecciona  'Fecha inicio'  como start"
echo "     y  'Fecha fin'  como target date"
echo ""
echo -e "${GREEN}¡Listo! El roadmap mostrará las 5 iteraciones como bloques temporales.${NC}"
