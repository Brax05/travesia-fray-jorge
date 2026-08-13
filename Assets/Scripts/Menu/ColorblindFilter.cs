using UnityEngine;

namespace TravesiaACasa.Menu
{
    /// <summary>
    /// Filtro global de pantalla completa para el Modo Daltónico.
    /// Se conecta automáticamente con SettingsManager.Instance.ModoDaltonico.
    /// Aplica una matriz de transformación de color optimizada para diferenciar
    /// rojos y verdes mediante corrección cromática y contraste asistido.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ColorblindFilter : MonoBehaviour
    {
        private static Shader filterShader;
        private Material filterMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttachToCamera()
        {
            EnsureCameraAttached(Camera.main);
        }

        public static void EnsureCameraAttached(Camera targetCamera)
        {
            if (targetCamera != null && targetCamera.GetComponent<ColorblindFilter>() == null)
            {
                targetCamera.gameObject.AddComponent<ColorblindFilter>();
            }
        }

        private void Awake()
        {
            EnsureMaterial();
            EnsureCanvasesUseCamera();
        }

        private void Start()
        {
            EnsureCanvasesUseCamera();
        }

        private void OnEnable()
        {
            EnsureCanvasesUseCamera();
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.Changed += OnSettingsChanged;
        }

        public void EnsureCanvasesUseCamera()
        {
            Camera cameraToUse = GetComponent<Camera>();
            if (cameraToUse == null) cameraToUse = Camera.main;
            if (cameraToUse == null) return;

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cameraToUse;
                    canvas.planeDistance = 1f;
                }
            }
        }

        private void OnDisable()
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.Changed -= OnSettingsChanged;
        }

        private void OnSettingsChanged()
        {
            // El estado se evalúa dinámicamente en OnRenderImage
        }

        private void EnsureMaterial()
        {
            if (filterMaterial != null) return;

            if (filterShader == null)
                filterShader = Shader.Find("Hidden/ColorblindFilter");

            if (filterShader != null && filterShader.isSupported)
            {
                filterMaterial = new Material(filterShader);
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            bool active = SettingsManager.Instance != null && SettingsManager.Instance.ModoDaltonico;

            EnsureMaterial();

            if (active && filterMaterial != null)
            {
                Graphics.Blit(source, destination, filterMaterial);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }

        private void OnDestroy()
        {
            if (filterMaterial != null)
            {
                DestroyImmediate(filterMaterial);
            }
        }
    }
}
