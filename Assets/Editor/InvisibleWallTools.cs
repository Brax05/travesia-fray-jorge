using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TravesiaACasa.Rooms;

namespace TravesiaACasa.Rooms.Editor
{
    /// <summary>
    /// Herramienta para crear Paredes Invisibles 2D fácilmente en el Editor.
    /// </summary>
    public static class InvisibleWallTools
    {
        [MenuItem("Game/Crear Pared Invisible 2D", false, 10)]
        [MenuItem("GameObject/2D Object/Pared Invisible 2D", false, 20)]
        public static void CreateInvisibleWall()
        {
            GameObject wall = new GameObject("ParedInvisible", typeof(BoxCollider2D), typeof(InvisibleWall2D));

            if (Selection.activeTransform != null)
            {
                wall.transform.SetParent(Selection.activeTransform, false);
            }
            else
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 camPos = cam.transform.position;
                    wall.transform.position = new Vector3(camPos.x, camPos.y, 0f);
                }
            }

            BoxCollider2D collider = wall.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 5f); // Tamaño por defecto (línea vertical)
            collider.isTrigger = false;

            Undo.RegisterCreatedObjectUndo(wall, "Crear Pared Invisible 2D");
            Selection.activeGameObject = wall;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[InvisibleWallTools] Pared invisible creada correctamente.");
        }
    }
}
