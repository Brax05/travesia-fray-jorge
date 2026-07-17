# HANDOFF — traspaso entre agentes

> Cada agente lee esto al empezar y lo actualiza al terminar su turno.
> Formato en AGENTS.md §5.

## 2026-07-17 — Antigravity (Gemini) → Claude Code
- Rol que tenía: Constructor
- Qué hice:
  - Creé e inserté los GameObjects de UI necesarios en `Juego.unity`: `DialoguePanelCanastero` (usando el sprite `dialogo_ave2.png`), `Avatar` (usando `ave2 hablando_1`) y `DialogueText` (UI Text con el diálogo inicial).
  - Enlacé `dialoguePanel` en el script `MissionBird` en el GameObject `AveMision` con el nuevo panel.
  - Conecté el UnityEvent `onInteract` de `GameHudController` con `MissionBird.OnInteractPressed()` en la escena.
  - Actualicé el `.gitignore` para omitir archivos temporales de recuperación de Unity.
- Estado: Cambios aplicados en escena. Listo para verificar en el editor de Unity.
- Qué falta:
  - Realizar prueba en play mode en Unity (acercarse al ave en Room 3, pulsar E o hacer clic en Interactuar en el HUD y verificar el despliegue del diálogo grande).
- NO TOCAR: La escena ya está configurada, no requiere cambios adicionales a menos que se detecte un bug durante la revisión.

## 2026-07-17 — Claude Code → Antigravity (Gemini)
- Rol que tenía: Revisor (Antigravity construyó IntroTutorialController.cs,
  MissionBird.cs y el wiring en Assets/Escenas/Juego.unity).
- Qué hice: revisé el diff sin editarlo. Encontré un bug funcional:
  - `MissionBird` (GameObject "AveMision") tiene `dialoguePanel: {fileID: 0}`
    sin asignar en el Inspector. Además, en la escena no existe ningún
    GameObject de "panel de diálogo grande" — solo se creó `GloboDialogo`
    (el globo chico). Resultado: `OnInteractPressed()` corta en seco por el
    null-check y nunca abre el diálogo.
  - Adicional: el `UnityEvent onInteract` de `GameHudController` sigue con
    `m_Calls: []` (vacío) — el botón "Interactuar" del HUD no está
    conectado a `MissionBird.OnInteractPressed`.
  - Conclusión: la misión del ave (Canastero) es inalcanzable tal como está
    (ni con el botón del HUD ni con la tecla E).
  - `IntroTutorialController.cs` sí quedó bien: revisé que `TutorialRoom1`
    cuelga de `HUD` (persistente, RoomManager solo instancia/destruye el
    contenido de la sala), así que el tutorial de intro se reproduce una
    sola vez como indica su comentario.
- Estado: sin commitear (AGENTS.md, CLAUDE.md, HANDOFF.md, TAREAS.md,
  .gitignore, y los scripts/escena de Antigravity siguen pendientes).
- Qué falta: crear el GameObject del panel de diálogo grande, asignarlo a
  `MissionBird.dialoguePanel`, y enganchar `AveMision.OnInteractPressed` al
  evento `onInteract` de `GameHudController`.
- NO TOCAR: `Assets/Scripts/Rooms/MissionBird.cs`,
  `Assets/Scripts/Rooms/IntroTutorialController.cs`, `Assets/Escenas/Juego.unity`
  (los dejo intactos: no soy Constructor en esta tarea).

## 2026-07-17 — Claude Code → Antigravity (Gemini) / usuario
- Rol que tenía: Revisor (Tarea A) + Constructor (Tarea B, en paralelo).
- Qué hice (Tarea A, revisión estática del fix del panel de diálogo):
  - Confirmé en el YAML de `Juego.unity` que `dialoguePanel` de `MissionBird`
    (GameObject `AveMision`) ahora apunta a `DialoguePanelCanastero`
    (fileID 880200010), que existe con sus hijos `Avatar` y `DialogueText`
    y arranca inactivo (`m_IsActive: 0`).
  - Confirmé que `onInteract.m_Calls` de `GameHudController` ahora invoca
    `MissionBird.OnInteractPressed` con `m_CallState: 2` (activo en editor
    y build), apuntando al `AveMision` correcto (fileID 880000004).
  - `AveMision` está activo en la escena. `MissionBird.cs` e
    `IntroTutorialController.cs` no tienen cambios de código desde la
    última revisión, siguen bien.
  - No encontré problemas en la revisión estática. **Sigue pendiente el
    playtest real en el Editor** (acercarse al ave en Room 3, Interactuar/E)
    que pidió Antigravity — no lo puedo correr desde acá.
- Qué hice (Tarea B, auditoría de `Assets/Scripts/Rooms/` en paralelo,
  archivos fuera de la Tarea A): encontré y corregí un bug en
  `CollectibleItem.cs` — leía `roomId` desde `RoomManager` (legacy del
  prototipo del cubo, nunca instanciado en `Juego.unity`) en vez de
  `RoomGraphManager`. Sin este fix, cualquier coleccionable que se agregara
  a futuro caería al fallback `"unknown_room"` y colisionaría el estado de
  "recogido" entre rooms distintas con el mismo `itemId`. Commit
  `eeb5f51`. Resto de scripts revisados sin bugs
  (`BirdPlayerController`, `BirdSpriteAnimator`, `CameraRoomFollower`,
  `CollectibleManager`, `GameHudController`, `HudMoveButton`,
  `InventoryManager`, `RoomTransitionUI`). Legacy confirmado sin tocar:
  `CubePlayerController`, `RoomData`, `RoomEdgeTrigger`, `Direction`.
- Estado: commit `eeb5f51` (solo `CollectibleItem.cs`). No toqué
  `Assets/Escenas/Juego.unity`, `MissionBird.cs` ni
  `IntroTutorialController.cs`.
- Qué falta: playtest en Unity Editor del panel de diálogo (Tarea A).
- NO TOCAR: nada bloqueado por ahora; ambas tareas listas para el siguiente
  paso (playtest de Tarea A, asignar Revisor para Tarea B si se quiere).
