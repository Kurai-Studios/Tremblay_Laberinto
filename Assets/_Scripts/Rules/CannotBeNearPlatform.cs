using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Regla: La plataforma NO puede estar cerca de otro tipo específico de plataforma.
/// Ejemplo: "Las plataformas trampa no pueden estar cerca de otras trampas"
/// </summary>
[CreateAssetMenu(
    fileName = "CannotBeNearPlatform",
    menuName = "Platform Rules/Cannot Be Near Platform"
)]
public class CannotBeNearPlatform : PlacementRule
{
    [Header("Plataforma Prohibida")]
    [Tooltip("El tipo de plataforma que NO debe estar cerca")]
    public GameObject forbiddenPrefab;

    [Tooltip("Distancia mínima requerida entre plataformas")]
    public float minDistance = 8f;

    [Header("Niveles a Revisar")]
    [Tooltip("Revisar plataformas en el mismo nivel")]
    public bool checkSameLevel = true;

    [Tooltip("Revisar plataformas en niveles adyacentes")]
    public bool checkAdjacentLevels = true;

    [Tooltip("Cuántos niveles arriba/abajo revisar (si checkAdjacentLevels está activo)")]
    public int adjacentLevelsToCheck = 1;

    [Header("Radio del Cilindro (para cálculos)")]
    [Tooltip("Radio del cilindro actual (se asigna automáticamente)")]
    public float cylinderRadius = 5f;

    public override bool CanPlace(
        GameObject prefab,
        int level,
        int index,
        List<PlatformData> existingPlatforms,
        TowerPlatform[,] allPlatforms)
    {
        // Si la regla está desactivada, permitir todo
        if (!isActive) return true;

        // Validar que el prefab prohibido esté asignado
        if (forbiddenPrefab == null)
        {
            if (showDebugLogs) Debug.LogWarning($"Regla '{ruleName}': forbiddenPrefab no asignado");
            return true;
        }

        // Variable para el nombre del prefab prohibido
        string forbiddenName = forbiddenPrefab.name;

        // Revisar cada plataforma ya colocada
        foreach (var platform in existingPlatforms)
        {
            // Verificar si es del tipo prohibido
            if (platform.prefab != forbiddenPrefab) continue;

            // Verificar si está en el mismo nivel
            if (checkSameLevel && platform.level == level)
            {
                // Calcular distancia angular en el mismo nivel
                float currentAngle = GetPlatformAngle(allPlatforms, level, index);
                float angleDiff = Mathf.Abs(platform.angle - currentAngle);
                angleDiff = Mathf.Min(angleDiff, 360f - angleDiff);

                // Convertir a distancia en unidades
                float distance = angleDiff * Mathf.Deg2Rad * cylinderRadius;

                // Si está demasiado cerca, rechazar
                if (distance < minDistance)
                {
                    if (showDebugLogs)
                    {
                        Debug.Log($"❌ Regla '{ruleName}': {prefab?.name ?? "null"} " +
                                 $"demasiado cerca de {forbiddenName} " +
                                 $"(distancia {distance:F2} < {minDistance}) en nivel {level}");
                    }
                    return false;
                }
            }

            // Verificar niveles adyacentes
            if (checkAdjacentLevels)
            {
                for (int offset = 1; offset <= adjacentLevelsToCheck; offset++)
                {
                    if (platform.level == level + offset || platform.level == level - offset)
                    {
                        // Distancia vertical
                        float verticalDistance = Mathf.Abs((level - platform.level) * GetLevelHeight());

                        // Distancia horizontal (si están en ángulos similares)
                        float currentAngle = GetPlatformAngle(allPlatforms, level, index);
                        float angleDiff = Mathf.Abs(platform.angle - currentAngle);
                        angleDiff = Mathf.Min(angleDiff, 360f - angleDiff);
                        float horizontalDistance = angleDiff * Mathf.Deg2Rad * cylinderRadius;

                        // Distancia total (combinación de vertical y horizontal)
                        float totalDistance = Mathf.Sqrt(verticalDistance * verticalDistance +
                                                        horizontalDistance * horizontalDistance);

                        // Si está demasiado cerca, rechazar
                        if (totalDistance < minDistance)
                        {
                            if (showDebugLogs)
                            {
                                Debug.Log($"❌ Regla '{ruleName}': {prefab?.name ?? "null"} " +
                                         $"demasiado cerca de {forbiddenName} en nivel adyacente " +
                                         $"(distancia {totalDistance:F2} < {minDistance})");
                            }
                            return false;
                        }
                    }
                }
            }
        }

        // Si no se encontró ninguna plataforma prohibida cerca, permitir
        if (showDebugLogs)
        {
            Debug.Log($"✅ Regla '{ruleName}': {prefab?.name ?? "null"} " +
                     $"aprobada (sin {forbiddenName} cerca) en nivel {level}");
        }

        return true;
    }

    /// <summary>
    /// Obtiene la altura de los niveles (valor por defecto o desde el generador)
    /// </summary>
    private float GetLevelHeight()
    {
        // Este valor debería venir del PlatformGenerator
        // Por ahora usamos un valor por defecto
        return 3f;
    }

    public override string GetDescription()
    {
        string forbiddenName = forbiddenPrefab != null ? forbiddenPrefab.name : "NINGUNO";
        string levels = checkSameLevel ? "mismo nivel" : "";
        if (checkAdjacentLevels) levels += (levels.Length > 0 ? " y " : "") + $"hasta {adjacentLevelsToCheck} niveles adyacentes";
        if (levels.Length == 0) levels = "sin restricción de niveles";

        return $"{ruleName}: No puede estar cerca de {forbiddenName} " +
               $"(distancia mínima {minDistance}, {levels})";
    }
}