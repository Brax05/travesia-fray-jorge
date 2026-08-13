using UnityEngine;

namespace TravesiaACasa.Rooms
{
    /// <summary>
    /// Salida entre rooms. El trigger amplio solo arma la salida; el cambio
    /// real espera hasta que el ave alcanza el borde visible de la cámara.
    /// También comprueba que el jugador se mueva hacia la room destino para
    /// evitar activaciones accidentales y regresos en ping-pong.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class RoomExitPoint : MonoBehaviour
    {
        [Tooltip("A qué RoomNode lleva esta salida")]
        [SerializeField] private RoomNode targetNode;

        [Tooltip("(Opcional) Dónde queda exactamente el jugador dentro de la room destino.")]
        [SerializeField] private Transform entryPoint;

        [SerializeField] private string playerTag = "Player";

        [Header("Momento de activación")]
        [Tooltip("Margen normalizado desde el borde visible. 0 espera a que el sprite toque " +
                 "el borde; valores mayores cambian un poco antes.")]
        [SerializeField, Range(0f, 0.15f)] private float visibleEdgeMargin = 0f;

        [Tooltip("Respaldo usado si no hay una cámara activa: distancia desde el centro " +
                 "del trigger hacia afuera.")]
        [SerializeField] private float activationOffset = 0f;

        public RoomNode TargetNode => targetNode;

        private Collider2D trackedPlayer;
        private RoomNode resolvedSourceNode;
        private RoomNode sourceResolutionTarget;

        /// <summary>
        /// Mantiene disponible el regreso aunque el punto de llegada quede
        /// entre el trigger y el borde visible de la room.
        /// </summary>
        public void ArmForArrival(Collider2D playerCollider)
        {
            if (isActiveAndEnabled && playerCollider != null)
                trackedPlayer = playerCollider;
        }

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryTrackPlayer(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryTrackPlayer(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other != trackedPlayer)
                return;

            RoomGraphManager manager = RoomGraphManager.Instance;
            if (manager == null || !IsOnOuterSideOfTrigger(other, manager))
                trackedPlayer = null;
        }

        private void FixedUpdate()
        {
            if (trackedPlayer != null)
                TryActivateTrackedPlayer();
            else
                TryActivateFromConnectedEdge();
        }

        private void OnDisable()
        {
            trackedPlayer = null;
        }

        private void TryActivateFromConnectedEdge()
        {
            RoomGraphManager manager = RoomGraphManager.Instance;
            if (manager == null || manager.IsTransitioning || manager.PlayerCollider == null)
                return;

            if (ResolveSourceNode() != manager.CurrentNode)
                return;

            Collider2D playerCollider = manager.PlayerCollider;
            if (!PlayerIsMovingTowardTarget(playerCollider, manager)) return;
            if (!IsWithinExitTriggerSpan(playerCollider, manager)) return;
            if (!HasReachedVisibleRoomEdge(playerCollider, manager)) return;

            // El borde completo funciona como salida virtual en el tramo del trigger.
            trackedPlayer = playerCollider;
            TryActivateTrackedPlayer();
        }

        private RoomNode ResolveSourceNode()
        {
            if (sourceResolutionTarget == targetNode)
                return resolvedSourceNode;

            sourceResolutionTarget = targetNode;
            resolvedSourceNode = null;
            if (targetNode == null || targetNode.connections == null)
                return null;

            float closestDistance = float.PositiveInfinity;
            Vector2 exitPosition = transform.position;

            foreach (RoomNode candidate in targetNode.connections)
            {
                if (candidate == null)
                    continue;

                float distance = ((Vector2)candidate.testWorldPosition - exitPosition).sqrMagnitude;
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                resolvedSourceNode = candidate;
            }

            return resolvedSourceNode;
        }

        private void TryTrackPlayer(Collider2D other)
        {
            if (!other.CompareTag(playerTag)) return;

            RoomGraphManager manager = RoomGraphManager.Instance;
            if (manager == null || manager.IsTransitioning) return;
            if (targetNode == null || targetNode == manager.CurrentNode) return;
            if (!PlayerIsMovingTowardTarget(other, manager)) return;

            // Entrar al trigger solo arma esta salida. Si el collider termina
            // antes que la pantalla, FixedUpdate continúa siguiendo al ave.
            trackedPlayer = other;
            TryActivateTrackedPlayer();
        }

        private void TryActivateTrackedPlayer()
        {
            if (trackedPlayer == null)
                return;

            RoomGraphManager manager = RoomGraphManager.Instance;
            if (manager == null || manager.CurrentNode == null || targetNode == null
                || targetNode == manager.CurrentNode)
            {
                trackedPlayer = null;
                return;
            }

            if (manager.IsTransitioning) return;
            if (!PlayerIsMovingTowardTarget(trackedPlayer, manager)) return;
            if (!HasReachedVisibleRoomEdge(trackedPlayer, manager)) return;

            bool started = manager.TravelTo(
                targetNode,
                entryPoint != null ? entryPoint.position : (Vector3?)null);

            if (started)
                trackedPlayer = null;
        }

        private bool PlayerIsMovingTowardTarget(Collider2D playerCollider, RoomGraphManager manager)
        {
            Rigidbody2D body = playerCollider.attachedRigidbody;
            if (body == null || manager.CurrentNode == null)
                return true;

            if (!TryGetTravelDirection(manager, out Vector2 travelDirection))
                return true;

            Vector2 movement = body.linearVelocity;
            if (body.TryGetComponent(out BirdPlayerController controller))
                movement = controller.MovementInput;

            if (movement.sqrMagnitude < 0.01f)
                return false;

            return Vector2.Dot(movement.normalized, travelDirection) > 0.1f;
        }

        private bool HasReachedVisibleRoomEdge(Collider2D playerCollider, RoomGraphManager manager)
        {
            Camera gameplayCamera = Camera.main;
            if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled
                || !TryGetTravelDirection(manager, out Vector2 travelDirection))
            {
                return HasCrossedFallbackLine(playerCollider, manager);
            }

            Bounds visibleBounds = playerCollider.bounds;
            Rigidbody2D body = playerCollider.attachedRigidbody;
            Renderer playerRenderer = body != null ? body.GetComponent<Renderer>() : null;
            if (playerRenderer != null && playerRenderer.enabled)
                visibleBounds = playerRenderer.bounds;

            Vector3 leadingPoint = visibleBounds.center;
            float margin = Mathf.Clamp(visibleEdgeMargin, 0f, 0.15f);

            if (Mathf.Abs(travelDirection.x) >= Mathf.Abs(travelDirection.y))
            {
                leadingPoint.x = travelDirection.x >= 0f ? visibleBounds.max.x : visibleBounds.min.x;
                Vector3 viewportPoint = gameplayCamera.WorldToViewportPoint(leadingPoint);
                if (viewportPoint.z <= 0f)
                    return HasCrossedFallbackLine(playerCollider, manager);

                return travelDirection.x >= 0f
                    ? viewportPoint.x >= 1f - margin
                    : viewportPoint.x <= margin;
            }

            leadingPoint.y = travelDirection.y >= 0f ? visibleBounds.max.y : visibleBounds.min.y;
            Vector3 verticalViewportPoint = gameplayCamera.WorldToViewportPoint(leadingPoint);
            if (verticalViewportPoint.z <= 0f)
                return HasCrossedFallbackLine(playerCollider, manager);

            return travelDirection.y >= 0f
                ? verticalViewportPoint.y >= 1f - margin
                : verticalViewportPoint.y <= margin;
        }

        private bool IsWithinExitTriggerSpan(Collider2D playerCollider, RoomGraphManager manager)
        {
            if (!TryGetTravelDirection(manager, out Vector2 travelDirection))
                return true;

            Collider2D triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider == null) return true;

            Bounds triggerBounds = triggerCollider.bounds;
            Bounds playerBounds = playerCollider.bounds;

            // Para viajes verticales (arriba/abajo), el jugador debe estar en el ancho del trigger de salida
            if (Mathf.Abs(travelDirection.y) > Mathf.Abs(travelDirection.x))
            {
                float margin = Mathf.Max(playerBounds.extents.x * 0.5f, 0.25f);
                return playerBounds.max.x >= triggerBounds.min.x - margin &&
                       playerBounds.min.x <= triggerBounds.max.x + margin;
            }
            // Para viajes horizontales (izquierda/derecha), el jugador debe estar en el alto del trigger de salida
            else
            {
                float margin = Mathf.Max(playerBounds.extents.y * 0.5f, 0.25f);
                return playerBounds.max.y >= triggerBounds.min.y - margin &&
                       playerBounds.min.y <= triggerBounds.max.y + margin;
            }
        }

        private bool IsOnOuterSideOfTrigger(Collider2D playerCollider, RoomGraphManager manager)
        {
            if (!TryGetTravelDirection(manager, out Vector2 travelDirection))
                return false;

            Collider2D triggerCollider = GetComponent<Collider2D>();
            Vector2 fromTriggerCenter = (Vector2)playerCollider.bounds.center
                                      - (Vector2)triggerCollider.bounds.center;
            return Vector2.Dot(fromTriggerCenter, travelDirection) >= 0f;
        }

        private bool TryGetTravelDirection(RoomGraphManager manager, out Vector2 travelDirection)
        {
            travelDirection = Vector2.zero;
            if (manager == null || manager.CurrentNode == null || targetNode == null)
                return false;

            travelDirection = targetNode.testWorldPosition - manager.CurrentNode.testWorldPosition;
            if (travelDirection.sqrMagnitude < 0.0001f)
                return false;

            travelDirection.Normalize();
            return true;
        }

        private bool HasCrossedFallbackLine(Collider2D playerCollider, RoomGraphManager manager)
        {
            if (!TryGetTravelDirection(manager, out Vector2 travelDirection))
                return true;

            Collider2D triggerCollider = GetComponent<Collider2D>();
            Vector2 fromTriggerCenter = (Vector2)playerCollider.bounds.center
                                      - (Vector2)triggerCollider.bounds.center;
            float distancePastCenter = Vector2.Dot(fromTriggerCenter, travelDirection);

            Vector3 triggerExtents = triggerCollider.bounds.extents;
            Vector3 playerExtents = playerCollider.bounds.extents;
            float triggerRadius = Mathf.Abs(travelDirection.x) * triggerExtents.x
                                + Mathf.Abs(travelDirection.y) * triggerExtents.y;
            float playerRadius = Mathf.Abs(travelDirection.x) * playerExtents.x
                               + Mathf.Abs(travelDirection.y) * playerExtents.y;
            float reachableOffset = Mathf.Max(0f, triggerRadius + playerRadius - 0.01f);
            float safeOffset = Mathf.Clamp(activationOffset, -reachableOffset, reachableOffset);

            return distancePastCenter >= safeOffset;
        }

        /// <summary>
        /// Calcula un punto apenas pasado este portal, del lado interior de
        /// la room, para que siempre sea posible volver directamente.
        /// </summary>
        public bool TryGetVisibleEdgeEntryPosition(
            Vector2 sourceRoomCenter,
            Vector2 roomCenter,
            Collider2D playerCollider,
            float edgePadding,
            out Vector3 entryPosition)
        {
            Collider2D triggerCollider = GetComponent<Collider2D>();
            Vector2 triggerCenter = triggerCollider.bounds.center;
            Vector2 towardInterior = roomCenter - triggerCenter;

            if (!triggerCollider.isActiveAndEnabled || towardInterior.sqrMagnitude < 0.0001f)
            {
                entryPosition = default;
                return false;
            }

            towardInterior.Normalize();

            Camera gameplayCamera = Camera.main;
            if (gameplayCamera != null && gameplayCamera.isActiveAndEnabled
                && gameplayCamera.orthographic && playerCollider != null)
            {
                Bounds visibleBounds = playerCollider.bounds;
                Rigidbody2D body = playerCollider.attachedRigidbody;
                Renderer playerRenderer = body != null ? body.GetComponent<Renderer>() : null;
                if (playerRenderer != null && playerRenderer.enabled)
                    visibleBounds = playerRenderer.bounds;

                Vector2 towardEdge = -towardInterior;
                Vector2 edgePosition = triggerCenter;
                Vector2 sourcePlayerCenter = playerCollider.bounds.center;
                Vector2 relativePosition = sourcePlayerCenter - sourceRoomCenter;
                float padding = Mathf.Max(0f, edgePadding);

                if (Mathf.Abs(towardEdge.x) >= Mathf.Abs(towardEdge.y))
                {
                    float available = Mathf.Max(
                        0f,
                        gameplayCamera.orthographicSize * gameplayCamera.aspect
                        - visibleBounds.extents.x - padding);
                    edgePosition.x = roomCenter.x + Mathf.Sign(towardEdge.x) * available;

                    float transverseLimit = Mathf.Max(
                        0f,
                        gameplayCamera.orthographicSize - visibleBounds.extents.y);
                    edgePosition.y = roomCenter.y + Mathf.Clamp(
                        relativePosition.y,
                        -transverseLimit,
                        transverseLimit);
                }
                else
                {
                    float available = Mathf.Max(
                        0f,
                        gameplayCamera.orthographicSize
                        - visibleBounds.extents.y - padding);
                    edgePosition.y = roomCenter.y + Mathf.Sign(towardEdge.y) * available;

                    float transverseLimit = Mathf.Max(
                        0f,
                        gameplayCamera.orthographicSize * gameplayCamera.aspect
                        - visibleBounds.extents.x);
                    edgePosition.x = roomCenter.x + Mathf.Clamp(
                        relativePosition.x,
                        -transverseLimit,
                        transverseLimit);
                }

                entryPosition = new Vector3(edgePosition.x, edgePosition.y, transform.position.z);
                return true;
            }

            // Respaldo para escenas sin cámara: permanece del lado interior
            // del trigger, que conserva el regreso seguro del sistema viejo.
            Vector3 triggerExtents = triggerCollider.bounds.extents;
            float triggerRadius = Mathf.Abs(towardInterior.x) * triggerExtents.x
                                + Mathf.Abs(towardInterior.y) * triggerExtents.y;

            float playerRadius = 0f;
            if (playerCollider != null)
            {
                Vector3 playerExtents = playerCollider.bounds.extents;
                playerRadius = Mathf.Abs(towardInterior.x) * playerExtents.x
                             + Mathf.Abs(towardInterior.y) * playerExtents.y;
            }

            Vector2 fallbackPosition = triggerCenter + towardInterior
                * (triggerRadius + playerRadius + Mathf.Max(0f, edgePadding));

            Vector2 fallbackRelativePosition = playerCollider != null
                ? (Vector2)playerCollider.bounds.center - sourceRoomCenter
                : Vector2.zero;
            if (Mathf.Abs(towardInterior.x) >= Mathf.Abs(towardInterior.y))
                fallbackPosition.y = roomCenter.y + fallbackRelativePosition.y;
            else
                fallbackPosition.x = roomCenter.x + fallbackRelativePosition.x;

            entryPosition = new Vector3(fallbackPosition.x, fallbackPosition.y, transform.position.z);
            return true;
        }
    }
}
