using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TravesiaACasa
{
    /// <summary>
    /// Fundido de pantalla compartido por los cambios de escena y de room.
    /// Se crea bajo demanda y sobrevive a LoadSceneAsync para que nunca se
    /// llegue a mostrar un frame a medio cargar.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenTransition : MonoBehaviour
    {
        private const string ObjectName = "ScreenTransition";
        private const int OverlaySortingOrder = short.MaxValue;

        private static ScreenTransition instance;

        private Canvas overlayCanvas;
        private CanvasGroup overlayGroup;
        private bool isBusy;

        public static bool IsBusy => instance != null && instance.isBusy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            BuildOverlay();
            SetOverlayVisible(false);
        }

        /// <summary>
        /// Ejecuta una operacion justo cuando la pantalla esta cubierta.
        /// Devuelve false si ya hay otra transicion en curso.
        /// </summary>
        public static bool TryFadeThrough(
            Action onCovered,
            Action onComplete = null,
            float fadeOutDuration = 0.12f,
            float coveredDuration = 0.02f,
            float fadeInDuration = 0.18f)
        {
            ScreenTransition transition = GetOrCreate();
            if (transition.isBusy)
                return false;

            transition.StartCoroutine(transition.FadeThroughRoutine(
                onCovered,
                onComplete,
                fadeOutDuration,
                coveredDuration,
                fadeInDuration));
            return true;
        }

        /// <summary>
        /// Carga una escena de forma asincrona entre dos fundidos. Las
        /// duraciones usan tiempo no escalado, por lo que no se bloquea si
        /// el juego estaba pausado.
        /// </summary>
        public static bool TryLoadScene(
            string sceneName,
            float fadeOutDuration = 0.22f,
            float fadeInDuration = 0.25f)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[ScreenTransition] El nombre de escena esta vacio.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[ScreenTransition] La escena '{sceneName}' no esta incluida en el build.");
                return false;
            }

            ScreenTransition transition = GetOrCreate();
            if (transition.isBusy)
                return false;

            transition.StartCoroutine(transition.LoadSceneRoutine(
                sceneName,
                fadeOutDuration,
                fadeInDuration));
            return true;
        }

        private static ScreenTransition GetOrCreate()
        {
            if (instance != null)
                return instance;

            GameObject root = new GameObject(ObjectName, typeof(RectTransform));
            return root.AddComponent<ScreenTransition>();
        }

        private IEnumerator FadeThroughRoutine(
            Action onCovered,
            Action onComplete,
            float fadeOutDuration,
            float coveredDuration,
            float fadeInDuration)
        {
            BeginTransition();
            yield return FadeTo(1f, fadeOutDuration);

            InvokeSafely(onCovered);

            // Siempre se deja al menos un frame completamente cubierto. Asi
            // la camara y la fisica adoptan la nueva room antes de mostrarla.
            yield return null;
            if (coveredDuration > 0f)
                yield return WaitUnscaled(coveredDuration);

            yield return FadeTo(0f, fadeInDuration);
            EndTransition();
            InvokeSafely(onComplete);
        }

        private IEnumerator LoadSceneRoutine(
            string sceneName,
            float fadeOutDuration,
            float fadeInDuration)
        {
            BeginTransition();
            yield return FadeTo(1f, fadeOutDuration);

            AsyncOperation loadOperation = null;
            try
            {
                loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            if (loadOperation == null)
            {
                Debug.LogError($"[ScreenTransition] No se pudo iniciar la carga de '{sceneName}'.");
                yield return FadeTo(0f, fadeInDuration);
                EndTransition();
                yield break;
            }

            while (!loadOperation.isDone)
                yield return null;

            // Awake/Start de la escena nueva deben terminar antes del fundido.
            yield return null;
            yield return FadeTo(0f, fadeInDuration);
            EndTransition();
        }

        private void BeginTransition()
        {
            isBusy = true;
            overlayCanvas.enabled = true;
            overlayGroup.blocksRaycasts = true;
            overlayGroup.interactable = true;
        }

        private void EndTransition()
        {
            isBusy = false;
            SetOverlayVisible(false);
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            float startAlpha = overlayGroup.alpha;
            duration = Mathf.Max(0f, duration);

            if (duration <= 0f)
            {
                overlayGroup.alpha = targetAlpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                overlayGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, SmoothStep(progress));
                yield return null;
            }

            overlayGroup.alpha = targetAlpha;
        }

        private static IEnumerator WaitUnscaled(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static float SmoothStep(float value)
            => value * value * (3f - 2f * value);

        private static void InvokeSafely(Action callback)
        {
            if (callback == null)
                return;

            try
            {
                callback.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void BuildOverlay()
        {
            overlayCanvas = gameObject.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = OverlaySortingOrder;

            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
            overlayGroup = gameObject.AddComponent<CanvasGroup>();

            GameObject imageObject = new GameObject("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(transform, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = imageObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;
        }

        private void SetOverlayVisible(bool visible)
        {
            overlayGroup.alpha = visible ? 1f : 0f;
            overlayGroup.blocksRaycasts = visible;
            overlayGroup.interactable = visible;
            overlayCanvas.enabled = visible;
        }
    }
}
