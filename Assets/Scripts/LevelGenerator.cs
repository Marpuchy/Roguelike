using UnityEngine;

/// <summary>
/// LevelGenerator simple: calcula tamaño en función del número de nivel.
/// Mantiene semilla opcional para reproducibilidad.
/// </summary>
public class LevelGenerator : MonoBehaviour
{
    public int baseSize = 8;
    public int maxSize = 255;

    // devuelve width,height (square) calculados para el nivel
    public Vector2Int GetSizeForLevel(int level)
    {
        int levelClamped = Mathf.Max(1, level);
        int size = baseSize + (levelClamped - 1);
        size = Mathf.Clamp(size, 3, maxSize);
        return new Vector2Int(size, size);
    }

    // si prefieres, podrías exponer GenerateMap(...) que devuelva una matriz bool[,] u otra estructura
}