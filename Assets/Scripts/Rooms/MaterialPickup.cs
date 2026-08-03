using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TravesiaACasa.Rooms
{
    /// <summary>
    /// Material de misión tirado en una room, que el jugador recoge con
    /// solo pasarle por encima (trigger). Al recogerlo suma al
    /// inventario (CollectibleManager/InventoryManager si existen) y
    /// muestra un aviso en pantalla con el nombre del componente
    /// ("¡Recogiste Madera de maitén!") vía PickupToast.
    ///
    /// Los coloca la tool Game/Misión/Colocar materiales de la misión;
    /// arrancan desactivados y MissionIntroCutscene los enciende cuando
    /// el jugador acepta la misión.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MaterialPickup : MonoBehaviour
    {
        [Tooltip("Id único de esta instancia, ej: madera_01")]
        [SerializeField] private string itemId = "material_01";

        [Tooltip("Clave de inventario del tipo de material, ej: pluma_alicanto")]
        [SerializeField] private string inventoryKey = "material";

        [Tooltip("Nombre para el aviso en pantalla, ej: Madera de maitén")]
        [SerializeField] private string displayName = "Material";

        [SerializeField] private string playerTag = "Player";

        /// <summary>Se dispara al recoger cualquier material (lo escucha MissionIntroCutscene).</summary>
        public static event System.Action<MaterialPickup> Collected;

        public string DisplayName => displayName;

        private bool collected;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || !other.CompareTag(playerTag)) return;
            collected = true;

            string roomId = RoomGraphManager.Instance != null && RoomGraphManager.Instance.CurrentNode != null
                ? RoomGraphManager.Instance.CurrentNode.roomId
                : "unknown_room";
            CollectibleManager.Instance?.Collect(roomId, itemId, inventoryKey);

            PickupToast.Show($"¡Recogiste {displayName}!");
            Collected?.Invoke(this);
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Aviso flotante único en el Canvas del HUD (texto centrado arriba)
    /// que se crea solo la primera vez que se usa y se desvanece después
    /// de un par de segundos. Uso: PickupToast.Show("mensaje").
    /// </summary>
    public class PickupToast : MonoBehaviour
    {
        private static PickupToast instance;

        private Text label;
        private Coroutine hideRoutine;

        public static void Show(string message)
        {
            if (instance == null) instance = Create();
            if (instance == null) return;
            instance.ShowInternal(message);
        }

        private static PickupToast Create()
        {
            Canvas canvas = TopLeftGameplayHud.FindGameplayCanvas();
            if (canvas == null) return null;

            GameObject go = new GameObject("PickupToast", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -170f);
            rect.sizeDelta = new Vector2(1200f, 90f);

            PickupToast toast = go.AddComponent<PickupToast>();
            toast.label = go.AddComponent<Text>();
            toast.label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            toast.label.fontSize = 52;
            toast.label.fontStyle = FontStyle.Bold;
            toast.label.alignment = TextAnchor.MiddleCenter;
            toast.label.color = Color.white;
            toast.label.raycastTarget = false;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.15f, 0.1f, 0.05f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);

            return toast;
        }

        private void ShowInternal(string message)
        {
            gameObject.SetActive(true);
            label.text = message;
            Color color = label.color;
            color.a = 1f;
            label.color = color;

            if (hideRoutine != null) StopCoroutine(hideRoutine);
            hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(2f);

            const float fadeSeconds = 0.6f;
            float elapsed = 0f;
            Color color = label.color;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.deltaTime;
                color.a = 1f - elapsed / fadeSeconds;
                label.color = color;
                yield return null;
            }

            hideRoutine = null;
            gameObject.SetActive(false);
        }
    }
}
