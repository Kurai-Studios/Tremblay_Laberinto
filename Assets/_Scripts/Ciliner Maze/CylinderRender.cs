using UnityEngine;

public class CylinderRender : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CylinderPlatformGen platformGen;
    [SerializeField] GameObject platformPrefab;

    [Header("Platform Settings")]
    public float platformScale = 1f;
    public float heightOffset = 0f;  // Desplazamiento vertical desde la base del cilindro

    [Header("Debug")]
    public bool showGizmos = false;

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

        if (platformPrefab == null)
        {
            Debug.LogError("PlatformPrefab no asignado en PlatformRenderer");
            return;
        }

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

        // Loop a traves de todos los niveles y plataformas
        int levelCount = platforms.GetLength(0);
        int maxPlatformsPerLevel = platforms.GetLength(1);

        for (int level = 0; level < levelCount; level++)
        {
            for (int i = 0; i < maxPlatformsPerLevel; i++)
            {
                TowerPlatform platformData = platforms[level, i];
                if (platformData == null) continue;

                GameObject newPlatform = Instantiate(platformPrefab, transform);
                newPlatform.transform.localScale = Vector3.one * platformScale;

                CylinderPlatformObj platformCell = newPlatform.GetComponent<CylinderPlatformObj>();
                if (platformCell == null)
                {
                    Debug.LogError($"PlatformPrefab no tiene componente PlatformCellObj");
                    continue;
                }

                
                platformCell.Init(platformData, cylinderRadius, heightOffset,
                                 platformGen.levelHeight, basePosition);
            }
        }

        Debug.Log($"Torre generada: {levelCount} niveles, {CountTotalPlatforms(platforms)} plataformas totales");
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
