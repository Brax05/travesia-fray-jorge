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
        so.FindProperty("avatarSprite").objectReferenceValue = LoadSprite("aveDialogo.png");
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

    private struct MaterialSpec
    {
        public string room;
        public string objectName;
        public string spriteFile;
        public string inventoryKey;
        public string displayName;
        public Vector2 localPos;
    }

    private static readonly MaterialSpec[] Materiales =
    {
        new MaterialSpec { room = "Room_1", objectName = "Material_Madera", spriteFile = "madera.png",
            inventoryKey = "madera_maiten", displayName = "Madera de maitén", localPos = new Vector2(4.2f, -2.4f) },
        new MaterialSpec { room = "Room_5", objectName = "Material_Pegamento", spriteFile = "pegamento.png",
            inventoryKey = "pegamento_larvas", displayName = "Pegamento de larvas", localPos = new Vector2(-3.9f, -2.5f) },
        new MaterialSpec { room = "Room_6", objectName = "Material_Copihue", spriteFile = "copi.png",
            inventoryKey = "copihue", displayName = "un Copihue", localPos = new Vector2(4.4f, -2.2f) },
        new MaterialSpec { room = "Room_7", objectName = "Material_Pluma_1", spriteFile = "plumas.png",
            inventoryKey = "pluma_alicanto", displayName = "una Pluma de alicanto", localPos = new Vector2(-4.3f, -2.4f) },
        new MaterialSpec { room = "Room_9", objectName = "Material_Pluma_2", spriteFile = "plumas.png",
            inventoryKey = "pluma_alicanto", displayName = "una Pluma de alicanto", localPos = new Vector2(3.6f, -2.6f) },
    };

    [MenuItem("Game/Misión/Colocar materiales de la misión")]
    public static void PlaceMaterials()
    {
        GameObject aveMisionGo = GameObject.Find("Room_3/AveMision");
        MissionIntroCutscene cutscene = aveMisionGo != null ? aveMisionGo.GetComponent<MissionIntroCutscene>() : null;
        if (cutscene == null)
        {
            EditorUtility.DisplayDialog("Materiales de misión",
                "No se encontró MissionIntroCutscene en Room_3/AveMision.\n" +
                "Corre primero 'Configurar cutscene de misión (Room 3)'.", "OK");
            return;
        }

        // Managers de inventario: sin ellos los pickups no suman al conteo.
        GameObject managersHost = GameObject.Find("GraphManager");
        if (managersHost != null)
        {
            if (managersHost.GetComponent<CollectibleManager>() == null)
                managersHost.AddComponent<CollectibleManager>();
            if (managersHost.GetComponent<InventoryManager>() == null)
                managersHost.AddComponent<InventoryManager>();
        }

        var wired = new System.Collections.Generic.List<GameObject>();
        var resumen = new System.Text.StringBuilder();
        int creados = 0;

        foreach (MaterialSpec spec in Materiales)
        {
            GameObject room = GameObject.Find(spec.room);
            if (room == null)
            {
                resumen.AppendLine($"⚠ {spec.room} no existe, se saltó {spec.objectName}");
                continue;
            }

            Transform pickup = room.transform.Find(spec.objectName);
            bool created = pickup == null;
            if (created)
            {
                pickup = new GameObject(spec.objectName,
                    typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(MaterialPickup)).transform;
                pickup.SetParent(room.transform, false);
                pickup.localPosition = spec.localPos;
                creados++;
            }

            Sprite sprite = LoadSprite(spec.spriteFile);
            SpriteRenderer renderer = pickup.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 1;

            if (created && sprite != null)
            {
                // Escala uniforme para que el material mida ~0.9 unidades de alto.
                float scale = 0.9f / sprite.bounds.size.y;
                pickup.localScale = Vector3.one * scale;
            }

            CircleCollider2D collider = pickup.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            if (sprite != null)
                collider.radius = Mathf.Max(sprite.bounds.extents.x, sprite.bounds.extents.y);

            MaterialPickup component = pickup.GetComponent<MaterialPickup>();
            SerializedObject pickupSo = new SerializedObject(component);
            pickupSo.FindProperty("itemId").stringValue = spec.objectName.ToLowerInvariant();
            pickupSo.FindProperty("inventoryKey").stringValue = spec.inventoryKey;
            pickupSo.FindProperty("displayName").stringValue = spec.displayName;
            pickupSo.ApplyModifiedPropertiesWithoutUndo();

            if (created)
                pickup.gameObject.SetActive(false); // aparecen al aceptar la misión

            wired.Add(pickup.gameObject);
            resumen.AppendLine($"{(created ? "Creado" : "Re-cableado")}: {spec.room}/{spec.objectName} ({spec.displayName})");
        }

        SerializedObject cutsceneSo = new SerializedObject(cutscene);
        SerializedProperty array = cutsceneSo.FindProperty("materialPickups");
        array.arraySize = wired.Count;
        for (int i = 0; i < wired.Count; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = wired[i];
        cutsceneSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(cutscene.gameObject.scene);

        resumen.AppendLine($"\n{creados} creados, {wired.Count} cableados a MissionIntroCutscene.");
        resumen.AppendLine("Guarda la escena (Ctrl+S) para conservar los cambios.");
        Debug.Log($"[MissionCutsceneSetupTool]\n{resumen}");
        EditorUtility.DisplayDialog("Materiales de misión", resumen.ToString(), "OK");
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
