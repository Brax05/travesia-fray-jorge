using UnityEngine;

namespace TravesiaACasa.Rooms
{
    /// <summary>
    /// Cámara 2D fija por room, estilo "pantalla fija": queda clavada
    /// en el centro del RoomNode actual y al cambiar de room corta en
    /// seco al centro del nodo nuevo. Sin seguimiento del jugador ni
    /// paneo: el encuadre nunca puede descuadrarse ni asomarse a la
    /// room vecina.
    ///
    /// Además ajusta el zoom para que el fondo de la room SIEMPRE cubra
    /// la pantalla, sea cual sea la resolución o el aspecto (pantalla
    /// completa, celular, ultrawide). Sin esto quedaban márgenes del
    /// color de cámara: el orthographicSize estaba fijo en la medida del
    /// arte viejo (8.78 de alto) y los escenarios nuevos son 16:9, así
    /// que en cuanto la pantalla era más panorámica que ~2.16:1 el fondo
    /// no llegaba a los bordes.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRoomFollower : MonoBehaviour
    {
        [Tooltip("Nombre del hijo de cada room que hace de fondo.")]
        [SerializeField] private string backgroundName = RoomBackgroundLocator.DefaultName;

        [Tooltip("Zoom a usar si una room no tiene fondo identificable.")]
        [SerializeField] private float fallbackOrthographicSize = 4.388184f;

        [Tooltip("Tope de zoom. Las rooms están separadas 10 unidades en vertical y los " +
                 "fondos nuevos miden 10.69 de alto, así que un encuadre de más de 5 dejaría " +
                 "asomar la room de arriba o de abajo. Subir esto solo si se separan las rooms.")]
        [SerializeField] private float maxOrthographicSize = 4.9f;

        private Camera cam;
        private RoomNode lastNode;
        private float lastAspect;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            ColorblindFilter.EnsureCameraAttached(cam);
        }

        private void LateUpdate()
        {
            RoomNode current = RoomGraphManager.Instance != null ? RoomGraphManager.Instance.CurrentNode : null;
            if (current == null) return;

            UpdateCameraForRoom(current);
        }

        /// <summary>
        /// Sincroniza inmediatamente posición y zoom. Se usa con la pantalla
        /// cubierta para poder calcular la entrada respecto del encuadre de
        /// la room destino, sin esperar al siguiente LateUpdate.
        /// </summary>
        public void SnapToRoom(RoomNode room)
        {
            if (room == null) return;
            UpdateCameraForRoom(room, forceRecalculate: true);
        }

        private void UpdateCameraForRoom(RoomNode room, bool forceRecalculate = false)
        {
            if (cam == null)
                cam = GetComponent<Camera>();

            Vector3 nodeCenter = room.testWorldPosition;
            SpriteRenderer background = RoomBackgroundLocator.Find(nodeCenter, backgroundName);
            Vector3 camPosition = background != null ? background.bounds.center : nodeCenter;

            transform.position = new Vector3(camPosition.x, camPosition.y, transform.position.z);

            if (forceRecalculate || room != lastNode || !Mathf.Approximately(cam.aspect, lastAspect))
            {
                lastNode = room;
                lastAspect = cam.aspect;
                FitToBackground(background);
            }
        }

        private void FitToBackground(SpriteRenderer background)
        {
            if (background == null)
            {
                cam.orthographicSize = fallbackOrthographicSize;
                return;
            }

            Vector3 size = background.bounds.size;
            if (size.x <= 0f || size.y <= 0f || cam.aspect <= 0f)
            {
                cam.orthographicSize = fallbackOrthographicSize;
                return;
            }

            // El zoom más abierto que todavía deja el fondo cubriendo los
            // dos ejes: si la pantalla es muy panorámica manda el ancho,
            // si es más cuadrada manda el alto.
            float byHeight = size.y * 0.5f;
            float byWidth = size.x * 0.5f / cam.aspect;
            float fitted = Mathf.Min(byHeight, byWidth);
            // Un tope en 0 (o negativo) dejaría la cámara sin encuadre: se ignora.
            cam.orthographicSize = maxOrthographicSize > 0f
                ? Mathf.Min(fitted, maxOrthographicSize)
                : fitted;
        }

    }
}
