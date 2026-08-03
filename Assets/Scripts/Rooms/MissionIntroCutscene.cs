using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TravesiaACasa.Rooms
{
    /// <summary>
    /// Cutscene de presentación de la misión en la Room 3. Cuando el
    /// jugador cierra el primer diálogo con el carpinterito (MissionBird),
    /// el AveNegra entra caminando desde el borde derecho, se detiene al
    /// lado del carpinterito, muestra la burbuja de idea y habla por el
    /// cuadro de diálogo. Un toque después aparece el letrero de Misión
    /// (LetreroFondo + TituloMision + lista de materiales, construido por
    /// código en el Canvas del HUD, igual que TopLeftGameplayHud). Al
    /// cerrarlo, el letrero "MISIÓN" del HUD superior izquierdo queda
    /// parpadeando para avisar que hay una misión activa.
    /// </summary>
    public class MissionIntroCutscene : MonoBehaviour
    {
        [Header("Actores (en la escena)")]
        [Tooltip("GameObject del AveNegra (inactivo al inicio, hijo de Room_3).")]
        [SerializeField] private GameObject aveNegra;

        [Tooltip("Burbuja de idea (hija del AveNegra, inactiva al inicio).")]
        [SerializeField] private GameObject ideaBubble;

        [Tooltip("Posición local (en Room_3) donde se detiene el AveNegra, al lado del carpinterito.")]
        [SerializeField] private Vector2 walkTargetLocalPos = new Vector2(1.1f, -0.4f);

        [SerializeField] private float walkSpeed = 2.2f;

        [Header("Diálogo del AveNegra")]
        [SerializeField] private string speakerName = "Carpinterito";

        [TextArea]
        [SerializeField] private string dialogueLine =
            "¡Piip! ¡Se me ocurrió una idea para que vuelvas a casa! " +
            "Pero vamos a necesitar materiales... ¡mira la lista!";

        [Header("Letrero de misión")]
        [SerializeField] private Sprite letreroFondoSprite;
        [SerializeField] private Sprite tituloMisionSprite;

        [TextArea]
        [SerializeField] private string missionText =
            "Recolecta los materiales que\nCarpintero te pidió:\n" +
            "- Madera de maitén\n- Pegamento de larvas\n- Un copihue\n- Dos plumas de alicanto";

        [Header("Iconos de materiales (fila inferior del letrero)")]
        [SerializeField] private Sprite maderaSprite;
        [SerializeField] private Sprite pegamentoSprite;
        [SerializeField] private Sprite copihueSprite;
        [SerializeField] private Sprite plumasSprite;

        private MissionBird missionBird;
        private GameHudController hud;
        private Canvas hudCanvas;
        private GameObject missionPanel;
        private bool played;

        private void Start()
        {
            missionBird = GetComponent<MissionBird>();
            hud = FindFirstObjectByType<GameHudController>();
            hudCanvas = TopLeftGameplayHud.FindGameplayCanvas();

            if (hudCanvas != null)
                missionPanel = BuildMissionPanel(hudCanvas.transform);

            if (missionBird != null)
                missionBird.DialogueClosed += OnMissionBirdDialogueClosed;
        }

        private void OnDestroy()
        {
            if (missionBird != null)
                missionBird.DialogueClosed -= OnMissionBirdDialogueClosed;
        }

        private void OnMissionBirdDialogueClosed()
        {
            if (played || aveNegra == null) return;
            played = true;
            StartCoroutine(RunCutscene());
        }

        private IEnumerator RunCutscene()
        {
            // Sin controles ni re-aperturas del diálogo durante la cutscene.
            if (missionBird != null) missionBird.enabled = false;
            SetHudControlsVisible(false);

            yield return new WaitForSeconds(0.35f);

            // Entra el AveNegra caminando hasta el lado del carpinterito.
            aveNegra.SetActive(true);
            SpriteRenderer aveRenderer = aveNegra.GetComponent<SpriteRenderer>();
            Transform aveTransform = aveNegra.transform;
            Vector3 target = new Vector3(walkTargetLocalPos.x, walkTargetLocalPos.y, aveTransform.localPosition.z);

            // Se compara solo X: el bob de caminata mueve Y y nunca dejaría
            // que la distancia 2D baje del umbral.
            float baseY = walkTargetLocalPos.y;
            while (Mathf.Abs(aveTransform.localPosition.x - target.x) > 0.05f)
            {
                Vector3 current = aveTransform.localPosition;
                Vector3 next = Vector3.MoveTowards(current, target, walkSpeed * Time.deltaTime);
                // Salto corto de caminata (el sprite mira a la izquierda por defecto).
                next.y = baseY + Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.07f;
                if (aveRenderer != null) aveRenderer.flipX = target.x > current.x;
                aveTransform.localPosition = next;
                yield return null;
            }
            aveTransform.localPosition = target;

            yield return new WaitForSeconds(0.3f);

            // Se le ocurre la idea y se pone a hablar.
            if (ideaBubble != null) ideaBubble.SetActive(true);
            GameObject dialoguePanel = OpenDialogueAsAveNegra(aveRenderer != null ? aveRenderer.sprite : null,
                out Text dialogueText, out string originalLine,
                out Image avatarImage, out Sprite originalAvatar,
                out Text nameText, out string originalName);

            yield return WaitForTap();

            if (ideaBubble != null) ideaBubble.SetActive(false);
            if (dialoguePanel != null)
            {
                if (dialogueText != null) dialogueText.text = originalLine;
                if (avatarImage != null) avatarImage.sprite = originalAvatar;
                if (nameText != null) nameText.text = originalName;
                dialoguePanel.SetActive(false);
            }

            // Letrero grande de Misión.
            if (missionPanel != null)
            {
                missionPanel.SetActive(true);
                yield return WaitForTap();
                missionPanel.SetActive(false);
            }

            // Misión aceptada: vuelve el control y el letrero MISIÓN parpadea.
            SetHudControlsVisible(true);
            if (missionBird != null)
            {
                missionBird.SetMission(false);
                missionBird.enabled = true;
            }
            StartCoroutine(BlinkMissionIndicator());
        }

        private GameObject OpenDialogueAsAveNegra(Sprite aveSprite,
            out Text dialogueText, out string originalLine,
            out Image avatarImage, out Sprite originalAvatar,
            out Text nameText, out string originalName)
        {
            dialogueText = null;
            originalLine = null;
            avatarImage = null;
            originalAvatar = null;
            nameText = null;
            originalName = null;

            if (hudCanvas == null) return null;
            Transform panel = TopLeftGameplayHud.FindDescendant(hudCanvas.transform, "DialoguePanelCanastero");
            if (panel == null) return null;

            Transform textTransform = TopLeftGameplayHud.FindDescendant(panel, "DialogueText");
            if (textTransform != null && textTransform.TryGetComponent(out dialogueText))
            {
                originalLine = dialogueText.text;
                dialogueText.text = dialogueLine;
            }

            Transform avatarTransform = TopLeftGameplayHud.FindDescendant(panel, "Avatar");
            if (avatarTransform != null && avatarTransform.TryGetComponent(out avatarImage))
            {
                originalAvatar = avatarImage.sprite;
                if (aveSprite != null)
                {
                    avatarImage.sprite = aveSprite;
                    avatarImage.preserveAspect = true;
                }
            }

            Transform nameTransform = TopLeftGameplayHud.FindDescendant(panel, "CharacterNameText");
            if (nameTransform != null && nameTransform.TryGetComponent(out nameText))
            {
                originalName = nameText.text;
                nameText.text = speakerName;
            }

            panel.gameObject.SetActive(true);
            return panel.gameObject;
        }

        private void SetHudControlsVisible(bool visible)
        {
            hud?.SetGameplayControlsVisible(visible);
            if (hudCanvas == null) return;
            Transform interact = TopLeftGameplayHud.FindDescendant(hudCanvas.transform, "InteractuarBtn");
            if (interact != null) interact.gameObject.SetActive(visible);
        }

        private IEnumerator WaitForTap()
        {
            // Un frame de gracia para no consumir el toque anterior.
            yield return null;
            while (!TapStartedThisFrame())
                yield return null;
        }

        private static bool TapStartedThisFrame()
        {
            Touchscreen touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
                return true;

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                return true;

            Keyboard kb = Keyboard.current;
            return kb != null && kb.eKey.wasPressedThisFrame;
        }

        private IEnumerator BlinkMissionIndicator()
        {
            if (hudCanvas == null) yield break;
            Transform letrero = TopLeftGameplayHud.FindDescendant(hudCanvas.transform, "MisionLetrero");
            if (letrero == null || !letrero.TryGetComponent(out Image image)) yield break;

            Color color = image.color;
            while (true)
            {
                color.a = Mathf.Lerp(0.25f, 1f, Mathf.PingPong(Time.unscaledTime * 2.2f, 1f));
                image.color = color;
                yield return null;
            }
        }

        private GameObject BuildMissionPanel(Transform canvas)
        {
            GameObject root = new GameObject("MissionBriefPanel", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image letrero = CreateImage(rootRect, "Letrero", letreroFondoSprite, new Vector2(0f, -10f), new Vector2(1180f, 640f));
            letrero.preserveAspect = false;

            CreateImage(rootRect, "Titulo", tituloMisionSprite, new Vector2(0f, 245f), new Vector2(500f, 170f));

            GameObject textGo = new GameObject("MisionTexto", typeof(RectTransform));
            textGo.transform.SetParent(rootRect, false);
            RectTransform textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0f, -30f);
            textRect.sizeDelta = new Vector2(960f, 300f);
            Text text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 42;
            text.alignment = TextAnchor.UpperLeft;
            text.color = new Color(0.13f, 0.1f, 0.08f);
            text.raycastTarget = false;
            text.text = missionText;

            Sprite[] icons = { maderaSprite, pegamentoSprite, copihueSprite, plumasSprite };
            string[] iconNames = { "Madera", "Pegamento", "Copihue", "Plumas" };
            for (int i = 0; i < icons.Length; i++)
            {
                float x = -300f + i * 200f;
                CreateImage(rootRect, iconNames[i], icons[i], new Vector2(x, -250f), new Vector2(110f, 110f));
            }

            root.SetActive(false);
            return root;
        }

        private static Image CreateImage(RectTransform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }
    }
}
