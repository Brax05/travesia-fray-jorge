using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TravesiaACasa.Menu
{
    /// <summary>
    /// Filtro global de pantalla completa para el Modo Daltónico.
    /// Se conecta dinámicamente con SettingsManager.Instance.ModoDaltonico.
    /// Mantiene el HUD en ScreenSpaceOverlay (siempre al frente de todo) y
    /// aplica la corrección cromática a toda la pantalla mediante un overlay.
    /// </summary>
    public class ColorblindFilter : MonoBehaviour
    {
        private static Material overlayMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            EnsureOverlayInScene();
        }

        public static void EnsureCameraAttached(Camera targetCamera)
        {
            EnsureOverlayInScene();
        }

        private void OnEnable()
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.Changed += Apply;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Apply();
        }

        private void OnDisable()
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.Changed -= Apply;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureOverlayInScene();
        }

        public static void EnsureOverlayInScene()
        {
            Canvas canvas = FindTargetCanvas();
            if (canvas == null) return;

            Transform overlayT = canvas.transform.Find("ColorblindUIOverlay");
            GameObject overlayGO;

            if (overlayT == null)
            {
                overlayGO = new GameObject("ColorblindUIOverlay", typeof(RectTransform), typeof(Image));
                overlayGO.transform.SetParent(canvas.transform, false);

                RectTransform rt = overlayGO.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                Image img = overlayGO.GetComponent<Image>();
                img.raycastTarget = false;
                img.color = Color.white;

                if (overlayMaterial == null)
                {
                    Shader shader = Shader.Find("UI/ColorblindOverlay");
                    if (shader != null && shader.isSupported)
                        overlayMaterial = new Material(shader);
                }

                if (overlayMaterial != null)
                    img.material = overlayMaterial;
            }
            else
            {
                overlayGO = overlayT.gameObject;
            }

            Image overlayImage = overlayGO.GetComponent<Image>();
            if (overlayMaterial == null)
            {
                Shader shader = Shader.Find("UI/ColorblindOverlay");
                if (shader != null && shader.isSupported)
                    overlayMaterial = new Material(shader);
            }

            if (overlayImage != null && overlayMaterial != null && overlayImage.material != overlayMaterial)
                overlayImage.material = overlayMaterial;

            bool active = SettingsManager.Instance != null && SettingsManager.Instance.ModoDaltonico;
            overlayGO.SetActive(active);
            overlayGO.transform.SetAsLastSibling();
        }

        private static Canvas FindTargetCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>();
            Canvas firstRootCanvas = null;

            foreach (Canvas canvas in canvases)
            {
                if (canvas == null || !canvas.isRootCanvas)
                    continue;

                if (canvas.name == "HUD" || canvas.name == "GameHUD")
                    return canvas;

                if (firstRootCanvas == null)
                    firstRootCanvas = canvas;
            }

            return firstRootCanvas != null
                ? firstRootCanvas
                : (canvases.Length > 0 ? canvases[0] : null);
        }

        public void Apply()
        {
            EnsureOverlayInScene();
        }
    }
}
