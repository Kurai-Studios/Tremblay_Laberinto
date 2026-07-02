using System.Collections.Generic;
using UnityEngine;

// Clase de datos para cada plataforma
public class TowerPlatform
{
    public int level;       // Nivel/altura donde esta la plataforma
    public float angle;     // Angulo alrededor del cilindro
    public bool isActive;

    public Vector3 position { get { return Vector3.zero; } }

    public TowerPlatform(int level, float angle)
    {
        this.level = level;
        this.angle = angle;
        this.isActive = true;
    }
}

public class CylinderPlatformGen : MonoBehaviour
{
    [Header("Platform Settings")]
    [Range(2, 50)] public int minPlatformsPerLevel = 2;
    [Range(2, 50)] public int maxPlatformsPerLevel = 5;
    public float levelHeight = 1.5f;
    public float minAngleSeparation = 30f;  // Grados minimos entre plataformas

    [Header("Randomization")]
    //public int seed = -1;

    [Header("Cylinder Prefabs")]
    public GameObject[] cylinderPrefabs;  // Al menos 4 prefabs diferentes

    // Variables internas
    private TowerPlatform[,] platforms;
    private GameObject selectedCylinder;
    private Vector3 platformBasePosition;
    private float cylinderHeight;
    private float cylinderRadius;
    private int totalLevels;

    // Lista de direcciones para randomizacion
    List<float> angleDirections = new List<float>();

    // Metodo principal que devuelve la matriz de plataformas
    public TowerPlatform[,] GetPlatforms()
    {

        /*if (seed >= 0)
        {
            Random.InitState(seed);
            Debug.Log($"Usando semilla fija: {seed}");
        }
        else
        {
            // Generar semilla aleatoria real usando GUID
            int randomSeed = System.Guid.NewGuid().GetHashCode();
            Random.InitState(randomSeed);
            Debug.Log($"Usando semilla aleatoria: {randomSeed}");
        }*/

        // Seleccionar cilindro aleatorio
        selectedCylinder = SelectRandomCylinder();

        // Obtener altura y radio del cilindro
        cylinderHeight = GetCylinderHeight(selectedCylinder);
        cylinderRadius = GetCylinderRadius(selectedCylinder);

        //Debug.Log($"Altura del cilindro: {cylinderHeight}");
        //Debug.Log($"Radio del cilindro: {cylinderRadius}");
        //Debug.Log($"Level Height: {levelHeight}");

        CylinderData info = selectedCylinder.GetComponent<CylinderData>();
        if (info != null && info.platformStartPoint != null)
        {
            platformBasePosition = info.platformStartPoint.transform.position;
            //Debug.Log($"StartPoint position: {platformBasePosition}");
        }
        else
        {
            platformBasePosition = Vector3.zero;
            //Debug.LogWarning("No hay StartPoint asignado, usando 0,0,0");
        }

        // Calcular numero de niveles basado en la altura
        totalLevels = Mathf.FloorToInt(cylinderHeight / levelHeight);
        if (totalLevels < 1) totalLevels = 1;

        //Debug.Log($"Total de niveles calculados: {totalLevels}");

        // Inicializar el array de plataformas
        List<TowerPlatform[]> platformList = new List<TowerPlatform[]>();

        // Generar plataformas para cada nivel
        for (int level = 0; level < totalLevels; level++)
        {
            // Determinar cuantas plataformas tendra este nivel (2-5 aleatorio)
            int platformsInLevel = Random.Range(minPlatformsPerLevel, maxPlatformsPerLevel + 1);

            // Generar angulos con separacion minima
            List<float> angles = GenerateAngles(platformsInLevel);

            // Crear array de plataformas para este nivel
            TowerPlatform[] levelPlatforms = new TowerPlatform[platformsInLevel];

            for (int i = 0; i < platformsInLevel; i++)
            {
                levelPlatforms[i] = new TowerPlatform(level, angles[i]);
            }

            platformList.Add(levelPlatforms);
        }

        // Convertir lista a array 2D
        platforms = ConvertTo2DArray(platformList);

        return platforms;
    }

    // Selecciona un cilindro aleatorio del array
    GameObject SelectRandomCylinder()
    {
        if (cylinderPrefabs == null || cylinderPrefabs.Length < 4)
        {
           // Debug.LogError("Se necesitan al menos 4 prefabs de cilindro en cylinderPrefabs");
            return null;
        }

        int randomIndex = Random.Range(0, cylinderPrefabs.Length);
        return cylinderPrefabs[randomIndex];
    }

    // Obtiene la altura del cilindro usando Mesh.bounds
    float GetCylinderHeight(GameObject cylinder)
    {
        if (cylinder == null)
        {
            //Debug.LogError("Cylinder es null");
            return 10f;
        }

        CylinderData info = cylinder.GetComponent<CylinderData>();
        if (info != null)
        {
            //Debug.Log($"Altura obtenida via CilindroInfo: {info.altura}");
            return info.altura;
        }

        // Buscar CilindroInfo en hijos
        info = cylinder.GetComponentInChildren<CylinderData>();
        if (info != null)
        {
            //Debug.Log($"Altura obtenida via CilindroInfo en hijo: {info.altura}");
            return info.altura;
        }

        // 2. Intentar obtener MeshFilter
        MeshFilter meshFilter = cylinder.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            float height = meshFilter.sharedMesh.bounds.size.y;
            //Debug.Log($"Altura obtenida via MeshFilter: {height}");
            return height;
        }

        // 3. Buscar MeshFilter en hijos
        meshFilter = cylinder.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            float height = meshFilter.sharedMesh.bounds.size.y;
            //Debug.Log($"Altura obtenida via MeshFilter en hijo: {height}");
            return height;
        }

        // 4. Intentar con SkinnedMeshRenderer
        SkinnedMeshRenderer skinnedMesh = cylinder.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMesh != null && skinnedMesh.sharedMesh != null)
        {
            float height = skinnedMesh.sharedMesh.bounds.size.y;
            //Debug.Log($"Altura obtenida via SkinnedMeshRenderer: {height}");
            return height;
        }

        // 5. Usar Renderer.bounds
        Renderer renderer = cylinder.GetComponent<Renderer>();
        if (renderer != null)
        {
            float height = renderer.bounds.size.y;
            //Debug.Log($"Altura obtenida via Renderer.bounds: {height}");
            return height;
        }

        // 6. Usar escala local como fallback
        float scaleHeight = cylinder.transform.localScale.y;
        if (scaleHeight > 0)
        {
            //Debug.Log($"Usando escala local como altura: {scaleHeight}");
            return scaleHeight;
        }

        //Debug.LogWarning($"No se pudo obtener la altura del cilindro {cylinder.name}, usando valor por defecto 10");
        return 10f;
    }

    // Obtiene el radio del cilindro usando Mesh.bounds
    float GetCylinderRadius(GameObject cylinder)
    {
        if (cylinder == null)
        {
            //Debug.LogError("Cylinder es null");
            return 5f; // Valor por defecto
        }

        CylinderData info = cylinder.GetComponent<CylinderData>();
        if (info != null)
        {
            //Debug.Log($"Radio obtenido via CilindroInfo: {info.radio}");
            return info.radio;
        }

        info = cylinder.GetComponentInChildren<CylinderData>();
        if (info != null)
        {
            //Debug.Log($"Radio obtenido via CilindroInfo en hijo: {info.radio}");
            return info.radio;
        }



        MeshFilter meshFilter = cylinder.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Bounds bounds = meshFilter.sharedMesh.bounds;
            float radius = Mathf.Max(bounds.size.x, bounds.size.z) / 2f;
            //Debug.Log($"Radio obtenido via MeshFilter: {radius}");
            return radius;
        }

        meshFilter = cylinder.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Bounds bounds = meshFilter.sharedMesh.bounds;
            float radius = Mathf.Max(bounds.size.x, bounds.size.z) / 2f;
            //Debug.Log($"Radio obtenido via MeshFilter en hijo: {radius}");
            return radius;
        }

        SkinnedMeshRenderer skinnedMesh = cylinder.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMesh != null && skinnedMesh.sharedMesh != null)
        {
            Bounds bounds = skinnedMesh.sharedMesh.bounds;
            float radius = Mathf.Max(bounds.size.x, bounds.size.z) / 2f;
            //Debug.Log($"Radio obtenido via SkinnedMeshRenderer: {radius}");
            return radius;
        }

        Renderer renderer = cylinder.GetComponent<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            float radius = Mathf.Max(bounds.size.x, bounds.size.z) / 2f;
            //Debug.Log($"Radio obtenido via Renderer.bounds: {radius}");
            return radius;
        }

        float scaleX = cylinder.transform.localScale.x;
        float scaleZ = cylinder.transform.localScale.z;
        float radio = Mathf.Max(scaleX, scaleZ) / 2f;
        if (radio > 0)
        {
            //Debug.Log($"Usando escala local como radio: {radio}");
            return radio;
        }

        return 5f;
    }

    List<float> GenerateAngles(int count)
    {
        List<float> angles = new List<float>();

        //Random.InitState(seed);

        // Si solo hay 1 plataforma, puede estar en cualquier angulo
        if (count == 1)
        {
            float angle = Random.Range(0f, 360f);
            //Debug.Log($"Generando 1 ángulo: {angle}");
            angles.Add(angle);
            return angles;
        }

        // Para multiples plataformas, asegurar separacion minima
        int maxAttempts = 1000;
        int attempts = 0;

        while (angles.Count < count && attempts < maxAttempts)
        {
            attempts++;
            float newAngle = Random.Range(0f, 360f);

            //Debug.Log($"Intento {attempts}: Generando ángulo {newAngle}");

            bool isValid = true;
            foreach (float existingAngle in angles)
            {
                float difference = Mathf.Abs(newAngle - existingAngle);
                difference = Mathf.Min(difference, 360f - difference);

                //Debug.Log($"  Comparando con {existingAngle}: diferencia {difference}, mínima {minAngleSeparation}");

                if (difference < minAngleSeparation)
                {
                    isValid = false;
                    //Debug.Log($"  Ángulo rechazado - demasiado cerca");
                    break;
                }
            }

            if (isValid)
            {
                angles.Add(newAngle);
                //Debug.Log($"  Ángulo aceptado: {newAngle}");
            }
        }

        //Debug.Log($"Ángulos finales generados: {string.Join(", ", angles)}");

        return angles;
    }

    // Convierte lista de arrays a un array 2D
    TowerPlatform[,] ConvertTo2DArray(List<TowerPlatform[]> platformList)
    {
        if (platformList == null || platformList.Count == 0)
            return new TowerPlatform[0, 0];

        // Encontrar el nivel con mas plataformas
        int maxPlatforms = 0;
        foreach (TowerPlatform[] level in platformList)
        {
            if (level.Length > maxPlatforms)
                maxPlatforms = level.Length;
        }

        // Crear array 2D con dimensiones [levels, maxPlatforms]
        TowerPlatform[,] result = new TowerPlatform[platformList.Count, maxPlatforms];

        // Llenar el array
        for (int level = 0; level < platformList.Count; level++)
        {
            TowerPlatform[] currentLevel = platformList[level];
            for (int i = 0; i < currentLevel.Length; i++)
            {
                result[level, i] = currentLevel[i];
            }
            // Las posiciones vacias quedan como null
        }

        return result;
    }

    // Metodo para obtener el cilindro seleccionado (usado por el renderer)
    public GameObject GetSelectedCylinder()
    {
        return selectedCylinder;
    }

    public Vector3 GetPlatformBasePosition()
    {
        return platformBasePosition;
    }

    // Metodo para obtener el radio del cilindro (usado por el renderer)
    public float GetCylinderRadius()
    {
        return cylinderRadius;
    }

    // Metodo para obtener la altura del cilindro (usado por el renderer)
    public float GetCylinderHeight()
    {
        return cylinderHeight;
    }

    // Metodo para obtener el numero total de niveles
    public int GetTotalLevels()
    {
        return totalLevels;
    }
}