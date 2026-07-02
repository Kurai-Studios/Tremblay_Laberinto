using UnityEngine;
using System.Collections.Generic;

public class RuleManager : MonoBehaviour
{
    [Header("Reglas de Colocacion")]
    [Tooltip("Array de reglas que se aplicaran durante la generacion")]
    public PlacementRule[] placementRules;

    [Header("Configuracion de Cilindro")]
    public float cylinderRadius = 5f;
    public float levelHeight = 3f;

    // Lista de plataformas ya colocadas (para verificar reglas)
    private List<PlatformData> placedPlatforms = new List<PlatformData>();

    // Estadisticas de reglas
    private Dictionary<string, int> rulePassCount = new Dictionary<string, int>();
    private Dictionary<string, int> ruleFailCount = new Dictionary<string, int>();

    // Verifica si una plataforma puede ser colocada en la posicion dada
    // Revisa TODAS las reglas activas
    public bool CanPlacePlatform(GameObject prefab, int level, int index, TowerPlatform[,] allPlatforms)
    {
        if (placementRules == null || placementRules.Length == 0)
        {
            Debug.Log("Sin reglas: plataforma permitida");
            return true;
        }

        // Inicializar estadisticas si es necesario
        if (rulePassCount.Count == 0)
        {
            foreach (var rule in placementRules)
            {
                if (rule != null && !string.IsNullOrEmpty(rule.ruleName))
                {
                    rulePassCount[rule.ruleName] = 0;
                    ruleFailCount[rule.ruleName] = 0;
                }
            }
        }

        // Verificar cada regla
        foreach (var rule in placementRules)
        {
            // Saltar reglas nulas o inactivas
            if (rule == null)
            {
                Debug.LogWarning("Regla nula encontrada en el array");
                continue;
            }

            if (!rule.isActive)
            {
                Debug.Log($"Regla '{rule.ruleName}' inactiva, saltando");
                continue;
            }

            // Actualizar el radio del cilindro en la regla (si es del tipo que lo usa)
            if (rule is MustBeNearPlatform nearRule)
            {
                nearRule.cylinderRadius = cylinderRadius;
            }
            /*else if (rule is CannotBeNearPlatform cannotRule)
            {
                cannotRule.cylinderRadius = cylinderRadius;
            }*/

            // Verificar la regla
            bool canPlace = rule.CanPlace(prefab, level, index, placedPlatforms, allPlatforms);

            // Actualizar estadisticas
            if (canPlace)
            {
                rulePassCount[rule.ruleName] = rulePassCount.ContainsKey(rule.ruleName) ?
                                               rulePassCount[rule.ruleName] + 1 : 1;

                Debug.Log($"Regla '{rule.ruleName}' APROBADA para {prefab?.name ?? "null"} " + $"en nivel {level}, índice {index}");
                
            }
            else
            {
                ruleFailCount[rule.ruleName] = ruleFailCount.ContainsKey(rule.ruleName) ?
                                               ruleFailCount[rule.ruleName] + 1 : 1;

                
                Debug.Log($"Regla '{rule.ruleName}' RECHAZADA para {prefab?.name ?? "null"} " + $"en nivel {level}, índice {index}");
                

                return false;  // Una regla falla = la plataforma no se coloca
            }
        }

        return true; // Todas las reglas aprobadas
    }

    // Registra una plataforma que fue colocada exitosamente
    // Esto permite que las reglas revisen plataformas ya colocadas
    public void RegisterPlacedPlatform(GameObject prefab, int level, int index, float angle, Vector3 position)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Intento de registrar plataforma nula");
            return;
        }

        PlatformData data = new PlatformData(prefab, level, index, angle, position);
        placedPlatforms.Add(data);


        Debug.Log($"Plataforma registrada: {prefab.name} en nivel {level}, " + $"índice {index}, ángulo {angle:F1}°, posición {position}");   
    }

    // Reinicia el sistema de reglas para una nueva generacion
    public void ResetRules()
    {
        placedPlatforms.Clear();

        // Reiniciar estadisticas
        rulePassCount.Clear();
        ruleFailCount.Clear();

        Debug.Log("Reglas reiniciadas para nueva generacion");
    }

    // Actualiza la configuracion del cilindro desde el generador
    public void UpdateCylinderConfig(float radius, float height)
    {
        cylinderRadius = radius;
        levelHeight = height;

            Debug.Log($"Configuración de cilindro actualizada: Radio {radius}, Altura de nivel {height}");
    }

    // Obtiene todas las plataformas colocadas (para depuracion)
    public List<PlatformData> GetPlacedPlatforms()
    {
        return placedPlatforms;
    }

    // Obtiene estadisticas de las reglas (para depuracion)
    public string GetRuleStatistics()
    {
        string stats = "=== ESTADISTICAS DE REGLAS ===\n";

        foreach (var rule in placementRules)
        {
            if (rule == null) continue;

            string name = rule.ruleName;
            int passes = rulePassCount.ContainsKey(name) ? rulePassCount[name] : 0;
            int fails = ruleFailCount.ContainsKey(name) ? ruleFailCount[name] : 0;
            int total = passes + fails;
            float percentage = total > 0 ? (passes / (float)total) * 100f : 0f;

            stats += $"{name}: {passes} aprobadas, {fails} rechazadas ({percentage:F1}% exito)\n";
        }

        return stats;
    }

    private void OnDrawGizmos()
    {
        // Dibujar puntos de plataformas colocadas
        Gizmos.color = Color.cyan;
        foreach (var platform in placedPlatforms)
        {
            Gizmos.DrawWireSphere(platform.position, 0.5f);
        }

        // Dibujar un circulo en cada nivel con plataformas
        if (placedPlatforms.Count > 0)
        {
            HashSet<int> levelsWithPlatforms = new HashSet<int>();
            foreach (var platform in placedPlatforms)
            {
                levelsWithPlatforms.Add(platform.level);
            }

            Gizmos.color = Color.yellow;
            foreach (int level in levelsWithPlatforms)
            {
                float y = level * levelHeight;
                Vector3 center = new Vector3(0, y, 0);
                Gizmos.DrawWireSphere(center, cylinderRadius);
            }
        }
    }

    // Verifica que todas las reglas esten configuradas correctamente.
    public bool ValidateRules()
    {
        if (placementRules == null || placementRules.Length == 0)
        {
            Debug.LogWarning("No hay reglas configuradas");
            return true;  // No hay reglas = valido
        }

        bool allValid = true;

        for (int i = 0; i < placementRules.Length; i++)
        {
            var rule = placementRules[i];

            if (rule == null)
            {
                Debug.LogError($"Regla en posición {i} es NULL");
                allValid = false;
                continue;
            }

            if (string.IsNullOrEmpty(rule.ruleName))
            {
                Debug.LogWarning($"Regla en posición {i} no tiene nombre");
            }

            // Verificar reglas específicas
            if (rule is MustBeNearPlatform nearRule && nearRule.targetPrefab == null)
            {
                Debug.LogWarning($"Regla '{rule.ruleName}' no tiene Target Prefab asignado");
            }

            /*if (rule is CannotBeNearPlatform cannotRule && cannotRule.forbiddenPrefab == null)
            {
                Debug.LogWarning($"Regla '{rule.ruleName}' no tiene Forbidden Prefab asignado");
            }*/
        }

        return allValid;
    }
}
