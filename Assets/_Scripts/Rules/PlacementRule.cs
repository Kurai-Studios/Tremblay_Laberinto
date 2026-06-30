using UnityEngine;
using System.Collections.Generic;

// Clase base para todas las reglas de colocacion de plataformas.
public abstract class PlacementRule : ScriptableObject
{
    [Header("Configuración General")]
    [Tooltip("Nombre descriptivo de la regla")]
    public string ruleName = "Nueva Regla";
    public bool isActive = true;

    // Verifica si la plataforma puede ser colocada en la posición dada.
    /// <param name="prefab">El prefab de la plataforma que se quiere colocar</param>
    /// <param name="level">Nivel donde se quiere colocar</param>
    /// <param name="index">Indice dentro del nivel</param>
    /// <param name="existingPlatforms">Lista de plataformas ya colocadas</param>
    /// <param name="allPlatforms">Matriz completa de todas las plataformas</param>
    /// <returns>True si se puede colocar, False si no</returns>
    
    public abstract bool CanPlace(
        GameObject prefab,
        int level,
        int index,
        List<PlatformData> existingPlatforms,
        TowerPlatform[,] allPlatforms
    );

    // Obtiene una descripcion de la regla para depuracion
    public virtual string GetDescription()
    {
        return $"{ruleName}: Regla base (sin descripcion especifica)";
    }

    // Metodo auxiliar para calcular distancia entre plataformas
    protected float GetDistanceBetweenAngles(float angle1, float angle2, float radius)
    {
        float angleDiff = Mathf.Abs(angle1 - angle2);
        angleDiff = Mathf.Min(angleDiff, 360f - angleDiff);
        return angleDiff * Mathf.Deg2Rad * radius;
    }

    // Metodo auxiliar para verificar si dos plataformas estan en el mismo nivel
    protected bool IsSameLevel(int level1, int level2)
    {
        return level1 == level2;
    }

    // Metodo auxiliar para verificar si dos plataformas estan en niveles adyacentes
    protected bool IsAdjacentLevel(int level1, int level2, int maxOffset = 1)
    {
        int difference = Mathf.Abs(level1 - level2);
        return difference > 0 && difference <= maxOffset;
    }

    // Metodo auxiliar para obtener el angulo de una plataforma desde la matriz
    protected float GetPlatformAngle(TowerPlatform[,] allPlatforms, int level, int index)
    {
        if (allPlatforms == null || level < 0 || level >= allPlatforms.GetLength(0) ||
            index < 0 || index >= allPlatforms.GetLength(1))
        {
            return 0f;
        }

        TowerPlatform platform = allPlatforms[level, index];
        return platform != null ? platform.angle : 0f;
    }

    // Metodo auxiliar para verificar si un prefab es de un tipo especifico
    protected bool IsPrefabType(GameObject prefab, GameObject targetPrefab)
    {
        if (prefab == null || targetPrefab == null) return false;
        return prefab == targetPrefab;
    }
}

// Estructura para almacenar informacion de plataformas ya colocadas
public struct PlatformData
{
    public GameObject prefab;      // El prefab de la plataforma
    public int level;              // Nivel donde esta
    public int index;              // Indice dentro del nivel
    public float angle;            // Angulo alrededor del cilindro
    public Vector3 position;       // Posicion en el mundo

    public PlatformData(GameObject prefab, int level, int index, float angle, Vector3 position)
    {
        this.prefab = prefab;
        this.level = level;
        this.index = index;
        this.angle = angle;
        this.position = position;
    }

    public override string ToString()
    {
        return $"{prefab?.name ?? "null"} - Nivel {level}, Índice {index}, Ángulo {angle}°";
    }
}
