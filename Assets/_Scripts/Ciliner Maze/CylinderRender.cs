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

// Registro de un tramo angular ocupado en un nivel (usado por PuzzlePathPlacer para encontrar
// espacio libre). 'angle' es el centro de la plataforma; 'halfWidthAngle' es cuanto ocupa a cada
// lado, ya convertido a grados via HalfWidthToAngle.
public struct OccupiedSlot
{
    public float angle;
    public float halfWidthAngle;
}

public class CylinderRender : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CylinderGeneration platformGen;
    [SerializeField] RuleManager ruleManager;
    [SerializeField] LadderPlacer ladderPlacer;
    [SerializeField] PuzzlePathPlacer puzzlePathPlacer;

    [Header("Platform Settings")]
    public GameObject[] platformPrefabs; // Reemplaza temporalmente a platformRules mientras el sistema de reglas esta desactivado
    public float platformScale = 1f;
    [Tooltip("Desactivado temporalmente para poder ver y diagnosticar el camino principal (plataformas + extras encadenadas) sin el ruido visual de las escaleras. Reactivar cuando el camino principal este validado")]
    public bool enableLadders = false;
    [Tooltip("Desactivado temporalmente mientras se verifica que el camino principal y las escaleras funcionen bien por si solos (deteccion de bloqueo por MainPath, etc.). Reactivar cuando ese trabajo este validado")]
    public bool enablePuzzlePath = false;

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

    // Anchors (L o R) de plataformas principales que ya tienen una plataforma extra encadenada
    // pegada a ellos (ver GetOccupiedAnchors). Solo el PRIMER eslabon de una cadena de extras
    // toca el anchor de la principal; los siguientes se pegan entre si, no a la principal.
    private readonly HashSet<Transform> occupiedAnchors = new HashSet<Transform>();

    // Todos los tramos angulares ocupados por nivel (principal + extras + trampas). PuzzlePathPlacer
    // lo usa para encontrar espacio libre donde encajar una rampa.
    private Dictionary<int, List<OccupiedSlot>> occupiedSlotsByLevel = new Dictionary<int, List<OccupiedSlot>>();

    // Radio y posicion base del cilindro generado, guardados para que PuzzlePathPlacer pueda
    // calcular posiciones de la misma forma que CylinderPlatformObj despues de terminada la generacion.
    private float generatedCylinderRadius;
    private Vector3 generatedBasePosition;

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

        // Se le pasa al RuleManager el radio, la altura de nivel, la tolerancia de inclinacion
        // de LadderPlacer y el angulo que ocupa el anchor L/R de una plataforma principal (tomando
        // "Normal" como representativa; hoy los 4 prefabs comparten el mismo offset de anchor)
        // para que el propio camino apunte sus pasos de angulo entre niveles al valor que deja
        // los anchors casi alineados verticalmente, en vez de generar pasos arbitrarios y luego
        // tener que arreglar la conexion despues.
        float maxLadderTilt = ladderPlacer != null ? ladderPlacer.maxTiltFromVertical : 45f;
        float connectorHalfWidthAngle = GetConnectorHalfWidthAngle(cylinderRadiusValue);
        List<PathPlatform> path = ruleManager.GeneratePath(totalLevels, cylinderRadiusValue, platformGen.levelHeight, maxLadderTilt, connectorHalfWidthAngle);

        mainPathPlatforms.Clear();
        occupiedAnchors.Clear();
        occupiedSlotsByLevel.Clear();
        generatedCylinderRadius = cylinderRadiusValue;
        generatedBasePosition = basePosition;

        // Estado del encadenado por anchors: angulo y medio-ancho tangencial de la ultima
        // plataforma colocada en el nivel actual. Se reinicia cada vez que llega una plataforma
        // principal (no encadenada), que es siempre la primera de cada nivel.
        float chainAngle = 0f;
        float chainHalfWidth = 0f;

        // Angulo y medio-ancho (angular y tangencial/lineal) de la plataforma de Spawn (nivel 0),
        // guardados para poder rellenar todo el nivel 0 con plataformas Normal encadenadas a
        // partir de su anchor (ver el relleno de anillo despues del foreach de abajo).
        float spawnAngle = 0f;
        float spawnHalfWidth = 0f;
        float spawnHalfWidthAngle = 0f;
        bool hasSpawnPlatform = false;

        // Niveles cuyo primer eslabon de cadena (el que toca el anchor de la principal) ya se
        // proceso, para no volver a marcar ese anchor con los eslabones siguientes (que se pegan
        // entre si, no a la principal).
        HashSet<int> levelsWithAnchorMarked = new HashSet<int>();

        foreach (PathPlatform pathPlatform in path)
        {
            GameObject selectedPrefab = GetPrefabByTag(pathPlatform.tag);
            if (selectedPrefab == null)
            {
                //Debug.LogWarning($"No hay ningun platformPrefab con el tag '{pathPlatform.tag}'");
                continue;
            }

            CylinderPlatformObj prefabData = selectedPrefab.GetComponent<CylinderPlatformObj>();
            float halfWidth = (prefabData != null ? prefabData.GetTangentialHalfWidth() : 0f) * platformScale;
            float halfWidthAngle = HalfWidthToAngle(halfWidth, cylinderRadiusValue);

            float finalAngle;
            if (pathPlatform.isChained)
            {
                // Encadenar por anchors: avanzar justo lo necesario para que el anchor de la
                // plataforma anterior quede pegado al anchor de esta, sin superponerse ni dejar hueco
                float angleStep = HalfWidthToAngle(chainHalfWidth, cylinderRadiusValue) + halfWidthAngle;
                finalAngle = NormalizeAngle(chainAngle + angleStep * pathPlatform.chainDirection);
            }
            else
            {
                finalAngle = pathPlatform.angle;
            }

            chainAngle = finalAngle;
            chainHalfWidth = halfWidth;

            if (pathPlatform.level == 0 && !pathPlatform.isChained)
            {
                hasSpawnPlatform = true;
                spawnAngle = finalAngle;
                spawnHalfWidth = halfWidth;
                spawnHalfWidthAngle = halfWidthAngle;
            }

            RecordOccupiedSlot(pathPlatform.level, finalAngle, halfWidthAngle);

            GameObject newPlatform = Instantiate(selectedPrefab, transform);
            newPlatform.transform.localScale = Vector3.one * platformScale;

            CylinderPlatformObj platformCell = newPlatform.GetComponent<CylinderPlatformObj>();
            if (platformCell == null)
            {
                //Debug.LogError($"PlatformPrefab no tiene componente PlatformCellObj");
                continue;
            }

            TowerPlatform platformData = new TowerPlatform(pathPlatform.level, finalAngle);

            platformCell.Init(platformData, cylinderRadiusValue,
                             platformGen.levelHeight, basePosition);

            // Solo se guarda la primera plataforma generada por nivel (la principal del
            // camino); las extras del mismo nivel no se usan como puntos de conexion de escaleras.
            if (!mainPathPlatforms.ContainsKey(pathPlatform.level))
                mainPathPlatforms[pathPlatform.level] = platformCell;

            // Si esta es la primera plataforma encadenada del nivel, queda pegada directamente al
            // anchor de la principal (el lado 'freeSide' que eligio RuleManager): marcar ese
            // anchor como ocupado para que LadderPlacer no haga llegar una escalera justo ahi.
            if (pathPlatform.isChained && levelsWithAnchorMarked.Add(pathPlatform.level)
                && mainPathPlatforms.TryGetValue(pathPlatform.level, out CylinderPlatformObj levelMain))
            {
                Transform touchedAnchor = ClosestAnchorTo(levelMain, platformCell.transform.position);
                if (touchedAnchor != null)
                    occupiedAnchors.Add(touchedAnchor);
            }
        }

        //Debug.Log($"Camino generado: {path.Count} plataformas en {totalLevels} niveles");

        // Rellenar el nivel de Spawn (nivel 0) por completo con plataformas Normal, encadenadas
        // por anchors igual que las extras de niveles intermedios, dando toda la vuelta al
        // cilindro a partir del anchor de Spawn. Asi el nivel 0 queda 100% caminable y el
        // jugador siempre puede alcanzar la escalera/rampa de salida sin importar en que angulo
        // haya quedado, en vez de depender de una unica plataforma de Spawn aislada.
        if (hasSpawnPlatform)
            FillSpawnLevelRing(spawnAngle, spawnHalfWidth, spawnHalfWidthAngle, cylinderRadiusValue, basePosition);

        // Este proyecto tiene 'Physics.autoSyncTransforms' desactivado (Edit > Project Settings >
        // Physics), asi que los colliders de las plataformas recien reposicionadas (transform.position
        // asignado arriba, en este mismo frame) NO quedan sincronizados con el motor de fisica hasta
        // el proximo paso de simulacion o una llamada explicita a esto. Sin este sync, cualquier
        // Physics.Raycast/RaycastAll/OverlapSphere hecho en el resto de este metodo (el bloqueo de
        // escaleras en LadderPlacer, los rayos de deteccion de anchor en CylinderPlatformObj, etc.)
        // consulta posiciones VIEJAS de los colliders y practicamente nunca detecta nada real.
        Physics.SyncTransforms();

        // Escaleras: desactivadas temporalmente via 'enableLadders' para poder ver y diagnosticar
        // el camino principal (plataformas + extras encadenadas) por si solo, sin el ruido visual
        // de las escaleras encima.
        if (enableLadders && ladderPlacer != null)
            ladderPlacer.PlaceLadders();
        else if (ladderPlacer != null)
            ladderPlacer.ClearLadders();

        // Puzzle Path: rampas alternativas, generadas despues del camino principal y las escaleras
        // para saber que angulos estan realmente libres en cada nivel. Desactivado temporalmente
        // via 'enablePuzzlePath' para poder validar el camino principal y las escaleras aislados.
        if (enablePuzzlePath && puzzlePathPlacer != null)
            puzzlePathPlacer.PlaceRamps();
        else if (puzzlePathPlacer != null)
            puzzlePathPlacer.ClearRamps();
    }

    // Registra el tramo angular que ocupa una plataforma en su nivel, para que PuzzlePathPlacer
    // pueda encontrar espacio libre despues
    void RecordOccupiedSlot(int level, float angle, float halfWidthAngle)
    {
        if (!occupiedSlotsByLevel.TryGetValue(level, out List<OccupiedSlot> slots))
        {
            slots = new List<OccupiedSlot>();
            occupiedSlotsByLevel[level] = slots;
        }

        slots.Add(new OccupiedSlot { angle = angle, halfWidthAngle = halfWidthAngle });
    }

    // Rellena todo el nivel 0 con plataformas "Normal" encadenadas por anchors, partiendo del
    // anchor de la plataforma de Spawn y dando toda la vuelta al cilindro en una sola direccion,
    // con el mismo calculo de paso angular (anchor a anchor, sin superponerse ni dejar hueco) que
    // usan las extras encadenadas de los niveles intermedios. Se detiene cuando ya se cubrio todo
    // el arco disponible (360 grados menos el propio ancho de Spawn), es decir, cuando el proximo
    // eslabon cerraria el anillo volviendo a tocar a Spawn desde el otro lado.
    //
    // La condicion de corte se basa en sumar los angleStep ya usados ('sweptAngle') en vez de
    // comparar el angulo del candidato contra el de Spawn con un chequeo de superposicion: ese
    // chequeo comparaba una diferencia de angulos (via DeltaAngle) contra una suma de dos
    // conversiones atan2 independientes, y el primer eslabon (que por construccion queda
    // exactamente tangente al anchor de Spawn, sin superponerse) terminaba marcado como
    // superpuesto por un error de redondeo de punto flotante entre ambos caminos de calculo,
    // frenando el relleno antes de agregar ni una sola plataforma.
    void FillSpawnLevelRing(float spawnAngle, float spawnHalfWidth, float spawnHalfWidthAngle, float cylinderRadiusValue, Vector3 basePosition)
    {
        GameObject normalPrefab = GetPrefabByTag("Normal");
        if (normalPrefab == null) return;

        CylinderPlatformObj normalPrefabData = normalPrefab.GetComponent<CylinderPlatformObj>();
        if (normalPrefabData == null) return;

        float ringHalfWidth = normalPrefabData.GetTangentialHalfWidth() * platformScale;
        float ringHalfWidthAngle = HalfWidthToAngle(ringHalfWidth, cylinderRadiusValue);
        if (ringHalfWidthAngle <= 0f) return;

        float chainAngle = spawnAngle;
        float chainHalfWidth = spawnHalfWidth;

        // Arco total disponible para el anillo: la vuelta completa menos el ancho que ya ocupa
        // la propia plataforma de Spawn (sus dos medios-anchos, a ambos lados de su centro).
        float remainingArc = 360f - (spawnHalfWidthAngle * 2f);
        float sweptAngle = 0f;

        // Salvaguarda contra loop infinito: nunca deberian hacer falta mas eslabones que los que
        // entran, en el peor caso, en una vuelta completa.
        int maxRingPlatforms = Mathf.CeilToInt(360f / Mathf.Max(1f, ringHalfWidthAngle)) + 1;

        for (int i = 0; i < maxRingPlatforms; i++)
        {
            float angleStep = HalfWidthToAngle(chainHalfWidth, cylinderRadiusValue) + ringHalfWidthAngle;

            // Si este eslabon ya no entra en el arco restante, el anillo esta completo: dejar de
            // agregar plataformas en vez de superponerse a Spawn por el otro lado.
            if (sweptAngle + angleStep > remainingArc)
                break;

            float candidateAngle = NormalizeAngle(chainAngle + angleStep);

            RecordOccupiedSlot(0, candidateAngle, ringHalfWidthAngle);

            GameObject ringPlatform = Instantiate(normalPrefab, transform);
            ringPlatform.transform.localScale = Vector3.one * platformScale;

            CylinderPlatformObj ringPlatformCell = ringPlatform.GetComponent<CylinderPlatformObj>();
            if (ringPlatformCell != null)
            {
                TowerPlatform ringPlatformData = new TowerPlatform(0, candidateAngle);
                ringPlatformCell.Init(ringPlatformData, cylinderRadiusValue, platformGen.levelHeight, basePosition);
            }

            chainAngle = candidateAngle;
            chainHalfWidth = ringHalfWidth;
            sweptAngle += angleStep;
        }
    }

    // De los dos anchors (L/R) de una plataforma, devuelve el mas cercano a una posicion dada.
    // Usado para saber cual de los dos queda pegado a una plataforma extra recien encadenada.
    Transform ClosestAnchorTo(CylinderPlatformObj platform, Vector3 position)
    {
        Transform anchorL = platform.GetAnchorL();
        Transform anchorR = platform.GetAnchorR();

        if (anchorL == null) return anchorR;
        if (anchorR == null) return anchorL;

        float distL = Vector3.Distance(anchorL.position, position);
        float distR = Vector3.Distance(anchorR.position, position);

        return distL <= distR ? anchorL : anchorR;
    }

    // Plataforma principal del camino por nivel, usada por LadderPlacer para conectar niveles consecutivos
    public Dictionary<int, CylinderPlatformObj> GetMainPathPlatforms()
    {
        return mainPathPlatforms;
    }

    // Tramos angulares ocupados por nivel, usado por PuzzlePathPlacer para encontrar espacio libre
    public Dictionary<int, List<OccupiedSlot>> GetOccupiedSlots()
    {
        return occupiedSlotsByLevel;
    }

    // Anchors de plataformas principales que ya tienen una plataforma extra pegada, usado por
    // LadderPlacer para no hacer llegar una escalera justo al punto donde ya hay otra plataforma.
    public HashSet<Transform> GetOccupiedAnchors()
    {
        return occupiedAnchors;
    }

    // Radio, altura de nivel y posicion base usados en la ultima generacion, para que
    // PuzzlePathPlacer pueda calcular posiciones de la misma forma que CylinderPlatformObj
    public float GetCylinderRadiusValue()
    {
        return generatedCylinderRadius;
    }

    public float GetLevelHeight()
    {
        return platformGen != null ? platformGen.levelHeight : 0f;
    }

    public Vector3 GetBasePosition()
    {
        return generatedBasePosition;
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

    // Convierte un medio-ancho tangencial (mitad del ancho de una plataforma, medido entre sus
    // anchors L/R) en el angulo (grados) que ocupa alrededor del cilindro a un radio dado.
    // Usado para encadenar plataformas del mismo nivel por sus anchors sin superponerse, y por
    // PuzzlePathPlacer para saber cuanto espacio angular necesita una rampa.
    public float HalfWidthToAngle(float halfWidth, float radius)
    {
        if (radius <= 0f) return 0f;
        return Mathf.Atan2(halfWidth, radius) * Mathf.Rad2Deg;
    }

    public float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    // Angulo (grados) que ocupa, a un radio dado, el anchor L/R del prefab principal del camino
    // (se usa "Normal" como representativo: hoy los 4 prefabs de plataforma comparten el mismo
    // offset de anchor). RuleManager lo usa para apuntar sus pasos al angulo que deja los anchors
    // de dos niveles consecutivos casi alineados verticalmente (ver GeneratePath).
    float GetConnectorHalfWidthAngle(float radius)
    {
        GameObject normalPrefab = GetPrefabByTag("Normal");
        if (normalPrefab == null) return 0f;

        CylinderPlatformObj prefabData = normalPrefab.GetComponent<CylinderPlatformObj>();
        if (prefabData == null) return 0f;

        float halfWidth = prefabData.GetTangentialHalfWidth() * platformScale;
        return HalfWidthToAngle(halfWidth, radius);
    }

    // Busca en platformPrefabs el primer prefab cuyo CylinderPlatformObj tenga el tag indicado
    public GameObject GetPrefabByTag(string tag)
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
        occupiedAnchors.Clear();
        occupiedSlotsByLevel.Clear();

        if (ladderPlacer != null)
            ladderPlacer.ClearLadders();

        if (puzzlePathPlacer != null)
            puzzlePathPlacer.ClearRamps();

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
