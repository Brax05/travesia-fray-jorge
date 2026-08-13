using UnityEditor;
using UnityEngine;

namespace TravesiaACasa.Editor
{
    /// <summary>
    /// Importa la hoja 4x2 de AveNegra como ocho cuadros de caminata.
    /// Los nombres mantienen el orden visual: izquierda a derecha en la
    /// fila superior y luego izquierda a derecha en la fila inferior.
    /// </summary>
    public sealed class AveNegraWalkSpriteImporter : AssetPostprocessor
    {
        private const string AssetPath = "Assets/Resources/Animations/AveNegraWalk-generated.png";
        private const int Columns = 4;
        private const int Rows = 2;
        private const int FrameWidth = 443;
        private const int FrameHeight = 443;

        // El generador dejó espacios distintos entre columnas. Los pivotes X
        // fijan el centro real del torso; así la parte superior permanece
        // inmóvil y solo cambian las patas.
        private static readonly float[] BodyPivotX =
        {
            0.6524f, 0.5914f, 0.4887f, 0.3713f,
            0.6524f, 0.5914f, 0.4887f, 0.3713f
        };

        // Los pivotes Y compensan la ubicación de cada fila y mantienen los
        // pies sobre la misma línea de suelo durante todo el ciclo.
        private static readonly float[] GroundedPivotY =
        {
            0.4151f, 0.4129f, 0.4106f, 0.4129f,
            0.5483f, 0.5438f, 0.5438f, 0.5438f
        };

        private void OnPreprocessTexture()
        {
            if (assetPath != AssetPath)
                return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            // 27.7 PPU conserva el mismo tamaño visual del AveNegra original
            // (878 px a 100 PPU frente a ~243 px por cuadro en esta hoja).
            importer.spritePixelsPerUnit = 27.7f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            var frames = new SpriteMetaData[Columns * Rows];
            for (int index = 0; index < frames.Length; index++)
            {
                int column = index % Columns;
                int rowFromTop = index / Columns;
                int unityRow = Rows - 1 - rowFromTop;

                frames[index] = new SpriteMetaData
                {
                    name = $"AveNegraWalk_{index:00}",
                    rect = new Rect(
                        column * FrameWidth,
                        unityRow * FrameHeight,
                        FrameWidth,
                        FrameHeight),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(BodyPivotX[index], GroundedPivotY[index])
                };
            }

#pragma warning disable CS0618
            importer.spritesheet = frames;
#pragma warning restore CS0618
        }
    }
}
