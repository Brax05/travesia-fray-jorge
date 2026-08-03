using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TravesiaACasa.Rooms;

/// <summary>
/// Herramienta de Editor que crea (o repara) el montaje de la cutscene de
/// misión de la Room 3 en la escena abierta: el GameObject AveNegra con su
/// BurbujaIdea dentro de Room_3, y el componente MissionIntroCutscene en
/// AveMision con todos los sprites y referencias cableados.
///
/// Es idempotente: si los objetos ya existen solo re-cablea sprites y
/// referencias, sin pisar posiciones/escala que se hayan ajustado a mano
/// en el Inspector. Correr con la escena Juego.unity abierta y guardar
/// después (Ctrl+S).
/// </summary>
public static class MissionCutsceneSetupTool
{
    private const string ArtePath = "Assets/Arte/juego/";

    [MenuItem("Game/Misión/Configurar cutscene de misión (Room 3)")]
    public static void Setup()
    {
        GameObject room3 = GameObject.Find("Room_3");
        if (room3 == null)
        {
            EditorUtility.DisplayDialog("Cutscene de misión",
                "No se encontró 'Room_3' en la escena abierta.\n" +
                "Abre Assets/Escenas/Juego.unity y vuelve a correr el comando.", "OK");
            return;
        }

        Transform aveMision = room3.transform.Find("AveMision");
        if (aveMision == null)
        {
            EditorUtility.DisplayDialog("Cutscene de misión",
                "Room_3 no tiene el hijo 'AveMision' (el carpinterito con MissionBird).", "OK");
            return;
        }

        // --- AveNegra (caminante), hija de Room_3 ---
        bool createdAveNegra = false;
        Transform aveNegra = room3.transform.Find("AveNegra");
        if (aveNegra == null)
        {
            aveNegra = new GameObject("AveNegra", typeof(SpriteRenderer)).transform;
            aveNegra.SetParent(room3.transform, false);
            aveNegra.localPosition = new Vector3(8.5f, -0.4f, 0f);
            aveNegra.localScale = Vector3.one * 0.09f;
            aveNegra.gameObject.SetActive(false);
            createdAveNegra = true;
        }
        SpriteRenderer aveRenderer = aveNegra.GetComponent<SpriteRenderer>();
        if (aveRenderer == null) aveRenderer = aveNegra.gameObject.AddComponent<SpriteRenderer>();
        aveRenderer.sprite = LoadSprite("AveNegra.png");
        aveRenderer.sortingOrder = 2;

        // --- BurbujaIdea, hija de AveNegra ---
        bool createdBurbuja = false;
        Transform burbuja = aveNegra.Find("BurbujaIdea");
        if (burbuja == null)
        {
            burbuja = new GameObject("BurbujaIdea", typeof(SpriteRenderer)).transform;
            burbuja.SetParent(aveNegra, false);
            burbuja.localPosition = new Vector3(4f, 11f, 0f);
            burbuja.localScale = Vector3.one * 1.4f;
            burbuja.gameObject.SetActive(false);
            createdBurbuja = true;
        }
        SpriteRenderer burbujaRenderer = burbuja.GetComponent<SpriteRenderer>();
        if (burbujaRenderer == null) burbujaRenderer = burbuja.gameObject.AddComponent<SpriteRenderer>();
        burbujaRenderer.sprite = LoadSprite("BurbujaIdea.png");
        burbujaRenderer.sortingOrder = 20;

        // --- Componente MissionIntroCutscene en AveMision ---
        MissionIntroCutscene cutscene = aveMision.GetComponent<MissionIntroCutscene>();
        bool createdComponent = cutscene == null;
        if (createdComponent)
            cutscene = aveMision.gameObject.AddComponent<MissionIntroCutscene>();

        SerializedObject so = new SerializedObject(cutscene);
        so.FindProperty("aveNegra").objectReferenceValue = aveNegra.gameObject;
        so.FindProperty("ideaBubble").objectReferenceValue = burbuja.gameObject;
        so.FindProperty("letreroFondoSprite").objectReferenceValue = LoadSprite("LetreroFondo.png");
        so.FindProperty("tituloMisionSprite").objectReferenceValue = LoadSprite("TituloMision.png");
        so.FindProperty("maderaSprite").objectReferenceValue = LoadSprite("madera.png");
        so.FindProperty("pegamentoSprite").objectReferenceValue = LoadSprite("pegamento.png");
        so.FindProperty("copihueSprite").objectReferenceValue = LoadSprite("copi.png");
        so.FindProperty("plumasSprite").objectReferenceValue = LoadSprite("plumas.png");
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(room3.scene);

        string resumen =
            (createdAveNegra ? "AveNegra creada" : "AveNegra ya existía (re-cableada)") + "\n" +
            (createdBurbuja ? "BurbujaIdea creada" : "BurbujaIdea ya existía (re-cableada)") + "\n" +
            (createdComponent ? "MissionIntroCutscene agregado a AveMision"
                              : "MissionIntroCutscene ya estaba (referencias actualizadas)") +
            "\n\nGuarda la escena (Ctrl+S) para conservar los cambios.";
        Debug.Log($"[MissionCutsceneSetupTool]\n{resumen}");
        EditorUtility.DisplayDialog("Cutscene de misión", resumen, "OK");

        Selection.activeGameObject = aveNegra.gameObject;
    }

    private static Sprite LoadSprite(string fileName)
    {
        string path = ArtePath + fileName;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite sprite)
                return sprite;
        }

        Debug.LogWarning($"[MissionCutsceneSetupTool] No se encontró un Sprite en '{path}'. " +
                         "Revisa que el PNG esté importado como Sprite (2D).");
        return null;
    }
}
