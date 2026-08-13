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
        [SerializeField] private Button homeButton;
        [SerializeField] private Button volverButton;
        [SerializeField] private string menuSceneName = "MenuPrincipal";

        private SettingsManager settings;

        private void OnEnable()
        {
            settings = SettingsManager.Instance;

            AutoFindButtons();
            EnsureHomeButtonAtRuntime();

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

        private void EnsureHomeButtonAtRuntime()
        {
            Sprite casaSprite = null;
#if UNITY_EDITOR
            casaSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/configuracion/icono_casa.png");
#endif

            if (homeButton == null)
            {
                Transform rootT = transform.parent != null ? transform.parent : transform;
                Transform panelT = rootT.Find("PanelCafe") ?? rootT;

                GameObject homeGO = new GameObject("BotonHome", typeof(RectTransform), typeof(Image), typeof(Button));
                homeGO.transform.SetParent(panelT, false);

                RectTransform homeRt = homeGO.GetComponent<RectTransform>();
                homeRt.anchorMin = new Vector2(0.5f, 0.10f);
                homeRt.anchorMax = new Vector2(0.5f, 0.10f);
                homeRt.pivot = new Vector2(0.5f, 0.5f);
                homeRt.anchoredPosition = Vector2.zero;
                homeRt.sizeDelta = new Vector2(110f, 95f);

                Image img = homeGO.GetComponent<Image>();
                if (toggleOnSprite != null)
                    img.sprite = toggleOnSprite;
                img.preserveAspect = true;

                homeButton = homeGO.GetComponent<Button>();
                homeButton.targetGraphic = img;
            }

            if (homeButton != null)
            {
                Transform homeT = homeButton.transform;
                Transform textChild = homeT.Find("TextHome");

                if (casaSprite != null && homeT.Find("IconoCasa") == null)
                {
                    GameObject iconGO = new GameObject("IconoCasa", typeof(RectTransform), typeof(Image));
                    iconGO.transform.SetParent(homeT, false);
                    RectTransform iconRt = iconGO.GetComponent<RectTransform>();
                    iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                    iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRt.pivot = new Vector2(0.5f, 0.5f);
                    iconRt.anchoredPosition = Vector2.zero;
                    iconRt.sizeDelta = new Vector2(55f, 55f);

                    Image iconImg = iconGO.GetComponent<Image>();
                    iconImg.sprite = casaSprite;
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget = false;
                }

                if (homeT.Find("IconoCasa") != null && textChild != null)
                {
                    textChild.gameObject.SetActive(false);
                }
            }
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
