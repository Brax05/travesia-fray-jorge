using UnityEngine;

namespace TravesiaACasa.Rooms
{
    /// <summary>
    /// Mantiene al jugador dentro del fondo de la room en la que está,
    /// para que no se salga por los bordes y quede caminando sobre el
    /// vacío que hay entre un escenario y el siguiente.
    ///
    /// El recorte se hace sobre el Rigidbody2D (no sobre el Transform)
    /// para no pelearse con la física, y descuenta el tamaño del collider
    /// del jugador, así el sprite entero queda adentro y no medio cuerpo.
    ///
    /// Ojo al mover los RoomExitPoint: si un trigger de salida queda por
    /// fuera del fondo, el jugador ya no puede alcanzarlo y esa room
    /// queda sin salida. Los exits verticales están a 3.8 del centro
    /// justamente para entrar en el fondo más bajo (el arte viejo de las
    /// rooms 7-9, de 8.78 de alto).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlayerRoomBounds : MonoBehaviour
    {
        [Tooltip("Nombre del hijo de cada room que hace de fondo.")]
        [SerializeField] private string backgroundName = RoomBackgroundLocator.DefaultName;

        [Tooltip("Margen extra hacia adentro del borde del fondo, en unidades.")]
        [SerializeField] private float margin = 0f;

        private Collider2D playerCollider;
        private Rigidbody2D body;

        private void Awake()
        {
            playerCollider = GetComponent<Collider2D>();
            body = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (RoomGraphManager.Instance != null && RoomGraphManager.Instance.IsTransitioning)
                return;

            RoomNode current = RoomGraphManager.Instance != null ? RoomGraphManager.Instance.CurrentNode : null;
            if (current == null) return;

            SpriteRenderer background = RoomBackgroundLocator.Find(current.testWorldPosition, backgroundName);
            if (background == null) return;

            Vector3 position = body != null ? (Vector3)body.position : transform.position;

            // El collider puede estar desplazado respecto del transform: se
            // recorta el centro del collider y después se traduce de vuelta.
            Vector3 colliderOffset = playerCollider.bounds.center - position;
            Vector3 colliderCenter = position + colliderOffset;

            Bounds limits = background.bounds;
            Vector3 extents = playerCollider.bounds.extents + Vector3.one * margin;

            colliderCenter.x = ClampAxis(colliderCenter.x, limits.min.x + extents.x, limits.max.x - extents.x, limits.center.x);
            colliderCenter.y = ClampAxis(colliderCenter.y, limits.min.y + extents.y, limits.max.y - extents.y, limits.center.y);

            Vector3 clamped = colliderCenter - colliderOffset;
            if (clamped == position) return;

            if (body != null)
            {
                body.position = clamped;
                // Sin esto el jugador sigue empujando contra el borde y la
                // posición vibra entre el recorte y lo que integra la física.
                Vector2 velocity = body.linearVelocity;
                if (!Mathf.Approximately(clamped.x, position.x)) velocity.x = 0f;
                if (!Mathf.Approximately(clamped.y, position.y)) velocity.y = 0f;
                body.linearVelocity = velocity;
            }
            else
            {
                transform.position = clamped;
            }
        }

        /// <summary>Si el jugador no cabe en ese eje, se lo centra en vez de invertir los límites.</summary>
        private static float ClampAxis(float value, float min, float max, float fallback)
            => min <= max ? Mathf.Clamp(value, min, max) : fallback;
    }
}
