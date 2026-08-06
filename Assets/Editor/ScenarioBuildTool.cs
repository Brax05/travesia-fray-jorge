using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Reemplaza el fondo genérico de las Rooms 1 a 6 por el escenario que
/// le corresponde a cada una (Assets/Arte/Escenarios/Escena N) y coloca
/// su decoración como sprites sueltos dentro de un hijo "Decoracion".
///
/// El fondo se escala al ANCHO con el que está construida la grilla de
/// rooms (~19 unidades, el ancho del arte viejo). Se ajusta al ancho y no
/// al alto porque el arte nuevo es 16:9 y el juego se ve más panorámico:
/// ajustando al alto quedarían barras del color de cámara a los costados.
/// Al ajustar al ancho sobra alto, que la cámara recorta arriba y abajo.
/// Como el ancho no cambia, las salidas, el clamp de entrada y las
/// posiciones ya afinadas siguen siendo válidas.
///
/// Las posiciones de la decoración salen de comparar cada "Ejemplo N"
/// contra su "Fondo N", así que son aproximadas: acomódalas a gusto en
/// la Scene view. Volver a correr la tool NO pisa lo que muevas a mano
/// (solo re-crea lo que falte); usa "Rehacer decoración desde cero" si
/// quieres volver al punto de partida.
///
/// Las Rooms 7, 8 y 9 no se tocan: todavía no hay arte para ellas.
/// </summary>
public static class ScenarioBuildTool
{
    private const string Base = "Assets/Arte/Escenarios";
    private const float PPU = 100f;

    /// <summary>
    /// Ancho en unidades de mundo que ocupa una room. Es el que tenía el
    /// fondo anterior (3376 px * 0.5625877 / 100) y con el que están
    /// separadas las rooms en la grilla, así que se conserva.
    /// </summary>
    private const float AnchoRoom = 18.996f;

    /// <summary>Nombres de la decoración vieja genérica que se retira.</summary>
    private static readonly string[] ObsoletosPrefijos = { "Arbusto_", "Hojas" };

    private struct Deco
    {
        public string file;   // ruta del png
        public float px, py;  // esquina sup-izq dentro de la imagen 1920x1080
        public float w, h;    // tamaño original en px
        public float scale;   // escala dentro de la composición
    }

    private static readonly Dictionary<int, string> Fondos = new Dictionary<int, string>
    {
        { 1, "Escena 1/fondo 1.png" }, { 2, "Escena 2/Fondo 2.jpg" }, { 3, "Escena 3/Fondo 3.jpg" },
        { 4, "Escena 4/Fondo 4.jpg" }, { 5, "Escena 5/Fondo 5.jpg" }, { 6, "Escena 6/fondo 6.jpg" },
    };

    // Rellenado desde el análisis de los "Ejemplo N".
    private static readonly Dictionary<int, Deco[]> Decoraciones = new Dictionary<int, Deco[]>
    {
        { 1, new[] {
            new Deco { file = "Assets/Arte/Escenarios/Escena 1/chagual 1.png", px = 144f, py = 176f, w = 373f, h = 253f, scale = 1f }, // 42.8
            new Deco { file = "Assets/Arte/Escenarios/Escena 1/Arbusto 1.png", px = 480f, py = 232f, w = 395f, h = 178f, scale = 1f }, // 71.7
            new Deco { file = "Assets/Arte/Escenarios/Escena 1/maravilla de campo 2.png", px = 800f, py = 800f, w = 733f, h = 270f, scale = 1f }, // 74.8
        } },
        { 2, new[] {
            new Deco { file = "Assets/Arte/Escenarios/Escena 2/Arbusto 1.png", px = 1032f, py = 208f, w = 395f, h = 178f, scale = 1f }, // 30.3
            new Deco { file = "Assets/Arte/Escenarios/Escena 2/chagual 4.png", px = 424f, py = 144f, w = 399f, h = 229f, scale = 1f }, // 43.4
        } },
        { 3, new[] {
            new Deco { file = "Assets/Arte/Escenarios/Escena 3/planta sin borde rojo.png", px = 1440f, py = 576f, w = 158f, h = 92f, scale = 1f }, // 66.4
            new Deco { file = "Assets/Arte/Escenarios/Escena 3/Arbusto 1.png", px = 536f, py = 360f, w = 395f, h = 178f, scale = 1f }, // 69.1
        } },
        { 4, new[] {
            new Deco { file = "Assets/Arte/Escenarios/Escena 4/roca 5.png", px = 1192f, py = 784f, w = 120f, h = 94f, scale = 1f }, // 30.9
            new Deco { file = "Assets/Arte/Escenarios/Escena 4/Flor sin borde rojo.png", px = 704f, py = 168f, w = 119f, h = 121f, scale = 1f }, // 37.2
            new Deco { file = "Assets/Arte/Escenarios/Escena 4/Camino 1.png", px = 952f, py = 0f, w = 774f, h = 1080f, scale = 1f }, // 39.8
            new Deco { file = "Assets/Arte/Escenarios/Escena 4/Maravilla de campo 7.png", px = 880f, py = 0f, w = 733f, h = 270f, scale = 1f }, // 56.0
        } },
        { 5, new[] {
            new Deco { file = "Assets/Arte/Escenarios/Escena 5/caracol sin borde rojo.png", px = 600f, py = 488f, w = 165f, h = 127f, scale = 1f }, // 31.7
            new Deco { file = "Assets/Arte/Escenarios/Escena 5/arbusto 9.png", px = 328f, py = 296f, w = 674f, h = 257f, scale = 1f }, // 58.7
            new Deco { file = "Assets/Arte/Escenarios/Escena 5/Arrayan 3.png", px = 1520f, py = 0f, w = 356f, h = 539f, scale = 1f }, // 65.7
            new Deco { file = "Assets/Arte/Escenarios/Escena 5/Maravilla de campo 6.png", px = 1176f, py = 784f, w = 674f, h = 257f, scale = 1f }, // 72.9
            new Deco { file = "Assets/Arte/Escenarios/Escena 5/arbusto 5.png", px = 960f, py = 312f, w = 395f, h = 178f, scale = 1f }, // 74.0
        } },
        { 6, new[] {
            new Deco { file = "Assets/Arte/Escenarios/Escena 6/Arrayan 3.png", px = 920f, py = 448f, w = 356f, h = 539f, scale = 1f }, // 20.5
            new Deco { file = "Assets/Arte/Escenarios/Escena 6/camino 2.png", px = 1032f, py = 344f, w = 885f, h = 739f, scale = 1f }, // 42.3
            new Deco { file = "Assets/Arte/Escenarios/Escena 6/Arbusto 1.png", px = 328f, py = 32f, w = 395f, h = 178f, scale = 1f }, // 55.8
            new Deco { file = "Assets/Arte/Escenarios/Escena 6/chagual 1.png", px = 1432f, py = 760f, w = 373f, h = 253f, scale = 1f }, // 57.6
            new Deco { file = "Assets/Arte/Escenarios/Escena 6/arbusto 9.png", px = 1248f, py = 0f, w = 674f, h = 257f, scale = 1f }, // 58.9
        } },
    };

    [MenuItem("Game/Escenarios/Reconstruir escenarios (Rooms 1-6)")]
    public static void Rebuild() => Run(false);

    [MenuItem("Game/Escenarios/Rehacer decoración desde cero (Rooms 1-6)")]
    public static void RebuildFromScratch()
    {
        if (!EditorUtility.DisplayDialog("Rehacer decoración",
                "Esto borra el hijo 'Decoracion' de las Rooms 1-6 y lo vuelve a crear, " +
                "perdiendo cualquier ajuste manual de posición.\n\n¿Seguir?", "Rehacer", "Cancelar"))
            return;
        Run(true);
    }

    private static void Run(bool wipeDecor)
    {
        var log = new System.Text.StringBuilder();
        int rooms = 0;

        for (int n = 1; n <= 6; n++)
        {
            GameObject room = GameObject.Find($"Room_{n}");
            if (room == null)
            {
                log.AppendLine($"⚠ Room_{n} no existe, se saltó");
                continue;
            }

            Sprite fondoSprite = LoadSprite($"{Base}/{Fondos[n]}");
            if (fondoSprite == null)
            {
                log.AppendLine($"⚠ Room_{n}: no se pudo cargar {Fondos[n]}");
                continue;
            }

            // El fondo cubre el ancho de la room; el alto que sobre lo recorta la cámara.
            float fit = AnchoRoom / (fondoSprite.rect.width / PPU);

            Transform fondo = room.transform.Find("Fondo");
            if (fondo == null)
                fondo = new GameObject("Fondo", typeof(SpriteRenderer)).transform;
            fondo.SetParent(room.transform, false);
            fondo.localPosition = Vector3.zero;
            fondo.localScale = Vector3.one * fit;
            SpriteRenderer fondoRenderer = fondo.GetComponent<SpriteRenderer>();
            if (fondoRenderer == null) fondoRenderer = fondo.gameObject.AddComponent<SpriteRenderer>();
            fondoRenderer.sprite = fondoSprite;
            fondoRenderer.sortingOrder = -10;
            fondo.SetAsFirstSibling();

            // Fuera la decoración genérica del arte viejo.
            foreach (Transform child in room.transform.Cast<Transform>().ToList())
            {
                if (ObsoletosPrefijos.Any(p => child.name.StartsWith(p)))
                    Object.DestroyImmediate(child.gameObject);
            }

            Transform decor = room.transform.Find("Decoracion");
            if (decor != null && wipeDecor)
            {
                Object.DestroyImmediate(decor.gameObject);
                decor = null;
            }
            if (decor == null)
            {
                decor = new GameObject("Decoracion").transform;
                decor.SetParent(room.transform, false);
                decor.localPosition = Vector3.zero;
            }

            int puestos = 0;
            if (Decoraciones.TryGetValue(n, out Deco[] lista))
            {
                foreach (Deco d in lista)
                {
                    string objName = System.IO.Path.GetFileNameWithoutExtension(d.file).Replace(" ", "_");
                    if (decor.Find(objName) != null) continue; // ya existe: respeta ajustes manuales

                    Sprite sprite = LoadSprite(d.file);
                    if (sprite == null)
                    {
                        log.AppendLine($"⚠ Room_{n}: falta {d.file}");
                        continue;
                    }

                    var go = new GameObject(objName, typeof(SpriteRenderer));
                    go.transform.SetParent(decor, false);

                    // (px,py) es la esquina sup-izq dentro de la imagen de 1920x1080;
                    // se pasa al centro y de ahí a coordenadas de mundo de la room.
                    float cx = d.px + d.w * d.scale * 0.5f;
                    float cy = d.py + d.h * d.scale * 0.5f;
                    go.transform.localPosition = new Vector3(
                        (cx - 960f) / PPU * fit,
                        (540f - cy) / PPU * fit,
                        0f);
                    go.transform.localScale = Vector3.one * fit * d.scale;

                    var sr = go.GetComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    // Los caminos van pegados al piso; el resto, detrás del jugador.
                    sr.sortingOrder = objName.ToLowerInvariant().StartsWith("camino") ? -8 : -5;
                    puestos++;
                }
            }

            rooms++;
            log.AppendLine($"Room_{n}: fondo '{System.IO.Path.GetFileName(Fondos[n])}' (escala {fit:0.###}), {puestos} decoraciones nuevas");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        log.AppendLine($"\n{rooms} rooms actualizadas. Rooms 7-9 sin tocar (falta arte).");
        log.AppendLine("Guarda la escena (Ctrl+S).");
        Debug.Log($"[ScenarioBuildTool]\n{log}");
        EditorUtility.DisplayDialog("Escenarios", log.ToString(), "OK");
    }

    private static Sprite LoadSprite(string path)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Sprite sprite) return sprite;

        Debug.LogWarning($"[ScenarioBuildTool] No hay Sprite en '{path}'. " +
                         "Revisa que esté importado como Sprite (2D).");
        return null;
    }
}
