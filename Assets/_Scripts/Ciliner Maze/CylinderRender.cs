using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

[System.Serializable]
public class PlatformRule
{
    [Header("Tipo de Plataforma")]
    public GameObject prefab;
    public string platformName = "Normal";

    [Header("Cantidades Exactas")]
    public int exactCount = 5;

    [Header("Restricciones (Opcional)")]
    [Tooltip("Si se activa, limita cuántas pueden aparecer por nivel")]
    public bool limitPerLevel = false;
    [Range(0, 10)]
    public int maxPerLevel = 1;

    [Header("Niveles Específicos (Opcional)")]
    [Tooltip("Si se activa, solo aparece en ciertos niveles")]
    public bool restrictToLevels = false;
    public int minLevel = 0;
    public int maxLevel = 10;

    // Contadores (se usan durante la generación)
    [HideInInspector] public int currentCount = 0;
    [HideInInspector] public int currentLevelCount = 0;
    [HideInInspector] public bool isSaturated = false;
}

public class CylinderRender : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CylinderPlatformGen platformGen;
    [SerializeField] RuleManager ruleManager;

    [Header("Platform Rules")]
    public PlatformRule[] platformRules;

    [Header("Platform Settings")]
    public float platformScale = 1f;
    public float heightOffset = 0f;  // Desplazamiento vertical desde la base del cilindro

    /*[Header("Debug")]
    public bool showGizmos = false;*/

    private List<PlatformRule> availableRules = new List<PlatformRule>();

    private void Start()
    {
        GenerateTower();
    }

    // Genera toda la torre con las plataformas
    public void GenerateTower()
    {
        // Validar referencias
        if (platformGen == null)
        {
            //Debug.LogError("PlatformGenerator no asignado en PlatformRenderer");
            return;
        }

        if (platformRules == null || platformRules.Length == 0)
        {
            //Debug.LogError("PlatformPrefab no asignado en PlatformRenderer");
            return;
        }

        ResetCounters();

        if (ruleManager != null)
        {
            ruleManager.ResetRules();

            float radius = platformGen.GetCylinderRadius();
            float height = platformGen.GetCylinderHeight();
            ruleManager.UpdateCylinderConfig(radius, height);
        }

        // Obtener los datos de las plataformas
        TowerPlatform[,] platforms = platformGen.GetPlatforms();

        // Obtener dimensiones del cilindro
        float cylinderRadius = platformGen.GetCylinderRadius();
        float cylinderHeight = platformGen.GetCylinderHeight();
        int totalLevels = platformGen.GetTotalLevels();

        Vector3 basePosition = platformGen.GetPlatformBasePosition();

        //Debug.Log($"Renderizando plataformas desde: {basePosition}");
        //Debug.Log($"Radio del cilindro: {cylinderRadius}");
        //Debug.Log($"Total de niveles: {platforms.GetLength(0)}");

        // Validar que hay datos
        if (platforms == null || platforms.Length == 0)
        {
            //Debug.LogWarning("No se generaron plataformas");
            return;
        }

        // Instanciar el cilindro seleccionado
        GameObject cylinder = platformGen.GetSelectedCylinder();
        if (cylinder != null)
        {
            GameObject cylinderInstance = Instantiate(cylinder, Vector3.zero, Quaternion.identity, transform);
            // El cilindro se instancia en el centro (0,0,0)
        }

        int totalPlatforms = CountTotalPlatforms(platforms);
        int totalAssigned = 0;

        foreach (var rule in platformRules)
        {
            totalAssigned += rule.exactCount;
        }

        // Verificar que el total de plataformas asignadas no exceda el total disponible
        if (totalAssigned > totalPlatforms)
        {
            //Debug.LogWarning($"¡Cuidado! Has asignado {totalAssigned} plataformas pero solo hay {totalPlatforms} espacios. Algunas no se podrán generar.");
        }
        else if (totalAssigned < totalPlatforms)
        {
            //Debug.Log($"Has asignado {totalAssigned} plataformas de {totalPlatforms} totales. El resto serán plataformas por defecto.");
        }

        // Loop a traves de todos los niveles y plataformas
        int levelCount = platforms.GetLength(0);
        int maxPlatformsPerLevel = platforms.GetLength(1);

        for (int level = 0; level < levelCount; level++)
        {

            ResetLevelCounters();

            for (int i = 0; i < maxPlatformsPerLevel; i++)
            {
                TowerPlatform platformData = platforms[level, i];
                if (platformData == null) continue;

                GameObject selectedPrefab = SelectPrefabWithRules(level, i, platforms);

                if (selectedPrefab == null)
                {
                    // Si no hay prefab seleccionado, usar el primero de la lista
                    selectedPrefab = platformRules[0].prefab;
                    //Debug.Log($"Usando prefab por defecto: {platformRules[0].platformName}");
                }

                GameObject newPlatform = Instantiate(selectedPrefab, transform);
                newPlatform.transform.localScale = Vector3.one * platformScale;

                CylinderPlatformObj platformCell = newPlatform.GetComponent<CylinderPlatformObj>();
                if (platformCell == null)
                {
                    //Debug.LogError($"PlatformPrefab no tiene componente PlatformCellObj");
                    continue;
                }

                
                platformCell.Init(platformData, cylinderRadius, heightOffset,
                                 platformGen.levelHeight, basePosition);

                if (ruleManager != null)
                {
                    ruleManager.RegisterPlacedPlatform(
                        selectedPrefab,
                        level,
                        i,
                        platformData.angle,
                        newPlatform.transform.position
                    );
                }

                UpdateCounters(selectedPrefab);
            }
        }

        Debug.Log($"=== ESTADÍSTICAS DE GENERACIÓN ===");
        foreach (var rule in platformRules)
        {
            Debug.Log($"{rule.platformName}: {rule.currentCount} / {rule.exactCount} plataformas generadas");
        }

        if (ruleManager != null)
        {
            Debug.Log(ruleManager.GetRuleStatistics());
        }

        //Debug.Log($"Torre generada: {levelCount} niveles, {CountTotalPlatforms(platforms)} plataformas totales");
    }

    GameObject SelectPrefabWithRules(int level, int index, TowerPlatform[,] allPlatforms)
    {
        List<GameObject> availablePrefabs = new List<GameObject>();

        foreach (var rule in platformRules)
        {
            if (rule.prefab == null) continue;
            if (rule.isSaturated) continue;

            if (rule.restrictToLevels)
            {
                if (level < rule.minLevel || level > rule.maxLevel) continue;
            }

            if (rule.limitPerLevel && rule.currentLevelCount >= rule.maxPerLevel) continue;

            availablePrefabs.Add(rule.prefab);
        }

        if (availablePrefabs.Count == 0) return null;

        for (int i = 0; i < availablePrefabs.Count * 2; i++)
        {
            int a = Random.Range(0, availablePrefabs.Count);
            int b = Random.Range(0, availablePrefabs.Count);
            GameObject temp = availablePrefabs[a];
            availablePrefabs[a] = availablePrefabs[b];
            availablePrefabs[b] = temp;
        }

        int maxAttempts = 50;
        int attempts = 0;

        while (attempts < maxAttempts && availablePrefabs.Count > 0)
        {
            attempts++;

            int randomIndex = Random.Range(0, availablePrefabs.Count);
            GameObject candidate = availablePrefabs[randomIndex];

            if (ruleManager == null || ruleManager.CanPlacePlatform(candidate, level, index, allPlatforms))
            {
                
                Debug.Log($"Prefab '{candidate.name}' seleccionado para nivel {level}, " +
                             $"índice {index} (intento {attempts})");
                
                return candidate;
            }
            else
            {
                availablePrefabs.RemoveAt(randomIndex);

                Debug.Log($"Prefab '{candidate.name}' rechazado por reglas en nivel {level}, " +
                             $"índice {index} (intento {attempts})");
            }
        }

        Debug.LogWarning($"No se encontró prefab que cumpla reglas en nivel {level}, índice {index}");
        
        return null;
    }

    void ResetCounters()
    {
        foreach (var rule in platformRules)
        {
            rule.currentCount = 0;
            rule.currentLevelCount = 0;
            rule.isSaturated = false;
        }
    }

    void ResetLevelCounters()
    {
        foreach (var rule in platformRules)
        {
            rule.currentLevelCount = 0;
        }
    }

    void UpdateCounters(GameObject prefab)
    {
        foreach (var rule in platformRules)
        {
            if (rule.prefab == prefab)
            {
                rule.currentCount++;
                rule.currentLevelCount++;

                // Verificar si ya alcanzó su límite
                if (rule.currentCount >= rule.exactCount)
                {
                    rule.isSaturated = true;
                    //Debug.Log($"✅ Regla '{rule.platformName}' completó su límite de {rule.exactCount}");
                }
                break;
            }
        }
    }

    // Cuenta el número total de plataformas
    int CountTotalPlatforms(TowerPlatform[,] platforms)
    {
        int count = 0;
        for (int level = 0; level < platforms.GetLength(0); level++)
        {
            for (int i = 0; i < platforms.GetLength(1); i++)
            {
                if (platforms[level, i] != null)
                    count++;
            }
        }
        return count;
    }

    // Limpia todas las plataformas generadas (para regenerar)
    public void ClearTower()
    {
        // Eliminar todos los hijos del PlatformRenderer
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        ResetCounters();

        if (ruleManager != null)
            ruleManager.ResetRules();
    }

    // Regenera la torre
    public void RegenerateTower()
    {
        ClearTower();
        GenerateTower();
    }

    // Métodos de depuración
    /*void OnDrawGizmos()
    {
        if (!showGizmos || platformGen == null) return;

        // Dibujar el cilindro (solo para visualización)
        float radius = platformGen.GetCylinderRadius();
        float height = platformGen.GetCylinderHeight();
        int levels = platformGen.GetTotalLevels();

        Gizmos.color = new Color(0, 1, 0, 0.1f);

        // Dibujar cilindro de referencia
        Vector3 center = new Vector3(0, height / 2f, 0);
        Gizmos.DrawWireCube(center, new Vector3(radius * 2f, height, radius * 2f));

        // Dibujar los niveles
        Gizmos.color = Color.yellow;
        for (int level = 0; level < levels; level++)
        {
            float y = heightOffset + (level * platformGen.levelHeight);
            Vector3 levelCenter = new Vector3(0, y, 0);
            Gizmos.DrawWireCube(levelCenter, new Vector3(radius * 2f, 0.1f, radius * 2f));
        }
    }*/
}
