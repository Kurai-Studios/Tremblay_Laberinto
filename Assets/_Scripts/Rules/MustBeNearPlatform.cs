using UnityEngine;
using System.Collections.Generic;

// Regla: La plataforma debe estar cerca de al menos N plataformas de un tipo especifico.
[CreateAssetMenu(
    fileName = "MustBeNearPlatform",
    menuName = "Platform Rules/Must Be Near Platform"
)]

public class MustBeNearPlatform : PlacementRule
{
    [Header("Configuracion de Vecindad")]
    [Tooltip("El tipo de plataforma que debe estar cerca")]
    public GameObject targetPrefab;
    [Tooltip("Distancia maxima para considerar que esta 'cerca'")]
    public float maxDistance = 5f;
    [Tooltip("Numero minimo de plataformas del tipo targetPrefab que deben estar cerca")]
    public int minNearbyCount = 1;

    [Header("Niveles a Revisar")]
    [Tooltip("Revisar plataformas en el mismo nivel")]
    public bool checkSameLevel = true;
    [Tooltip("Revisar plataformas en niveles adyacentes")]
    public bool checkAdjacentLevels = true;
    [Tooltip("Cuantos niveles arriba/abajo revisar (si checkAdjacentLevels esta activo)")]
    public int adjacentLevelsToCheck = 1;

    [Header("Radio del Cilindro (para calculos)")]
    [Tooltip("Radio del cilindro actual (se asigna automaticamente)")]
    public float cylinderRadius = 5f;

    public override bool CanPlace(
        GameObject prefab,
        int level,
        int index,
        List<PlatformData> existingPlatforms,
        TowerPlatform[,] allPlatforms)
    {
        // Si la regla esta desactivada, permitir todo
        if (!isActive) return true;

        // Validar que el prefab objetivo este asignado
        if (targetPrefab == null)
        {
            Debug.LogWarning($"Regla '{ruleName}': targetPrefab no asignado");
            return true;  // Si no hay objetivo, permitir
        }

        // Contar cuantas plataformas del tipo targetPrefab estan cerca
        int nearbyCount = 0;

        foreach (var platform in existingPlatforms)
        {
            // Verificar si es del tipo que buscamos
            if (platform.prefab != targetPrefab) continue;

            // Verificar si esta en el mismo nivel
            if (checkSameLevel && platform.level == level)
            {
                // Calcular distancia angular en el mismo nivel
                float angleDiff = Mathf.Abs(platform.angle - GetPlatformAngle(allPlatforms, level, index));
                angleDiff = Mathf.Min(angleDiff, 360f - angleDiff);

                // Convertir a distancia en unidades
                float distance = angleDiff * Mathf.Deg2Rad * cylinderRadius;

                if (distance <= maxDistance)
                {
                    nearbyCount++;
                    Debug.Log($"Regla '{ruleName}': Encontrada plataforma {targetPrefab.name} cerca (distancia {distance:F2})");
                }
            }

            // Verificar niveles adyacentes
            if (checkAdjacentLevels)
            {
                for (int offset = 1; offset <= adjacentLevelsToCheck; offset++)
                {
                    if (platform.level == level + offset || platform.level == level - offset)
                    {
                        // Para niveles adyacentes, usamos distancia vertical principalmente
                        float verticalDistance = Mathf.Abs((level - platform.level) * GetLevelHeight());

                        // Tambien considerar distancia horizontal (si estan en angulos similares)
                        float angleDiff = Mathf.Abs(platform.angle - GetPlatformAngle(allPlatforms, level, index));
                        angleDiff = Mathf.Min(angleDiff, 360f - angleDiff);
                        float horizontalDistance = angleDiff * Mathf.Deg2Rad * cylinderRadius;

                        // Distancia total (combinacion de vertical y horizontal)
                        float totalDistance = Mathf.Sqrt(verticalDistance * verticalDistance +
                                                        horizontalDistance * horizontalDistance);

                        if (totalDistance <= maxDistance)
                        {
                            nearbyCount++;
                            Debug.Log($"Regla '{ruleName}': Encontrada plataforma {targetPrefab.name} en nivel adyacente (distancia {totalDistance:F2})");
                        }
                    }
                }
            }
        }

        // Verificar si cumple con el minimo requerido
        bool result = nearbyCount >= minNearbyCount;

        
        
            Debug.Log($"Regla '{ruleName}': {prefab.name} en nivel {level} - " +
                     $"Cerca de {nearbyCount} plataformas {targetPrefab.name} " +
                     $"(mínimo {minNearbyCount}) => {(result ? "APROBADA" : "RECHAZADA")}");
        

        return result;
    }

    // Obtiene la altura de los niveles (valor por defecto o desde el generador)
    private float GetLevelHeight()
    {
        // Este valor deberia venir del PlatformGenerator
        // Por ahora usamos un valor por defecto
        return 3f;
    }

    public override string GetDescription()
    {
        string targetName = targetPrefab != null ? targetPrefab.name : "NINGUNO";
        string levels = checkSameLevel ? "mismo nivel" : "";
        if (checkAdjacentLevels) levels += (levels.Length > 0 ? " y " : "") + $"hasta {adjacentLevelsToCheck} niveles adyacentes";
        if (levels.Length == 0) levels = "sin restricción de niveles";

        return $"{ruleName}: Debe estar cerca de {targetName} " +
               $"(mínimo {minNearbyCount}, distancia {maxDistance}, {levels})";
    }
}
