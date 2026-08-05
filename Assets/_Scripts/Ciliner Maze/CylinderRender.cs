using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

/* ===== SISTEMA DE REGLAS - Comentado por ahora, retomar mas adelante =====
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
*/

public class CylinderRender : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CylinderGeneration platformGen;
    [SerializeField] RuleManager ruleManager;
    [SerializeField] LadderPlacer ladderPlacer;

    [Header("Platform Settings")]
    public GameObject[] platformPrefabs; // Reemplaza temporalmente a platformRules mientras el sistema de reglas esta desactivado
    public float platformScale = 1f;

    [Header("Cylinder Settings")]
    public bool overrideCylinderDimensions = false;
    public float cylinderRadius = 5f;
    public float cylinderHeight = 10f;

    [Header("Cylinder Materials")]
    [Tooltip("Se elige un material al azar de esta lista y se aplica al cilindro instanciado en cada partida")]
    public Material[] cylinderMaterials;

    /*[Header("Debug")]
    public bool showGizmos = false;*/

    /* private List<PlatformRule> availableRules = new List<PlatformRule>(); */

    // Plataforma principal del camino por nivel (la primera generada en ese nivel; las
    // extras del mismo nivel no cuentan). LadderPlacer la usa para conectar niveles consecutivos.
    private Dictionary<int, CylinderPlatformObj> mainPathPlatforms = new Dictionary<int, CylinderPlatformObj>();

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

        if (platformPrefabs == null || platformPrefabs.Length == 0)
        {
            //Debug.LogError("No hay platformPrefabs asignados en PlatformRenderer");
            return;
        }

        /* ResetCounters(); */

        /* if (ruleManager != null)
        {
            ruleManager.ResetRules();

            float radius = platformGen.GetCylinderRadius();
            float height = platformGen.GetCylinderHeight();
            ruleManager.UpdateCylinderConfig(radius, height);
        } */

        // Aplicar (o limpiar) el override de dimensiones del cilindro antes de generar
        if (overrideCylinderDimensions)
        {
            platformGen.SetCylinderOverride(cylinderRadius, cylinderHeight);
        }
        else
        {
            platformGen.ClearCylinderOverride();
        }

        // Obtener los datos de las plataformas
        TowerPlatform[,] platforms = platformGen.GetPlatforms();

        // Obtener dimensiones del cilindro
        float cylinderRadiusValue = platformGen.GetCylinderRadius();
        float cylinderHeightValue = platformGen.GetCylinderHeight();
        int totalLevels = platformGen.GetTotalLevels();

        Vector3 basePosition = platformGen.GetPlatformBasePosition();

        //Debug.Log($"Renderizando plataformas desde: {basePosition}");
        //Debug.Log($"Radio del cilindro: {cylinderRadiusValue}");
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

            // Si hay override activo, escalar el cilindro instanciado para que coincida visualmente
            if (overrideCylinderDimensions)
            {
                CylinderData baseData = cylinder.GetComponent<CylinderData>();
                if (baseData == null)
                    baseData = cylinder.GetComponentInChildren<CylinderData>();

                if (baseData != null && baseData.radio > 0f && baseData.altura > 0f)
                {
                    float radiusScale = cylinderRadius / baseData.radio;
                    float heightScale = cylinderHeight / baseData.altura;
                    cylinderInstance.transform.localScale = new Vector3(radiusScale, heightScale, radiusScale);
                }
            }

            // Aplicar un material aleatorio de la lista, si hay alguno asignado
            if (cylinderMaterials != null && cylinderMaterials.Length > 0)
            {
                Material randomMaterial = cylinderMaterials[Random.Range(0, cylinderMaterials.Length)];
                ApplyMaterial(cylinderInstance, randomMaterial);
            }
        }

        // Por ahora solo generamos el camino (path) definido por el RuleManager,
        // sin ninguna otra regla ni relleno aleatorio de plataformas.
        if (ruleManager == null)
        {
            //Debug.LogError("RuleManager no asignado en CylinderRender");
            return;
        }

        List<PathPlatform> path = ruleManager.GeneratePath(totalLevels);

        mainPathPlatforms.Clear();

        foreach (PathPlatform pathPlatform in path)
        {
            GameObject selectedPrefab = GetPrefabByTag(pathPlatform.tag);
            if (selectedPrefab == null)
            {
                //Debug.LogWarning($"No hay ningun platformPrefab con el tag '{pathPlatform.tag}'");
                continue;
            }

            GameObject newPlatform = Instantiate(selectedPrefab, transform);
            newPlatform.transform.localScale = Vector3.one * platformScale;

            CylinderPlatformObj platformCell = newPlatform.GetComponent<CylinderPlatformObj>();
            if (platformCell == null)
            {
                //Debug.LogError($"PlatformPrefab no tiene componente PlatformCellObj");
                continue;
            }

            TowerPlatform platformData = new TowerPlatform(pathPlatform.level, pathPlatform.angle);

            platformCell.Init(platformData, cylinderRadiusValue,
                             platformGen.levelHeight, basePosition);

            // Solo se guarda la primera plataforma generada por nivel (la principal del
            // camino); las extras del mismo nivel no se usan como puntos de conexion de escaleras.
            if (!mainPathPlatforms.ContainsKey(pathPlatform.level))
                mainPathPlatforms[pathPlatform.level] = platformCell;
        }

        //Debug.Log($"Camino generado: {path.Count} plataformas en {totalLevels} niveles");

        if (ladderPlacer != null)
            ladderPlacer.PlaceLadders();
    }

    // Plataforma principal del camino por nivel, usada por LadderPlacer para conectar niveles consecutivos
    public Dictionary<int, CylinderPlatformObj> GetMainPathPlatforms()
    {
        return mainPathPlatforms;
    }

    // Aplica un material a todos los renderers del cilindro instanciado (el propio objeto y sus hijos)
    void ApplyMaterial(GameObject target, Material material)
    {
        if (material == null) return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.material = material;
        }
    }

    // Busca en platformPrefabs el primer prefab cuyo CylinderPlatformObj tenga el tag indicado
    GameObject GetPrefabByTag(string tag)
    {
        foreach (GameObject prefab in platformPrefabs)
        {
            CylinderPlatformObj platformObj = prefab.GetComponent<CylinderPlatformObj>();
            if (platformObj != null && platformObj.HasTag(tag))
                return prefab;
        }

        return null;
    }

    /* ===== SELECCION POR REGLAS - Comentado por ahora, retomar mas adelante =====
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
    */

    // Limpia todas las plataformas generadas (para regenerar)
    public void ClearTower()
    {
        // Eliminar todos los hijos del PlatformRenderer
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        mainPathPlatforms.Clear();

        if (ladderPlacer != null)
            ladderPlacer.ClearLadders();

        /* ResetCounters();

        if (ruleManager != null)
            ruleManager.ResetRules(); */
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
