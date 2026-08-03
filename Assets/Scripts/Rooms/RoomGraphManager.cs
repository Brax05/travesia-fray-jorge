using System;
using UnityEngine;

namespace TravesiaACasa.Rooms
{
    /// <summary>
    /// Controla en qué RoomNode está el jugador y valida que solo se
    /// pueda mover a rooms realmente conectadas en el grafo.
    ///
    /// Cruzar de room no interrumpe al jugador: al pisar la salida
    /// solo cambia el nodo actual (la cámara corta en seco vía
    /// CameraRoomFollower) y el jugador sigue caminando con su propio
    /// impulso, sin perder el control. Si el RoomExitPoint define un
    /// entryPoint explícito, el jugador aparece ahí al instante (útil
    /// para puertas que cruzan huecos o distancias grandes).
    /// </summary>
    public class RoomGraphManager : MonoBehaviour
    {
        public static RoomGraphManager Instance { get; private set; }

        [Header("Nodo inicial")]
        [SerializeField] private RoomNode startingNode;

        [Header("Referencias")]
        [SerializeField] private Transform player;

        [Header("Entrada a la room destino")]
        [Tooltip("Al entrar sin entryPoint explícito, el jugador se reubica dentro de este " +
                 "rectángulo (medio ancho/alto) alrededor del centro de la room destino. Evita " +
                 "que camine por el vacío entre fondos (las rooms están separadas ~15 unidades " +
                 "en vertical pero el fondo mide ~8.8 de alto).")]
        [SerializeField] private Vector2 entryClampHalfExtents = new Vector2(8.6f, 3.6f);

        public RoomNode CurrentNode { get; private set; }

        /// <summary>Se dispara cada vez que el jugador entra a una nueva room.</summary>
        public event Action<RoomNode> NodeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (startingNode == null) return;

            // La carga inicial sí es un posicionamiento en seco: todavía
            // no hay "desde dónde" entrar caminando.
            CurrentNode = startingNode;
            if (player != null)
                player.position = startingNode.testWorldPosition;

            NodeChanged?.Invoke(startingNode);
        }

        /// <summary>
        /// Intenta moverse al nodo destino. Si no está conectado al nodo
        /// actual en el grafo, no hace nada (evita "atajos" ilegales
        /// aunque algún trigger esté mal configurado).
        /// </summary>
        /// <param name="target">Room destino.</param>
        /// <param name="entryPosition">
        /// (Opcional) Posición exacta donde debe quedar el jugador dentro
        /// de la room destino. Si es null, el jugador no se toca: entra
        /// caminando por su cuenta.
        /// </param>
        public void TravelTo(RoomNode target, Vector3? entryPosition = null)
        {
            if (target == null || target == CurrentNode) return;

            if (CurrentNode != null && !CurrentNode.IsConnectedTo(target))
            {
                Debug.LogWarning($"[RoomGraphManager] '{target.roomId}' no está conectado a " +
                                  $"'{CurrentNode.roomId}'. Revisa las conexiones en el RoomNode.");
                return;
            }

            CurrentNode = target;
            NodeChanged?.Invoke(target);

            if (player == null) return;

            if (entryPosition.HasValue)
            {
                Vector3 end = entryPosition.Value;
                end.z = player.position.z;
                player.position = end;
            }
            else
            {
                // Sin entryPoint: el jugador conserva su impulso, pero se
                // recorta su posición al área visible de la room nueva para
                // que no tenga que cruzar caminando el hueco entre fondos.
                Vector3 clamped = player.position;
                Vector2 center = target.testWorldPosition;
                clamped.x = Mathf.Clamp(clamped.x, center.x - entryClampHalfExtents.x, center.x + entryClampHalfExtents.x);
                clamped.y = Mathf.Clamp(clamped.y, center.y - entryClampHalfExtents.y, center.y + entryClampHalfExtents.y);
                player.position = clamped;
            }
        }
    }
}
