using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TravesiaACasa.Rooms
{
    /// <summary>
    /// Encuentra el sprite de fondo de una room a partir de su centro.
    ///
    /// Se busca por NOMBRE y por cercanía en vez de por jerarquía para no
    /// atar nada a cómo estén organizadas las rooms en la escena (los
    /// RoomNode son assets y no guardan una referencia al GameObject de
    /// su room). Lo usan la cámara, para encuadrar, y el límite de
    /// movimiento del jugador, para no dejarlo salirse.
    /// </summary>
    public static class RoomBackgroundLocator
    {
        public const string DefaultName = "Fondo";

        private static readonly List<SpriteRenderer> cache = new List<SpriteRenderer>();
        private static string cachedName;

        /// <summary>Fondo cuyo centro queda más cerca de <paramref name="worldPosition"/>.</summary>
        public static SpriteRenderer Find(Vector3 worldPosition, string backgroundName = DefaultName)
        {
            // El cache se rehace si cambió el nombre buscado, si está vacío
            // o si quedó con referencias muertas (escena recargada en el Editor).
            if (cachedName != backgroundName || cache.Count == 0 || cache.Any(b => b == null))
                Refresh(backgroundName);

            SpriteRenderer best = null;
            float bestDistance = float.MaxValue;
            foreach (SpriteRenderer candidate in cache)
            {
                if (candidate == null) continue;
                float distance = ((Vector2)(candidate.bounds.center - worldPosition)).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
            return best;
        }

        /// <summary>Vuelve a escanear la escena. Llamar si se crean rooms en runtime.</summary>
        public static void Refresh(string backgroundName = DefaultName)
        {
            cachedName = backgroundName;
            cache.Clear();
            cache.AddRange(
                Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(sr => sr.name == backgroundName && sr.sprite != null));
        }
    }
}
