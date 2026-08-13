using System;
using UnityEngine;

namespace TravesiaACasa.Rooms
{
    /// <summary>
    /// Controla en que RoomNode esta el jugador y valida que solo se pueda
    /// mover a rooms realmente conectadas en el grafo.
    ///
    /// Cada viaje es una operacion atomica: bloquea nuevas solicitudes y el
    /// movimiento, cubre la pantalla, sincroniza camara/fisica con la room
    /// nueva y solo entonces devuelve el control.
    /// </summary>
    public class RoomGraphManager : MonoBehaviour
    {
        public static RoomGraphManager Instance { get; private set; }

        [Header("Nodo inicial")]
        [SerializeField] private RoomNode startingNode;

        [Header("Referencias")]
        [SerializeField] private Transform player;

        [Header("Entrada a la room destino")]
        [Tooltip("Al entrar sin entryPoint explicito, el jugador se reubica dentro de este " +
                 "rectangulo (medio ancho/alto) alrededor del centro de la room destino.")]
        [SerializeField] private Vector2 entryClampHalfExtents = new Vector2(8.6f, 3.6f);
        [Tooltip("Separacion visual entre el sprite del jugador y el borde al aparecer.")]
        [SerializeField, Min(0f)] private float edgeSpawnPadding = 0.85f;

        [Header("Transicion visual")]
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.07f;
        [SerializeField, Min(0f)] private float coveredDuration = 0f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.09f;

        public RoomNode CurrentNode { get; private set; }
        public bool IsTransitioning { get; private set; }
        public Collider2D PlayerCollider { get; private set; }

        /// <summary>Se dispara cuando el jugador ya esta en la nueva room.</summary>
        public event Action<RoomNode> NodeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CachePlayerCollider();
        }

        private void Start()
        {
            if (startingNode == null)
                return;

            CachePlayerCollider();

            CurrentNode = startingNode;
            if (player != null)
                SetPlayerPosition(startingNode.testWorldPosition);

            NodeChanged?.Invoke(startingNode);
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            SetPlayerControlEnabled(true);
            Instance = null;
        }

        /// <summary>
        /// Intenta iniciar el viaje al nodo destino. Devuelve false si la
        /// ruta no es valida o ya hay una transicion en curso.
        /// </summary>
        public bool TravelTo(RoomNode target, Vector3? entryPosition = null)
        {
            if (target == null || target == CurrentNode || IsTransitioning)
                return false;

            if (CurrentNode != null && !CurrentNode.IsConnectedTo(target))
            {
                Debug.LogWarning($"[RoomGraphManager] '{target.roomId}' no esta conectado a " +
                                 $"'{CurrentNode.roomId}'. Revisa las conexiones en el RoomNode.");
                return false;
            }

            // El fundido global tambien protege la carga Menu -> Juego.
            if (ScreenTransition.IsBusy)
                return false;

            IsTransitioning = true;
            SetPlayerControlEnabled(false);
            RoomNode source = CurrentNode;

            bool started = ScreenTransition.TryFadeThrough(
                () => ApplyTravel(source, target, entryPosition),
                CompleteTransition,
                fadeOutDuration,
                coveredDuration,
                fadeInDuration);

            if (started)
                return true;

            IsTransitioning = false;
            SetPlayerControlEnabled(true);
            return false;
        }

        private void ApplyTravel(RoomNode source, RoomNode target, Vector3? entryPosition)
        {
            CurrentNode = target;
            SnapCameraToRoom(target);

            if (player != null)
            {
                RoomExitPoint returnExit = FindReciprocalExit(source, target);
                Vector3 destination;
                if (entryPosition.HasValue)
                {
                    destination = entryPosition.Value;
                    destination.z = player.position.z;
                }
                else if (returnExit != null
                    && player.TryGetComponent(out Collider2D entryCollider)
                    && returnExit.TryGetVisibleEdgeEntryPosition(
                        source.testWorldPosition,
                        target.testWorldPosition,
                        entryCollider,
                        edgeSpawnPadding,
                        out Vector3 reciprocalEntry))
                {
                    destination = reciprocalEntry;
                    destination.z = player.position.z;
                }
                else
                {
                    // Conserva el eje transversal del movimiento, pero lleva
                    // al jugador inmediatamente al borde interior del destino.
                    destination = player.position;
                    Vector2 center = target.testWorldPosition;
                    destination.x = Mathf.Clamp(
                        destination.x,
                        center.x - entryClampHalfExtents.x,
                        center.x + entryClampHalfExtents.x);
                    destination.y = Mathf.Clamp(
                        destination.y,
                        center.y - entryClampHalfExtents.y,
                        center.y + entryClampHalfExtents.y);
                }

                SetPlayerPosition(destination);

                if (returnExit != null && player.TryGetComponent(out Collider2D playerCollider))
                    returnExit.ArmForArrival(playerCollider);
            }

            NodeChanged?.Invoke(target);
        }

        private RoomExitPoint FindReciprocalExit(RoomNode source, RoomNode target)
        {
            if (source == null || target == null || player == null)
                return null;

            RoomExitPoint[] exits = FindObjectsByType<RoomExitPoint>(FindObjectsInactive.Exclude);
            RoomExitPoint closestReturnExit = null;
            float closestDistance = float.PositiveInfinity;
            Vector2 targetCenter = target.testWorldPosition;

            foreach (RoomExitPoint exit in exits)
            {
                if (!exit.isActiveAndEnabled || exit.TargetNode != source)
                    continue;

                float distance = ((Vector2)exit.transform.position - targetCenter).sqrMagnitude;
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestReturnExit = exit;
            }

            if (closestReturnExit == null)
                return null;

            return closestReturnExit;
        }

        private static void SnapCameraToRoom(RoomNode room)
        {
            Camera gameplayCamera = Camera.main;
            if (gameplayCamera != null
                && gameplayCamera.TryGetComponent(out CameraRoomFollower follower))
            {
                follower.SnapToRoom(room);
            }
        }

        private void CompleteTransition()
        {
            IsTransitioning = false;
            SetPlayerControlEnabled(true);
        }

        private void SetPlayerControlEnabled(bool enabled)
        {
            if (player == null)
                return;

            if (player.TryGetComponent(out BirdPlayerController controller))
                controller.SetMovementEnabled(enabled);

            if (!enabled && player.TryGetComponent(out Rigidbody2D body))
                body.linearVelocity = Vector2.zero;
        }

        private void CachePlayerCollider()
        {
            Collider2D playerCollider = null;
            if (player != null)
                player.TryGetComponent(out playerCollider);

            PlayerCollider = playerCollider;
        }

        private void SetPlayerPosition(Vector3 position)
        {
            if (player == null)
                return;

            if (player.TryGetComponent(out Rigidbody2D body))
            {
                body.position = new Vector2(position.x, position.y);
                body.linearVelocity = Vector2.zero;
            }
            else
            {
                player.position = position;
            }

            Physics2D.SyncTransforms();
        }
    }
}
