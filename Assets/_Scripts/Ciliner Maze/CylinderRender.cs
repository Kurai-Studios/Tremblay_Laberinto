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
            Debug.LogError("PlatformGenerator no asignado en PlatformRenderer");
            return;
        }

        if (platformRules == null || platformRules.Length == 0)
        {
            Debug.LogError("PlatformPrefab no asignado en PlatformRenderer");
            return;
        }

        ResetCounters();

        // Obtener los datos de las plataformas
        TowerPlatform[,] platforms = platformGen.GetPlatforms();

        // Obtener dimensiones del cilindro
        float cylinderRadius = platformGen.GetCylinderRadius();
        float cylinderHeight = platformGen.GetCylinderHeight();
        int totalLevels = platformGen.GetTotalLevels();

        Vector3 basePosition = platformGen.GetPlatformBasePosition();

        Debug.Log($"Renderizando plataformas desde: {basePosition}");
        Debug.Log($"Radio del cilindro: {cylinderRadius}");
        Debug.Log($"Total de niveles: {platforms.GetLength(0)}");

        // Validar que hay datos
        if (platforms == null || platforms.Length == 0)
        {
            Debug.LogWarning("No se generaron plataformas");
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
            Debug.LogWarning($"¡Cuidado! Has asignado {totalAssigned} plataformas pero solo hay {totalPlatforms} espacios. Algunas no se podrán generar.");
        }
        else if (totalAssigned < totalPlatforms)
        {
            Debug.Log($"Has asignado {totalAssigned} plataformas de {totalPlatforms} totales. El resto serán plataformas por defecto.");
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

                GameObject selectedPrefab = GetPrefabByExactCount(level);

                if (selectedPrefab == null)
                {
                    // Si no hay prefab seleccionado, usar el primero de la lista
                    selectedPrefab = platformRules[0].prefab;
                    Debug.Log($"Usando prefab por defecto: {platformRules[0].platformName}");
                }

                GameObject newPlatform = Instantiate(selectedPrefab, transform);
                newPlatform.transform.localScale = Vector3.one * platformScale;

                CylinderPlatformObj platformCell = newPlatform.GetComponent<CylinderPlatformObj>();
                if (platformCell == null)
                {
                    Debug.LogError($"PlatformPrefab no tiene componente PlatformCellObj");
                    continue;
                }

                
                platformCell.Init(platformData, cylinderRadius, heightOffset,
                                 platformGen.levelHeight, basePosition);

                UpdateCounters(selectedPrefab);
            }
        }

        foreach (var rule in platformRules)
        {
            Debug.Log($"{rule.platformName}: {rule.currentCount} / {rule.exactCount} plataformas generadas");
        }

        Debug.Log($"Torre generada: {levelCount} niveles, {CountTotalPlatforms(platforms)} plataformas totales");
    }

    GameObject GetPrefabByExactCount(int currentLevel)
    {
        availableRules.Clear();

        foreach (var rule in platformRules)
        {
            // Verificar si el prefab está asignado
            if (rule.prefab == null)
            {
                Debug.LogWarning($"Regla '{rule.platformName}' no tiene prefab asignado");
                continue;
            }

            // Verificar si ya alcanzó su límite
            if (rule.isSaturated)
            {
                Debug.Log($"Regla '{rule.platformName}' ya alcanzó su límite de {rule.exactCount}");
                continue;
            }

            // Verificar restricción de nivel
            if (rule.restrictToLevels)
            {
                if (currentLevel < rule.minLevel || currentLevel > rule.maxLevel)
                {
                    Debug.Log($"Regla '{rule.platformName}' no disponible en nivel {currentLevel} (rango: {rule.minLevel}-{rule.maxLevel})");
                    continue;
                }
            }

            // Verificar límite por nivel
            if (rule.limitPerLevel && rule.currentLevelCount >= rule.maxPerLevel)
            {
                Debug.Log($"Regla '{rule.platformName}' alcanzó su límite por nivel ({rule.maxPerLevel}) en nivel {currentLevel}");
                continue;
            }

            // Añadir a disponibles
            availableRules.Add(rule);
        }

        // Si no hay reglas disponibles, retornar null
        if (availableRules.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, availableRules.Count);
        return availableRules[randomIndex].prefab;
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
                    Debug.Log($"✅ Regla '{rule.platformName}' completó su límite de {rule.exactCount}");
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
