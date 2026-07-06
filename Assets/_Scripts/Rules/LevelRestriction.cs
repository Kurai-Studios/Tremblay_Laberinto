using UnityEngine;
using System.Collections.Generic;

// Regla: Restringe la aparición de una plataforma a ciertos niveles.
[CreateAssetMenu(
    fileName = "LevelRestriction",
    menuName = "Platform Rules/Level Restriction"
)]

public class LevelRestriction : PlacementRule
{
    [Header("Plataforma Afectada")]
    public GameObject targetPrefab;

    [Tooltip("Si esta vacio, la regla aplica a TODAS las plataformas")]
    public bool applyToAll = false;

    [Header("Rango de Niveles")]
    public int minLevel = 0;
    public int maxLevel = 10;

    [Header("Filtros Adicionales")]
    [Tooltip("Si se activa, solo aparece en niveles pares (0, 2, 4, 6...)")]
    public bool onlyEvenLevels = false;

    [Tooltip("Si se activa, solo aparece en niveles impares (1, 3, 5, 7...)")]
    public bool onlyOddLevels = false;

    [Header("Comportamiento cuando falla")]
    [Tooltip("Si no cumple las condiciones, se usa el prefab por defecto en lugar de rechazar")]
    public bool useDefaultInstead = false;

    public override bool CanPlace(
        GameObject prefab,
        int level,
        int index,
        List<PlatformData> existingPlatforms,
        TowerPlatform[,] allPlatforms)
    {
        if (!isActive) return true;

        // 1. VERIFICAR RANGO DE NIVELES
        string targetName;

        // Si applyToAll esta activado, afecta a TODOS
        if (applyToAll)
        {
            targetName = "TODOS";
        }

        // Si targetPrefab esta asignado, afecta solo a ese prefab
        else if (targetPrefab != null)
        {
            targetName = targetPrefab.name;

            // Si el prefab actual no es el target, esta regla no aplica
            if (prefab != targetPrefab)
            {
                return true;  // No aplica a este prefab
            }
        }
        // Si targetPrefab esta vacío y applyToAll está false, aplicar a TODOS (compatibilidad)
        else
        {
            targetName = "TODOS (sin target)";

            if (showDebugLogs) Debug.Log($"Regla '{ruleName}': targetPrefab vacío, aplicando a TODOS los prefabs");
        }

        // 2. VERIFICAR RANGO DE NIVELES
        if (level < minLevel || level > maxLevel)
        {

            if (showDebugLogs) Debug.Log($"Regla '{ruleName}': Nivel {level} fuera de rango " + $"({minLevel}-{maxLevel}) para {targetName}");

            if (useDefaultInstead)
            {

                if (showDebugLogs) Debug.Log($"Regla '{ruleName}': Usando prefab por defecto en nivel {level}");
                
                return true;
            }

            return false;
        }

        // 3. VERIFICAR FILTRO DE NIVELES PARES
        if (onlyEvenLevels && level % 2 != 0)
        {

            if (showDebugLogs) Debug.Log($"Regla '{ruleName}': Nivel {level} no es par para {targetName}");

            if (useDefaultInstead)
            {
                if (showDebugLogs) Debug.Log($"Regla '{ruleName}': Usando prefab por defecto en nivel {level}");

                return true;
            }

            return false;
        }

        // 4. VERIFICAR FILTRO DE NIVELES IMPARES
        if (onlyOddLevels && level % 2 == 0)
        {

            if (showDebugLogs) Debug.Log($"Regla '{ruleName}': Nivel {level} no es impar para {targetName}");          

            if (useDefaultInstead)
            {
                if (showDebugLogs) Debug.Log($"Regla '{ruleName}': Usando prefab por defecto en nivel {level}");

                return true;
            }

            return false;
        }

        // 5. TODAS LAS CONDICIONES CUMPLIDAS
        string details = $"nivel {level}";

        if (onlyEvenLevels) details += " (par)";
        if (onlyOddLevels) details += " (impar)";

        if (showDebugLogs) Debug.Log($"Regla '{ruleName}': Aprobada para {targetName} en {details}");

        return true;
    }

    public override string GetDescription()
    {
        string targetName;

        if (applyToAll)
            targetName = "TODOS";
        else if (targetPrefab != null)
            targetName = targetPrefab.name;
        else
            targetName = "TODOS (sin target)";

        string description = $"{ruleName}: {targetName} en niveles {minLevel}-{maxLevel}";

        if (onlyEvenLevels)
            description += " (solo pares)";
        else if (onlyOddLevels)
            description += " (solo impares)";

        if (useDefaultInstead)
            description += " [usa default si falla]";

        return description;
    }
}
