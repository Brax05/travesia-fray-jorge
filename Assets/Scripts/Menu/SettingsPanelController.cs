using UnityEngine;
using UnityEngine.UI;

namespace TravesiaACasa.Menu
{
    /// <summary>
    /// Conecta los 4 sliders y los 2 botones-interruptor del panel de
    /// Configuración (Arte/configuracion/) con SettingsManager.
    /// Los interruptores no usan el componente Toggle de Unity: son un
    /// Button que alterna manualmente entre el sprite "boton marron"
    /// (apagado) y "boton naranjo" (encendido), que es el par de assets
    /// que entregó la diseñadora para este control.
    /// </summary>
    public class SettingsPanelController : MonoBehaviour
    {
        public const float MenuButtonWidth = 320f;
        public const float MenuButtonRightInset = 70f;
        public const float MenuButtonBottomInset = 40f;

        [Header("Sliders (Sonido + Brillo)")]
        [SerializeField] private Slider ambienteSlider;
        [SerializeField] private Slider personajesSlider;
        [SerializeField] private Slider cinematicaSlider;
        [SerializeField] private Slider brilloSlider;

        [Header("Interruptores")]
        [SerializeField] private Button modoDaltonicoButton;
        [SerializeField] private Image modoDaltonicoImage;
        [SerializeField] private Button vibracionButton;
        [SerializeField] private Image vibracionImage;

        [Header("Sprites de interruptor")]
        [SerializeField] private Sprite toggleOffSprite; // boton marron
        [SerializeField] private Sprite toggleOnSprite;  // boton naranjo

        [Header("Navegación")]
        [SerializeField] private Sprite menuButtonSprite;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button volverButton;
        [SerializeField] private string menuSceneName = "MenuPrincipal";

        private const string MenuButtonLabel = "Volver al men\u00fa";
        private const string MenuButtonAssetPath = "Assets/Arte/juego/volvermenu.png";
        private static readonly Color32 MenuButtonColor = new Color32(255, 126, 38, 255);

        private SettingsManager settings;

        private void OnEnable()
        {
            settings = SettingsManager.Instance;

            AutoFindButtons();
            EnsureMenuButtonAtRuntime();

            if (homeButton != null)
                homeButton.onClick.AddListener(OnHomeClicked);

            if (volverButton != null)
                volverButton.onClick.AddListener(OnVolverClicked);

            if (settings == null) return;

            if (ambienteSlider != null) ambienteSlider.SetValueWithoutNotify(settings.AmbienteVolume);
            if (personajesSlider != null) personajesSlider.SetValueWithoutNotify(settings.PersonajesVolume);
            if (cinematicaSlider != null) cinematicaSlider.SetValueWithoutNotify(settings.CinematicaVolume);
            if (brilloSlider != null) brilloSlider.SetValueWithoutNotify(settings.Brillo);

            if (modoDaltonicoImage != null) RefreshToggle(modoDaltonicoImage, settings.ModoDaltonico);
            if (vibracionImage != null) RefreshToggle(vibracionImage, settings.Vibracion);

            if (ambienteSlider != null) ambienteSlider.onValueChanged.AddListener(OnAmbienteChanged);
            if (personajesSlider != null) personajesSlider.onValueChanged.AddListener(OnPersonajesChanged);
            if (cinematicaSlider != null) cinematicaSlider.onValueChanged.AddListener(OnCinematicaChanged);
            if (brilloSlider != null) brilloSlider.onValueChanged.AddListener(OnBrilloChanged);
            if (modoDaltonicoButton != null) modoDaltonicoButton.onClick.AddListener(OnToggleModoDaltonico);
            if (vibracionButton != null) vibracionButton.onClick.AddListener(OnToggleVibracion);
        }

        private void OnDisable()
        {
            if (homeButton != null)
                homeButton.onClick.RemoveListener(OnHomeClicked);

            if (volverButton != null)
                volverButton.onClick.RemoveListener(OnVolverClicked);

            if (settings == null) return;
            settings.Save();

            if (ambienteSlider != null) ambienteSlider.onValueChanged.RemoveListener(OnAmbienteChanged);
            if (personajesSlider != null) personajesSlider.onValueChanged.RemoveListener(OnPersonajesChanged);
            if (cinematicaSlider != null) cinematicaSlider.onValueChanged.RemoveListener(OnCinematicaChanged);
            if (brilloSlider != null) brilloSlider.onValueChanged.RemoveListener(OnBrilloChanged);
            if (modoDaltonicoButton != null) modoDaltonicoButton.onClick.RemoveListener(OnToggleModoDaltonico);
            if (vibracionButton != null) vibracionButton.onClick.RemoveListener(OnToggleVibracion);
        }

        public void OnHomeClicked()
        {
            Time.timeScale = 1f;
            Transform rootT = transform.parent != null && transform.parent.name == "SettingsPanel" ? transform.parent : transform;
            rootT.gameObject.SetActive(false);

            MainMenuController mmc = FindFirstObjectByType<MainMenuController>();
            if (mmc != null)
            {
                mmc.OnCloseSettingsClicked();
            }
            else
            {
                ScreenTransition.TryLoadScene(menuSceneName);
            }
        }

        public void OnVolverClicked()
        {
            Time.timeScale = 1f;
            Transform rootT = transform.parent != null && transform.parent.name == "SettingsPanel" ? transform.parent : transform;
            rootT.gameObject.SetActive(false);

            MainMenuController mmc = FindFirstObjectByType<MainMenuController>();
            if (mmc != null)
            {
                mmc.OnCloseSettingsClicked();
            }
        }

        private void AutoFindButtons()
        {
            Transform rootT = transform.parent != null ? transform.parent : transform;

            if (homeButton == null)
            {
                Transform homeT = rootT.Find("PanelCafe/BotonHome") ?? rootT.Find("BotonHome");
                if (homeT != null)
                {
                    homeButton = homeT.GetComponent<Button>();
                }
            }

            if (volverButton == null)
            {
                Transform volverT = rootT.Find("BotonVolver") ?? rootT.Find("PanelCafe/BotonVolver");
                if (volverT != null)
                {
                    volverButton = volverT.GetComponent<Button>();
                }
            }
        }

        private void EnsureMenuButtonAtRuntime()
        {
            Transform rootT = transform.parent != null ? transform.parent : transform;
            Transform panelT = rootT.Find("PanelCafe") ?? rootT;

            menuButtonSprite = ResolveMenuButtonSprite();

            if (homeButton == null)
            {
                GameObject homeGO = new GameObject("BotonHome", typeof(RectTransform), typeof(Image), typeof(Button));
                homeGO.transform.SetParent(panelT, false);

                Image img = homeGO.GetComponent<Image>();
                img.sprite = menuButtonSprite;
                img.color = menuButtonSprite != null ? Color.white : MenuButtonColor;

                homeButton = homeGO.GetComponent<Button>();
                homeButton.targetGraphic = img;
            }

            if (homeButton == null)
                return;

            Transform homeT = homeButton.transform;
            if (homeT.parent != panelT)
                homeT.SetParent(panelT, false);

            RectTransform homeRt = homeButton.GetComponent<RectTransform>();
            homeRt.anchorMin = new Vector2(1f, 0f);
            homeRt.anchorMax = new Vector2(1f, 0f);
            homeRt.pivot = new Vector2(1f, 0f);
            homeRt.anchoredPosition = new Vector2(-MenuButtonRightInset, MenuButtonBottomInset);
            homeRt.sizeDelta = menuButtonSprite != null
                ? SizeForWidth(menuButtonSprite, MenuButtonWidth)
                : new Vector2(340f, 82f);

            Image background = homeButton.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = menuButtonSprite;
                background.color = menuButtonSprite != null ? Color.white : MenuButtonColor;
                background.preserveAspect = menuButtonSprite != null;
            }

            Outline border = homeButton.GetComponent<Outline>();
            if (border == null && menuButtonSprite == null)
                border = homeButton.gameObject.AddComponent<Outline>();
            if (border != null)
            {
                border.enabled = menuButtonSprite == null;
                border.effectColor = Color.black;
                border.effectDistance = new Vector2(5f, -5f);
                border.useGraphicAlpha = true;
            }

            Transform iconT = homeT.Find("IconoCasa");
            if (iconT != null)
                iconT.gameObject.SetActive(false);

            Transform textT = homeT.Find("TextHome");
            if (menuButtonSprite != null)
            {
                if (textT != null)
                    textT.gameObject.SetActive(false);
                return;
            }

            Text label;
            if (textT == null)
            {
                GameObject textGO = new GameObject("TextHome", typeof(RectTransform), typeof(Text));
                textGO.transform.SetParent(homeT, false);
                label = textGO.GetComponent<Text>();
            }
            else
            {
                textT.gameObject.SetActive(true);
                label = textT.GetComponent<Text>();
                if (label == null)
                    label = textT.gameObject.AddComponent<Text>();
            }

            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(18f, 8f);
            labelRt.offsetMax = new Vector2(-18f, -8f);
            label.text = MenuButtonLabel;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 34;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;

            Outline outline = label.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;
        }

        private Sprite ResolveMenuButtonSprite()
        {
#if UNITY_EDITOR
            // Fuerza el arte original también cuando Unity conserva en memoria
            // una versión antigua de MenuPrincipal con el botón cuadrado.
            foreach (Object asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(MenuButtonAssetPath))
            {
                if (asset is Sprite sprite)
                    return sprite;
            }
#endif

            if (menuButtonSprite != null)
                return menuButtonSprite;

            if (homeButton != null && homeButton.TryGetComponent(out Image existingImage))
            {
                Sprite existingSprite = existingImage.sprite;
                if (existingSprite != toggleOnSprite && existingSprite != toggleOffSprite)
                    return existingSprite;
            }

            return null;
        }

        private static Vector2 SizeForWidth(Sprite sprite, float width)
        {
            if (sprite == null || sprite.rect.height <= 0f)
                return new Vector2(width, 100f);

            return new Vector2(width, width * sprite.rect.height / sprite.rect.width);
        }

        private void OnAmbienteChanged(float v) => settings.AmbienteVolume = v;
        private void OnPersonajesChanged(float v) => settings.PersonajesVolume = v;
        private void OnCinematicaChanged(float v) => settings.CinematicaVolume = v;
        private void OnBrilloChanged(float v) => settings.Brillo = v;

        private void OnToggleModoDaltonico()
        {
            settings.ModoDaltonico = !settings.ModoDaltonico;
            RefreshToggle(modoDaltonicoImage, settings.ModoDaltonico);
        }

        private void OnToggleVibracion()
        {
            settings.Vibracion = !settings.Vibracion;
            RefreshToggle(vibracionImage, settings.Vibracion);
        }

        private void RefreshToggle(Image image, bool on)
        {
            if (image != null)
                image.sprite = on ? toggleOnSprite : toggleOffSprite;
        }
    }
}
