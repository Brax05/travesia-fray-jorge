using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TravesiaACasa.Rooms
{
    /// <summary>
    /// Cartel simple e intuitivo de estado de la misión para niños de 5 a 12 años.
    /// Al presionar MisionLetrero en el HUD, muestra los materiales con sus íconos
    /// grandes y coloridos, indicando si ya fueron conseguidos (✓) o faltan (○).
    /// Se cierra al tocar en cualquier parte de la pantalla.
    /// </summary>
    public class MissionStatusPopup : MonoBehaviour
    {
        public static MissionStatusPopup Instance { get; private set; }

        private GameObject popupPanel;
        private Text titleText;
        private Text footerText;
        private GameObject noMissionGO;
        private Text noMissionText;
        private GameObject rowsContainer;
        private readonly List<ItemRowUI> itemRows = new List<ItemRowUI>();
        private MissionIntroCutscene missionIntro;

        private bool isShowing;
        private float openTime;
        private const float InputCooldown = 0.25f;

        public class ItemRowUI
        {
            public GameObject rowGO;
            public Image iconImage;
            public Text itemText;
        }

        public class MaterialRequirement
        {
            public string key;
            public string name;
            public int requiredAmount;
            public string spritePath;

            public MaterialRequirement(string key, string name, int requiredAmount, string spritePath)
            {
                this.key = key;
                this.name = name;
                this.requiredAmount = requiredAmount;
                this.spritePath = spritePath;
            }
        }

        private readonly List<MaterialRequirement> requirements = new List<MaterialRequirement>
        {
            new MaterialRequirement("madera_maiten", "Madera de maitén", 1, "Assets/Arte/juego/Items/madera.png"),
            new MaterialRequirement("pegamento_larvas", "Pegamento de larvas", 1, "Assets/Arte/juego/Items/pegamento.png"),
            new MaterialRequirement("copihue", "Un copihue", 1, "Assets/Arte/juego/Items/copi.png"),
            new MaterialRequirement("pluma_alicanto", "Plumas de alicanto", 2, "Assets/Arte/juego/Items/plumas.png")
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

        private void OnEnable()
        {
            BindMisionLetreroButton();
        }

        private void Start()
        {
            missionIntro = FindAnyObjectByType<MissionIntroCutscene>();
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
                Image img = letreroT.GetComponent<Image>();
                if (img != null) img.raycastTarget = true;

                Button btn = letreroT.GetComponent<Button>();
                if (btn == null) btn = letreroT.gameObject.AddComponent<Button>();
                if (btn.targetGraphic == null) btn.targetGraphic = img;

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
            RefreshStatusVisuals();

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

        private void RefreshStatusVisuals()
        {
            if (popupPanel == null) return;

            bool accepted = MissionIntroCutscene.IsMissionAccepted;

            if (!accepted)
            {
                if (noMissionGO != null) noMissionGO.SetActive(true);
                if (rowsContainer != null) rowsContainer.SetActive(false);
                if (titleText != null) titleText.text = "<b><size=38>¡SIN MISIÓN!</size></b>";
                return;
            }

            if (noMissionGO != null) noMissionGO.SetActive(false);
            if (rowsContainer != null) rowsContainer.SetActive(true);
            if (titleText != null) titleText.text = "<b><size=38>TUS MATERIALES</size></b>";

            for (int i = 0; i < requirements.Count && i < itemRows.Count; i++)
            {
                var req = requirements[i];
                var row = itemRows[i];

                int current = InventoryManager.Instance != null ? InventoryManager.Instance.GetCount(req.key) : 0;
                bool completed = current >= req.requiredAmount;

                string iconSymbol = completed ? "<color=#1B8A22><b>✓</b></color>" : "<color=#D93829><b>○</b></color>";
                string statusMsg = completed ? "<color=#1B8A22>¡Conseguido!</color>" : $"({current} de {req.requiredAmount})";

                row.itemText.text = $"<size=28>{iconSymbol} <b>{req.name}</b> {statusMsg}</size>";
            }
        }

        private void BuildPopupUI()
        {
            Canvas canvas = TopLeftGameplayHud.FindGameplayCanvas();
            if (canvas == null) return;

            Transform existing = canvas.transform.Find("MissionStatusPopupPanel");
            if (existing != null)
            {
                popupPanel = existing.gameObject;
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
            rt.sizeDelta = new Vector2(800f, 540f);
            rt.anchoredPosition = Vector2.zero;

            Image bg = popupPanel.GetComponent<Image>();
            bg.color = new Color(0.96f, 0.93f, 0.84f, 0.98f);

            Outline bgOutline = popupPanel.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.35f, 0.22f, 0.12f, 0.95f);
            bgOutline.effectDistance = new Vector2(5f, -5f);

            Font mainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 28);

            // Título superior
            GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleGO.transform.SetParent(popupPanel.transform, false);
            RectTransform titleRt = titleGO.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 70f);
            titleRt.anchoredPosition = new Vector2(0f, -15f);

            titleText = titleGO.GetComponent<Text>();
            titleText.font = mainFont;
            titleText.fontSize = 38;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.2f, 0.12f, 0.06f, 1f);
            titleText.supportRichText = true;
            titleText.raycastTarget = false;

            // Mensaje Sin Misión
            noMissionGO = new GameObject("NoMissionGroup", typeof(RectTransform));
            noMissionGO.transform.SetParent(popupPanel.transform, false);
            RectTransform noMissionRt = noMissionGO.GetComponent<RectTransform>();
            noMissionRt.anchorMin = Vector2.zero;
            noMissionRt.anchorMax = Vector2.one;
            noMissionRt.offsetMin = new Vector2(30f, 60f);
            noMissionRt.offsetMax = new Vector2(-30f, -80f);

            GameObject noMsgTextGO = new GameObject("NoMissionText", typeof(RectTransform), typeof(Text));
            noMsgTextGO.transform.SetParent(noMissionGO.transform, false);
            RectTransform noMsgTextRt = noMsgTextGO.GetComponent<RectTransform>();
            noMsgTextRt.anchorMin = Vector2.zero;
            noMsgTextRt.anchorMax = Vector2.one;
            noMsgTextRt.offsetMin = Vector2.zero;
            noMsgTextRt.offsetMax = Vector2.zero;

            noMissionText = noMsgTextGO.GetComponent<Text>();
            noMissionText.font = mainFont;
            noMissionText.fontSize = 28;
            noMissionText.alignment = TextAnchor.MiddleCenter;
            noMissionText.color = new Color(0.25f, 0.18f, 0.12f, 1f);
            noMissionText.supportRichText = true;
            noMissionText.text = "<size=28>Busca al pajarito <b>Carpinterito</b>\npara recibir tu primera misión.</size>";
            noMissionText.raycastTarget = false;

            // Contenedor de filas de ítems
            rowsContainer = new GameObject("RowsContainer", typeof(RectTransform));
            rowsContainer.transform.SetParent(popupPanel.transform, false);
            RectTransform rowsRt = rowsContainer.GetComponent<RectTransform>();
            rowsRt.anchorMin = new Vector2(0f, 0f);
            rowsRt.anchorMax = new Vector2(1f, 1f);
            rowsRt.offsetMin = new Vector2(40f, 60f);
            rowsRt.offsetMax = new Vector2(-40f, -85f);

            itemRows.Clear();
            float startY = -10f;
            float rowHeight = 85f;

            for (int i = 0; i < requirements.Count; i++)
            {
                var req = requirements[i];

                GameObject rowGO = new GameObject($"Row_{i}", typeof(RectTransform));
                rowGO.transform.SetParent(rowsContainer.transform, false);
                RectTransform rowRt = rowGO.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0f, 1f);
                rowRt.anchorMax = new Vector2(1f, 1f);
                rowRt.pivot = new Vector2(0.5f, 1f);
                rowRt.sizeDelta = new Vector2(0f, rowHeight);
                rowRt.anchoredPosition = new Vector2(0f, startY - (i * rowHeight));

                // Ícono del objeto
                GameObject iconGO = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(rowGO.transform, false);
                RectTransform iconRt = iconGO.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0f, 0.5f);
                iconRt.sizeDelta = new Vector2(68f, 68f);
                iconRt.anchoredPosition = new Vector2(10f, 0f);

                Image iconImg = iconGO.GetComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;

                Sprite itemSprite = LoadSprite(req);
                if (itemSprite != null)
                {
                    iconImg.sprite = itemSprite;
                    iconImg.color = Color.white;
                }
                else
                {
                    iconImg.color = new Color(0.85f, 0.75f, 0.6f, 0.5f); // Color fallback si no hay sprite
                }

                // Texto del objeto
                GameObject itemTextGO = new GameObject("ItemText", typeof(RectTransform), typeof(Text));
                itemTextGO.transform.SetParent(rowGO.transform, false);
                RectTransform textRt = itemTextGO.GetComponent<RectTransform>();
                textRt.anchorMin = new Vector2(0f, 0f);
                textRt.anchorMax = new Vector2(1f, 1f);
                textRt.offsetMin = new Vector2(95f, 0f);
                textRt.offsetMax = new Vector2(0f, 0f);

                Text t = itemTextGO.GetComponent<Text>();
                t.font = mainFont;
                t.fontSize = 28;
                t.alignment = TextAnchor.MiddleLeft;
                t.color = new Color(0.18f, 0.12f, 0.08f, 1f);
                t.supportRichText = true;
                t.raycastTarget = false;

                itemRows.Add(new ItemRowUI
                {
                    rowGO = rowGO,
                    iconImage = iconImg,
                    itemText = t
                });
            }

            // Pie de página
            GameObject footerGO = new GameObject("FooterText", typeof(RectTransform), typeof(Text));
            footerGO.transform.SetParent(popupPanel.transform, false);
            RectTransform footerRt = footerGO.GetComponent<RectTransform>();
            footerRt.anchorMin = new Vector2(0f, 0f);
            footerRt.anchorMax = new Vector2(1f, 0f);
            footerRt.pivot = new Vector2(0.5f, 0f);
            footerRt.sizeDelta = new Vector2(0f, 50f);
            footerRt.anchoredPosition = new Vector2(0f, 10f);

            footerText = footerGO.GetComponent<Text>();
            footerText.font = mainFont;
            footerText.fontSize = 20;
            footerText.alignment = TextAnchor.MiddleCenter;
            footerText.color = new Color(0.45f, 0.4f, 0.35f, 1f);
            footerText.text = "(Toca la pantalla para cerrar)";
            footerText.raycastTarget = false;

            popupPanel.SetActive(false);
        }

        private Sprite LoadSprite(MaterialRequirement requirement)
        {
            if (missionIntro == null)
                missionIntro = FindAnyObjectByType<MissionIntroCutscene>();

            Sprite serializedSprite = missionIntro != null
                ? missionIntro.GetMaterialSprite(requirement.key)
                : null;
            if (serializedSprite != null)
                return serializedSprite;

#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(requirement.spritePath);
#else
            return null;
#endif
        }
    }
}
