using UnityEngine;

namespace TravesiaACasa.Rooms
{
    /// <summary>
    /// Pared o barrera invisible 2D (BoxCollider2D sin SpriteRenderer).
    /// El jugador colisiona y no puede traspasarlo. En el Editor muestra una guía visual
    /// semitransparente para posicionarlo fácilmente sin estorbar en el juego.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class InvisibleWall2D : MonoBehaviour
    {
        [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.4f, 0.35f);

        private BoxCollider2D boxCollider;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                boxCollider.isTrigger = false;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider2D>();

            if (boxCollider == null) return;

            Gizmos.color = gizmoColor;
            Vector3 center = transform.TransformPoint(boxCollider.offset);
            Vector3 size = Vector3.Scale(boxCollider.size, transform.lossyScale);
            Gizmos.DrawCube(center, size);

            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.9f);
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
