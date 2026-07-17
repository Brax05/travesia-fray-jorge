# Protocolo de coordinación de agentes — PracticeProject-Game

> **Fuente única de la verdad.** Este archivo lo leen y respetan TODOS los agentes
> (Google Antigravity / Gemini y Claude Code / Opus). `CLAUDE.md` apunta aquí.
> Si cambias las reglas, cámbialas SOLO aquí.

Proyecto Unity 2D (juego de salas: personaje pájaro/cubo, grafo de salas,
coleccionables, menús y ajustes). Dos agentes trabajan sobre el mismo repo.
El objetivo es que **nunca choquen** y que cada uno aporte su fuerte.

---

## 1. Modo de trabajo activo: Patrón C — Constructor ↔ Revisor

En cada tarea hay dos roles. Se **rotan** según la fase; nunca los dos construyen
el mismo archivo a la vez.

- **CONSTRUCTOR**: implementa el cambio (escribe/edita código, crea sistemas).
- **REVISOR**: NO edita el código en revisión. Lee el diff, busca bugs, valida
  lógica, propone mejoras y las deja escritas (comentarios/notas), no aplicadas.
  El Constructor decide e integra.

Reparto de fuertes (orientativo, no rígido):

| Fase | Rol natural | Por qué |
|---|---|---|
| Diseño / exploración amplia / UI / escenas / arte | Antigravity construye | Contexto grande + multimodal |
| Lógica fina C#, refactors, depuración, git | Claude Code construye | Edición quirúrgica + razonamiento |
| Revisión cruzada | El que NO construyó | Cada modelo ve lo que al otro se le escapa |

**Regla del turno:** solo un agente tiene el rol CONSTRUCTOR a la vez sobre un
mismo conjunto de archivos. El otro es REVISOR o trabaja en otra carpeta.

---

## 2. Reglas anti-choque (obligatorias)

1. **Commit antes de ceder el turno.** Al terminar tu parte:
   `git add -A && git commit -m "..."`. El siguiente agente arranca con
   `git pull --rebase` (o revisando el último commit) sobre ese estado.
2. **Commits pequeños y frecuentes.** Nada de cambios gigantes sin commitear:
   si git no ve el trabajo, no puede protegerlo.
3. **Un solo agente por archivo a la vez.** Si necesitas tocar algo que el otro
   está editando, espera el commit o trabaja en otra rama/worktree.
4. **Deja el turno explícito.** Al ceder, escribe en `HANDOFF.md` (ver §5) qué
   hiciste, qué falta y qué NO tocar.

---

## 3. Archivos CRÍTICOS de Unity — máxima precaución

Estos archivos YAML se **corrompen con edición concurrente o merges**. Trátalos
con cuidado extra:

- `Assets/Escenas/**/*.unity` (escenas)
- `Assets/**/*.prefab`
- `Assets/**/*.asset` (ScriptableObjects: RoomData, ajustes, etc.)
- Cualquier `*.meta`
- `ProjectSettings/*.asset`, `Packages/manifest.json`

Reglas:
- **Un único agente** toca escenas/prefabs/.asset en una tarea dada. Nunca dos.
- **No edites `.meta` a mano.** Los genera Unity. Si un `.meta` cambió, avísalo
  en el handoff.
- **Cierra el Editor de Unity** mientras un agente modifica assets/escenas/meta,
  o Unity regenerará archivos y provocará conflictos.
- Si hay conflicto de merge en un `.unity`/`.prefab`, NO lo resuelvas a ciegas:
  párate y avisa al usuario.

---

## 4. Mapa de carpetas (para repartir sin pisarse)

- `Assets/Scripts/Rooms/` — sistema de salas: grafo, transiciones, jugador
  (pájaro/cubo), cámara, coleccionables, HUD, inventario, misiones.
- `Assets/Scripts/Menu/` — menú principal, panel de ajustes, brillo, settings.
- `Assets/Editor/` — herramientas de editor (build de escenas, setup, utils).
  Ojo: código de editor, solo corre en el Editor de Unity.
- `Assets/Escenas/` — escenas (CRÍTICO, §3).
- `Assets/Rooms/` — datos de salas (probables `.asset` de RoomData).
- `Assets/Arte/` — sprites y arte 2D.
- `Assets/Settings/` — configuración de render/proyecto (.asset, CRÍTICO §3).
- `Assets/_Recovery/` — recuperación de Unity. **NO tocar.**
- Generados / no versionar / no tocar: `Library/`, `Temp/`, `Logs/`, `build/`,
  `a.apk`, `a_BackUpThisFolder_...`, `*_BurstDebugInformation_DoNotShip`,
  `archivos-modificados/`.

Cuando trabajen en paralelo, divídanse por carpeta (p. ej. uno en `Menu/`, otro
en `Rooms/`) para no colisionar.

---

## 5. Handoff entre agentes

Antes de empezar una tarea, defínanla en `TAREAS.md` (raíz, **no versionado**,
está en `.gitignore`): qué se va a hacer, quién es Constructor y quién Revisor,
y qué archivos toca. Es un archivo vivo, se sobrescribe en cada sesión.

Al terminar, mantengan `HANDOFF.md` en la raíz (este SÍ versionado) con el
último traspaso:

```
## <fecha> — <de quién> → <a quién>
- Rol que tenía: Constructor / Revisor
- Qué hice: ...
- Estado: commit <hash>
- Qué falta: ...
- NO TOCAR: <archivos que quedan en proceso>
```

Cada agente lo lee al empezar y lo actualiza al terminar.

---

## 6. Convenciones del proyecto

- Idioma del código, commits y comunicación: **español**.
- Mensajes de commit en español, imperativo (ej: "Corrige transición entre salas").
- No cambiar versiones de paquetes ni Project Settings sin avisar al usuario.
- Antes de dar algo por terminado, si toca lógica de juego, verificar que
  compila en Unity (o dejar claro que falta verificarlo).
