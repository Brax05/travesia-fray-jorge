/**
 * Archivo: MissionStatusTools.cs
 * Proposito: Crear y configurar desde el editor el popup de estado de mision.
 * Responsabilidades: Buscar el HUD de juego, agregar MissionStatusPopup y guardar los cambios necesarios en la escena activa.
 *
 */
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TravesiaACasa.Rooms;

namespace TravesiaACasa.Rooms.Editor
{
    /// <summary>
    /// Herramienta de Editor para configurar el cartel de estado de la misión.
    /// Genera la herramienta en el menú "Game > Misión > Configurar cartel de estado de misión".
    /// </summary>
    public static class MissionStatusTools
    {
        [MenuItem("Game/Misión/Configurar cartel de estado de misión", false, 1)]
        public static void SetupMissionStatusPopup()
        {
            Canvas canvas = TopLeftGameplayHud.FindGameplayCanvas();
            if (canvas == null)
            {
                Debug.LogError("[MissionStatusTools] No se encontró el Canvas del HUD en la escena activa.");
                return;
            }

            MissionStatusPopup popup = canvas.GetComponent<MissionStatusPopup>();
            if (popup == null)
            {
                popup = canvas.gameObject.AddComponent<MissionStatusPopup>();
            }

            popup.BindMisionLetreroButton();
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

            Debug.Log("[MissionStatusTools] Cartel de estado de misión configurado correctamente en el HUD Canvas.");
        }
    }
}
