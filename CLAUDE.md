# CLAUDE.md

**Lee y respeta [AGENTS.md](AGENTS.md).** Es la fuente única de la verdad para la
coordinación entre agentes (Claude Code + Google Antigravity). No dupliques sus
reglas aquí; síguelas.

## Rol de Claude Code en el Patrón C

Tu fuerte en este repo (ver reparto en AGENTS.md §1):
- **Como Constructor:** lógica fina en C#, refactors, depuración, resolución de
  conflictos y flujo git limpio (`Assets/Scripts/**`, `Assets/Editor/**`).
- **Como Revisor:** cuando Antigravity construyó (UI/escenas/arte/diseño), revisa
  el diff, busca bugs y deja notas — NO edites lo que estás revisando.

## Recordatorios rápidos (detalle en AGENTS.md)
- Commit pequeño antes de ceder el turno; actualiza `HANDOFF.md`.
- NO edites `.meta`, escenas `.unity`, `.prefab` ni `.asset` sin ser el único
  agente en esa tarea (AGENTS.md §3).
- NO toques `Assets/_Recovery/` ni carpetas generadas (`Library/`, `build/`, etc.).
- Todo en español (código, commits, comunicación).
