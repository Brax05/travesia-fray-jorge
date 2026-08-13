using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TravesiaACasa.EditorTools
{
    /// <summary>
    /// Herramientas de Editor para organizar capas de renderizado,
    /// generar prefabs de decoración reutilizables y organizar la estructura de assets y jerarquía.
    /// </summary>
    public static class AssetOrganizerTools
    {
        [MenuItem("Game/Organización/1. Ajustar capas de la escena (Sorting Orders)", false, 1)]
        public static void FixSceneSortingOrders()
        {
            SpriteRenderer[] renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            int countFondo = 0, countCamino = 0, countDecoracion = 0;

            foreach (SpriteRenderer sr in renderers)
            {
                string name = sr.gameObject.name.ToLower();

                if (name.Contains("fondo"))
                {
                    sr.sortingOrder = -10;
                    countFondo++;
                }
                else if (name.Contains("camino"))
                {
                    sr.sortingOrder = -8;
                    countCamino++;
                }
                else if (name.Contains("arbusto") || name.Contains("roca") || name.Contains("chagual") ||
                         name.Contains("olivillo") || name.Contains("arrayan") || name.Contains("nido") ||
                         name.Contains("maravilla"))
                {
                    sr.sortingOrder = 10;
                    countDecoracion++;
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[AssetOrganizerTools] Capas ajustadas: {countFondo} Fondos (-10), {countCamino} Caminos (-8), {countDecoracion} Decoraciones (10).");
        }

        [MenuItem("Game/Organización/2. Generar Prefabs de Decoración en Assets/Prefabs/Decoracion", false, 2)]
        public static void GenerateDecorationPrefabs()
        {
            string folderPath = "Assets/Prefabs/Decoracion";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                AssetDatabase.CreateFolder("Assets/Prefabs", "Decoracion");
            }

            SpriteRenderer[] renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            int created = 0;

            foreach (SpriteRenderer sr in renderers)
            {
                string name = sr.gameObject.name;
                string lower = name.ToLower();

                if (lower.Contains("arbusto") || lower.Contains("roca") || lower.Contains("chagual") ||
                    lower.Contains("olivillo") || lower.Contains("arrayan") || lower.Contains("nido"))
                {
                    string safeName = ObjectNames.NicifyVariableName(name).Replace(" ", "_");
                    string prefabPath = $"{folderPath}/{safeName}.prefab";

                    if (!File.Exists(prefabPath))
                    {
                        PrefabUtility.SaveAsPrefabAssetAndConnect(sr.gameObject, prefabPath, InteractionMode.UserAction);
                        created++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AssetOrganizerTools] Se generaron {created} nuevos prefabs de decoración en '{folderPath}'.");
        }

        [MenuItem("Game/Organización/3. Asegurar estructura de carpetas de Arte limpias", false, 3)]
        public static void EnsureCleanFolderStructure()
        {
            string[] folders = new string[]
            {
                "Assets/Arte/Escenarios",
                "Assets/Arte/Personajes",
                "Assets/Arte/UI",
                "Assets/Arte/Items",
                "Assets/Prefabs/Decoracion",
                "Assets/Prefabs/UI"
            };

            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                    string child = Path.GetFileName(folder);
                    AssetDatabase.CreateFolder(parent, child);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("[AssetOrganizerTools] Estructura de carpetas verificada y creada correctamente.");
        }

        [MenuItem("Game/Organización/4. Organizar Jerarquía de la Escena (Hierarchy)", false, 4)]
        public static void OrganizeSceneHierarchy()
        {
            GameObject paredesRoot = GameObject.Find("ParedesInvisibles");
            if (paredesRoot == null)
                paredesRoot = new GameObject("ParedesInvisibles");

            GameObject decorRoot = GameObject.Find("DecoracionEscena");
            if (decorRoot == null)
                decorRoot = new GameObject("DecoracionEscena");

            GameObject[] rootObjects = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            int paredesMoved = 0, decorMoved = 0;

            foreach (GameObject go in rootObjects)
            {
                if (go == paredesRoot || go == decorRoot) continue;

                string name = go.name.ToLower();

                if (name.Contains("paredinvisible"))
                {
                    Undo.SetTransformParent(go.transform, paredesRoot.transform, "Organizar Jerarquia");
                    paredesMoved++;
                }
                else if (name.Contains("roca") || name.Contains("arbusto") || name.Contains("camino"))
                {
                    if (go.name != "Main Camera" && go.name != "Jugador_Yal" && go.name != "GraphManager" &&
                        go.name != "EventSystem" && go.name != "HUD" && go.name != "GameHudController" &&
                        go.name != "Rooms" && go.name != "Exits")
                    {
                        Undo.SetTransformParent(go.transform, decorRoot.transform, "Organizar Jerarquia");
                        decorMoved++;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[AssetOrganizerTools] Jerarquía organizada: {paredesMoved} Paredes en 'ParedesInvisibles', {decorMoved} Decoraciones en 'DecoracionEscena'.");
        }
    }
}
