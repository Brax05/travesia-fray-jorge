using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TravesiaACasa.Rooms
{
    /// <summary>
    /// Cartel simple de estado de la misión.
    /// Al presionar el botón MisionLetrero en el HUD, abre este cartel
    /// con la lista de materiales recolectados (ej: Madera, Plumas, Copihue).
    /// Se cierra al hacer clic o tocar en cualquier parte de la pantalla.
    /// </summary>
    public class MissionStatusPopup : MonoBehaviour
    {
        public static MissionStatusPopup Instance { get; private set; }

        private GameObject popupPanel;
        private Text statusText;
        private bool isShowing;
        private float openTime;
        private const float InputCooldown = 0.25f;

        [System.Serializable]
        public class MaterialRequirement
        {
            public string key;
            public string name;
            public int requiredAmount;

            public MaterialRequirement(string key, string name, int requiredAmount)
            {
                this.key = key;
                this.name = name;
                this.requiredAmount = requiredAmount;
            }
        }

        private readonly List<MaterialRequirement> requirements = new List<MaterialRequirement>
        {
            new MaterialRequirement("madera_maiten", "Madera de maitén", 1),
            new MaterialRequirement("pegamento_larvas", "Pegamento de larvas", 1),
            new MaterialRequirement("copihue", "Un copihue", 1),
            new MaterialRequirement("pluma_alicanto", "Plumas de alicanto", 2)
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            EnsureInstance();
        }

        public static void EnsureInstance()
        {
            if (Instance != null) return;
            Canvas canvas = TopLeftGameplayHud.FindGameplayCanvas();
            if (canvas != null && canvas.GetComponent<MissionStatusPopup>() == null)
            {
                canvas.gameObject.AddComponent<MissionStatusPopup>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            BuildPopupUI();
            BindMisionLetreroButton();
        }

        public void BindMisionLetreroButton()
        {
            Canvas canvas = TopLeftGameplayHud.FindGameplayCanvas();
            if (canvas == null) return;

            Transform letreroT = TopLeftGameplayHud.FindDescendant(canvas.transform, "MisionLetrero");
            if (letreroT != null)
            {
                Button btn = letreroT.GetComponent<Button>();
                if (btn == null) btn = letreroT.gameObject.AddComponent<Button>();

                btn.onClick.RemoveListener(TogglePopup);
                btn.onClick.AddListener(TogglePopup);
            }
        }

        public void TogglePopup()
        {
            if (isShowing)
                Hide();
            else
                Show();
        }

        public void Show()
        {
            if (popupPanel == null) BuildPopupUI();
            RefreshStatusText();

            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
                popupPanel.transform.SetAsLastSibling();
            }
            isShowing = true;
            openTime = Time.unscaledTime;
        }

        public void Hide()
        {
            if (popupPanel != null)
                popupPanel.SetActive(false);
            isShowing = false;
        }

        private void Update()
        {
            if (!isShowing) return;

            // Cooldown inicial para evitar que el mismo clic que abre el cartel lo cierre de inmediato
            if (Time.unscaledTime - openTime < InputCooldown) return;

            if (WasAnyInputPressed())
            {
                Hide();
            }
        }

        private static bool WasAnyInputPressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
                return true;

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                return true;

            Touchscreen touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
                return true;

            return false;
        }

        private void RefreshStatusText()
        {
            if (statusText == null) return;

            string content = "<b><size=32>MATERIALES DE LA MISIÓN</size></b>\n\n";

            int totalCollected = 0;
            int totalRequired = 0;

            foreach (var req in requirements)
            {
                int current = InventoryManager.Instance != null ? InventoryManager.Instance.GetCount(req.key) : 0;
                totalCollected += Mathf.Min(current, req.requiredAmount);
                totalRequired += req.requiredAmount;

                bool completed = current >= req.requiredAmount;
                string checkMark = completed ? "<color=#228B22>[✓] Conseguido</color>" : "<color=#D9534F>[ ] Pendiente</color>";

                content += $"{checkMark}  <b>{req.name}</b> ({current}/{req.requiredAmount})\n";
            }

            content += $"\n<size=22>Progreso total: {totalCollected}/{totalRequired}</size>";
            content += "\n\n<color=#555555><size=18>(Toca cualquier parte de la pantalla para cerrar)</size></color>";

            statusText.text = content;
        }

        private void BuildPopupUI()
        {
            Canvas canvas = TopLeftGameplayHud.FindGameplayCanvas();
            if (canvas == null) return;

            Transform existing = canvas.transform.Find("MissionStatusPopupPanel");
            if (existing != null)
            {
                popupPanel = existing.gameObject;
                statusText = popupPanel.GetComponentInChildren<Text>();
                popupPanel.SetActive(false);
                return;
            }

            // Panel contenedor principal
            popupPanel = new GameObject("MissionStatusPopupPanel", typeof(RectTransform), typeof(Image));
            popupPanel.transform.SetParent(canvas.transform, false);

            RectTransform rt = popupPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(640f, 400f);
            rt.anchoredPosition = Vector2.zero;

            Image bg = popupPanel.GetComponent<Image>();
            bg.color = new Color(0.96f, 0.93f, 0.84f, 0.97f);

            Outline bgOutline = popupPanel.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.35f, 0.22f, 0.12f, 0.9f);
            bgOutline.effectDistance = new Vector2(4f, -4f);

            // Texto del cartel
            GameObject textGO = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(popupPanel.transform, false);

            RectTransform textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(30f, 20f);
            textRt.offsetMax = new Vector2(-30f, -20f);

            statusText = textGO.GetComponent<Text>();
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 22);
            statusText.fontSize = 22;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = new Color(0.2f, 0.15f, 0.1f, 1f);
            statusText.supportRichText = true;

            popupPanel.SetActive(false);
        }
    }
}
