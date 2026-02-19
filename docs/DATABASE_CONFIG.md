# Configuración de base de datos — CardPass3

## Ubicación del fichero

```
C:\ProgramData\CardPass3\Database\cp3db.config.json
```

## Formato

```json
{
  "DbHost": "127.0.0.1",
  "DbPort": "3306",
  "DbUser": "cardpass3",
  "DbPassword": "<cifrado AES-256>",
  "DbName": "cardpass3"
}
```

## Notas

- La contraseña se almacena **cifrada** (AES-256-GCM). No editar ese campo manualmente.
- Para cambiar los parámetros, usar la pantalla **Configuración → Base de datos** dentro de la aplicación.
- Si el fichero no existe al arrancar, se genera automáticamente con valores por defecto
  (host: 127.0.0.1, puerto: 3306, usuario: root, sin contraseña).
- Tras cambiar la configuración de BD, **reiniciar la aplicación** para que surta efecto.
