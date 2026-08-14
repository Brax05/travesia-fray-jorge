/**
 * Archivo: BackgroundMusicPlayer.cs
 * Proposito: Mantener la musica de fondo del menu y aplicar el volumen configurado.
 * Responsabilidades: Conservar una instancia persistente, sincronizar volumen con SettingsManager y liberar eventos al destruirse.
 *
 */
using UnityEngine;

namespace TravesiaACasa.Menu
{
    /// <summary>
    /// Reproduce la música ambiental durante toda la aplicación. Este
    /// componente se crea automáticamente junto a SettingsManager, por lo
    /// que la pista no se reinicia cuando cambia la escena.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class BackgroundMusicPlayer : MonoBehaviour
    {
        private const string MusicResourcePath = "Audio/TheMountainKid";
        private const float VolumeCurveExponent = 2f;

        private AudioSource musicSource;
        private SettingsManager settings;

        private void Awake()
        {
            musicSource = GetComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.priority = 0;

            AudioClip musicClip = Resources.Load<AudioClip>(MusicResourcePath);
            if (musicClip == null)
            {
                Debug.LogError($"No se encontró la música en Resources/{MusicResourcePath}.");
                enabled = false;
                return;
            }

            musicSource.clip = musicClip;

            settings = SettingsManager.Instance;
            if (settings != null)
            {
                settings.Changed += ApplyVolume;
                ApplyVolume();
            }

            musicSource.Play();
        }

        private void OnDestroy()
        {
            if (settings != null)
                settings.Changed -= ApplyVolume;
        }

        private void ApplyVolume()
        {
            if (musicSource != null && settings != null)
            {
                float sliderValue = Mathf.Clamp01(settings.AmbienteVolume);

                // Una curva cuadrática entrega más recorrido útil en la zona
                // baja: 50 % de la barra equivale a 25 % de volumen y 25 %
                // de la barra equivale a 6,25 % de volumen.
                musicSource.volume = Mathf.Pow(sliderValue, VolumeCurveExponent);
            }
        }
    }
}
