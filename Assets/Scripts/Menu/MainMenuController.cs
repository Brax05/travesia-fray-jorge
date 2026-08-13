using UnityEngine;

namespace TravesiaACasa.Menu
{
    /// <summary>
    /// Botones del menú principal (Arte/menu/jugar.png y
    /// configuracion.png). "Jugar" carga la escena de juego por nombre
    /// para no depender del orden en Build Settings.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "Juego";
        [SerializeField] private GameObject settingsPanelRoot;
        [SerializeField] private GameObject menuContentRoot;

        private GameObject[] fallbackMenuObjects;

        private void Awake()
        {
            AutoFindSettingsPanel();
        }

        private void AutoFindSettingsPanel()
        {
            if (settingsPanelRoot == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform panelT = canvas.transform.Find("SettingsPanel");
                    if (panelT != null)
                        settingsPanelRoot = panelT.gameObject;
                }
            }
        }

        public void OnPlayClicked()
        {
            ScreenTransition.TryLoadScene(gameSceneName);
        }

        public void OnOpenSettingsClicked()
        {
            AutoFindSettingsPanel();
            SetMenuContentVisible(false);
            if (settingsPanelRoot != null)
                settingsPanelRoot.SetActive(true);
        }

        public void OnCloseSettingsClicked()
        {
            AutoFindSettingsPanel();
            if (settingsPanelRoot != null)
                settingsPanelRoot.SetActive(false);
            SetMenuContentVisible(true);
        }

        private void SetMenuContentVisible(bool visible)
        {
            if (menuContentRoot == null)
                menuContentRoot = GameObject.Find("MenuContent");

            if (menuContentRoot != null)
            {
                menuContentRoot.SetActive(visible);
                return;
            }

            SetNamedObjectVisible("Titulo", visible);
            SetNamedObjectVisible("BotonJugar", visible);
            SetNamedObjectVisible("BotonConfiguracion", visible);
        }

        private void SetNamedObjectVisible(string objectName, bool visible)
        {
            GameObject go = GetFallbackMenuObject(objectName);
            if (go != null)
                go.SetActive(visible);
        }

        private GameObject GetFallbackMenuObject(string objectName)
        {
            if (fallbackMenuObjects == null)
            {
                fallbackMenuObjects = new[]
                {
                    GameObject.Find("Titulo"),
                    GameObject.Find("BotonJugar"),
                    GameObject.Find("BotonConfiguracion")
                };
            }

            foreach (GameObject go in fallbackMenuObjects)
            {
                if (go != null && go.name == objectName)
                    return go;
            }

            return null;
        }
    }
}
