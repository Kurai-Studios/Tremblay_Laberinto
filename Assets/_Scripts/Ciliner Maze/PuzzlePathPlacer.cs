using System.Collections.Generic;
using UnityEngine;

// Genera el "Puzzle Path": rampas (plataformas Normal rotadas) que conectan un nivel con el
// siguiente por una zona angular libre (no usada por el camino principal ni por las trampas),
// como ruta alternativa a la escalera de esa conexion. Se ejecuta despues del camino principal
// y de las escaleras, para saber que angulos estan realmente disponibles en cada nivel.
//
// Version inicial: intenta colocar UNA rampa en cada conexion de niveles consecutivos donde
// encuentre espacio libre en ambos extremos, probando angulos al azar. No prioriza todavia las
// conexiones cuya escalera toque una plataforma Trampa (eso se afinara mas adelante).
public class PuzzlePathPlacer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CylinderRender cylinderRender;

    [Header("Ramp Settings")]
    [Tooltip("Angulo minimo (grados) que recorre la rampa mientras sube de un nivel al siguiente; le da forma de rampa/espiral en vez de subir en linea recta. Si maxRampTiltAngle exige mas angulo para no quedar demasiado empinada, se usa ese valor mas grande en su lugar")]
    public float rampAngleSpan = 40f;
    [Tooltip("Cuantos angulos candidatos se prueban por conexion de niveles antes de rendirse si no encuentra espacio libre")]
    public int placementAttempts = 12;
    [Tooltip("Inclinacion maxima (grados respecto a la horizontal) que puede tener la rampa. Mas bajo = rampa mas plana y menos brusca; rampAngleSpan se amplia automaticamente si hace falta para respetar este limite")]
    [Range(5f, 80f)]
    public float maxRampTiltAngle = 25f;
    [Tooltip("Minimo de plataformas que se instancian para formar el tramo entre dos niveles, contando ambos extremos (mas plataformas = escalones mas cortos y un tramo que realmente conecta ambos niveles en vez de flotar a mitad de camino)")]
    public int minRampSegments = 2;
    [Tooltip("Maximo de plataformas que se instancian para formar el tramo entre dos niveles")]
    public int maxRampSegments = 4;

    [Header("Debug")]
    [Tooltip("Dibuja en el Scene View cada linea start-a-end probada por TryFindFreeConnection (verde = libre y se acepto, rojo = bloqueada por otra plataforma y se descarto ese candidato), con una esfera amarilla en el punto exacto del impacto que la bloqueo")]
    public bool showDebugRays = true;

    // Contenedor propio para las rampas generadas, por la misma razon que LadderPlacer usa el
    // suyo: no se puede parentear directamente a "transform" (compartido con CylinderRender) o
    // ClearRamps() borraria tambien el cilindro y las plataformas.
    private Transform rampContainer;

    // Registro de cada linea start-a-end probada en la ultima generacion, para dibujarla como
    // gizmo y poder ver si el raycast esta detectando (o no) la plataforma del medio.
    private struct DebugLineCheck
    {
        public Vector3 from;
        public Vector3 to;
        public bool blocked;
        public Vector3 blockingPoint;
    }
    private readonly List<DebugLineCheck> debugChecks = new List<DebugLineCheck>();

    void EnsureContainer()
    {
        if (rampContainer != null) return;

        GameObject containerObj = new GameObject("PuzzlePath");
        containerObj.transform.SetParent(transform, false);
        rampContainer = containerObj.transform;
    }

    // Intenta colocar una rampa en cada conexion de niveles consecutivos del camino principal
    public void PlaceRamps()
    {
        ClearRamps();
        debugChecks.Clear();

        if (cylinderRender == null) return;

        GameObject rampPrefab = cylinderRender.GetPrefabByTag("Normal");
        if (rampPrefab == null) return;

        CylinderPlatformObj rampPrefabData = rampPrefab.GetComponent<CylinderPlatformObj>();
        if (rampPrefabData == null) return;

        EnsureContainer();

        float radius = cylinderRender.GetCylinderRadiusValue();
        float levelHeight = cylinderRender.GetLevelHeight();
        Vector3 basePosition = cylinderRender.GetBasePosition();
        float platformScale = cylinderRender.platformScale;

        float halfWidth = rampPrefabData.GetTangentialHalfWidth() * platformScale;
        float halfWidthAngle = cylinderRender.HalfWidthToAngle(halfWidth, radius);

        // Angulo total minimo que debe recorrer la rampa para que su inclinacion no supere
        // maxRampTiltAngle al subir un nivel completo (mismo rise para cualquier cantidad de
        // escalones, ya que es una interpolacion lineal). Si rampAngleSpan ya es mayor a este
        // minimo se respeta rampAngleSpan; si no, se amplia automaticamente para evitar una
        // inclinacion demasiado brusca.
        float requiredRun = levelHeight / Mathf.Tan(Mathf.Clamp(maxRampTiltAngle, 1f, 89f) * Mathf.Deg2Rad);
        float requiredSpanForTilt = cylinderRender.HalfWidthToAngle(requiredRun, radius);
        float totalSpan = Mathf.Max(rampAngleSpan, requiredSpanForTilt);

        // Copia de trabajo de los espacios ocupados: arranca con lo que ya ocupa el camino
        // principal y se le suma cada rampa colocada, para que tampoco se superpongan entre si.
        Dictionary<int, List<OccupiedSlot>> occupied = CloneOccupiedSlots(cylinderRender.GetOccupiedSlots());

        Dictionary<int, CylinderPlatformObj> mainPath = cylinderRender.GetMainPathPlatforms();

        List<int> levels = new List<int>(mainPath.Keys);
        levels.Sort();

        foreach (int level in levels)
        {
            // No generar puzzle path desde el nivel 0 (Spawn): evita renderizado innecesario
            // en la conexion inicial, donde no aporta como ruta alternativa.
            if (level == 0) continue;

            int nextLevel = level + 1;
            if (!mainPath.ContainsKey(nextLevel)) continue;

            if (!TryFindFreeConnection(occupied, level, nextLevel, halfWidthAngle, totalSpan, radius, levelHeight, basePosition,
                                        out float startAngle, out float direction))
                continue;

            float endAngle = NormalizeAngle(startAngle + totalSpan * direction);

            PlaceRamp(rampPrefab, level, nextLevel, startAngle, direction, totalSpan, radius, levelHeight, basePosition, platformScale);

            AddOccupiedSlot(occupied, level, startAngle, halfWidthAngle);
            AddOccupiedSlot(occupied, nextLevel, endAngle, halfWidthAngle);
        }
    }

    // Busca un par de angulos (uno por nivel) donde quepa la rampa sin superponerse con nada ya
    // colocado en ninguno de los dos niveles, probando candidatos al azar. Ademas de que el
    // hueco angular este libre, se comprueba que ninguna plataforma ya colocada quede
    // fisicamente en medio del tramo recto entre ambos puntos (ver CylinderPlatformObj.IsLineBlocked).
    bool TryFindFreeConnection(Dictionary<int, List<OccupiedSlot>> occupied, int level, int nextLevel,
                                float halfWidthAngle, float totalSpan, float radius, float levelHeight, Vector3 basePosition,
                                out float startAngle, out float direction)
    {
        for (int attempt = 0; attempt < placementAttempts; attempt++)
        {
            float candidateStart = Random.Range(0f, 360f);
            float candidateDirection = Random.value < 0.5f ? 1f : -1f;
            float candidateEnd = NormalizeAngle(candidateStart + totalSpan * candidateDirection);

            bool startFree = IsAngleFree(occupied, level, candidateStart, halfWidthAngle);
            bool endFree = IsAngleFree(occupied, nextLevel, candidateEnd, halfWidthAngle);

            if (!startFree || !endFree) continue;

            Vector3 startPos = CylinderPlatformObj.ComputeWorldPosition(candidateStart, level, radius, levelHeight, basePosition);
            Vector3 endPos = CylinderPlatformObj.ComputeWorldPosition(candidateEnd, nextLevel, radius, levelHeight, basePosition);

            bool blocked = CylinderPlatformObj.IsLineBlocked(startPos, endPos, out Vector3 blockingPoint);
            debugChecks.Add(new DebugLineCheck { from = startPos, to = endPos, blocked = blocked, blockingPoint = blockingPoint });

            if (blocked)
                continue;

            startAngle = candidateStart;
            direction = candidateDirection;
            return true;
        }

        startAngle = 0f;
        direction = 1f;
        return false;
    }

    bool IsAngleFree(Dictionary<int, List<OccupiedSlot>> occupied, int level, float angle, float halfWidthAngle)
    {
        if (!occupied.TryGetValue(level, out List<OccupiedSlot> slots) || slots == null)
            return true;

        foreach (OccupiedSlot slot in slots)
        {
            float difference = Mathf.Abs(NormalizeAngle(angle) - NormalizeAngle(slot.angle));
            difference = Mathf.Min(difference, 360f - difference);

            if (difference < halfWidthAngle + slot.halfWidthAngle)
                return false;
        }

        return true;
    }

    // Construye el tramo como una escalinata de varias plataformas encadenadas entre el nivel de
    // partida y el siguiente, en vez de una unica plataforma flotando a mitad de camino sin tocar
    // ninguno de los dos extremos. Angulo y altura se interpolan linealmente entre ambos niveles,
    // asi que cada escalon queda con la misma inclinacion suave que el tramo completo (acotada
    // por maxRampTiltAngle via 'totalSpan', calculado en PlaceRamps).
    void PlaceRamp(GameObject rampPrefab, int level, int nextLevel, float startAngle, float direction, float totalSpan,
                    float radius, float levelHeight, Vector3 basePosition, float platformScale)
    {
        int segments = Random.Range(minRampSegments, maxRampSegments + 1);
        if (segments < 1) segments = 1;

        // 'segments + 1' puntos incluyendo ambos extremos (t = 0 en 'level', t = 1 en 'nextLevel')
        Vector3[] points = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = NormalizeAngle(startAngle + totalSpan * direction * t);
            float levelValue = Mathf.Lerp(level, nextLevel, t);
            points[i] = CylinderPlatformObj.ComputeWorldPosition(angle, levelValue, radius, levelHeight, basePosition);
        }

        for (int i = 0; i <= segments; i++)
        {
            GameObject rampInstance = Instantiate(rampPrefab, rampContainer);
            rampInstance.transform.localScale = Vector3.one * platformScale;

            CylinderPlatformObj rampCell = rampInstance.GetComponent<CylinderPlatformObj>();
            if (rampCell != null)
            {
                // Reutiliza Init() para activar visuales/collider y guardar los datos base; la
                // posicion/rotacion final se sobreescribe justo despues para que cada escalon
                // quede en su punto de la escalinata en vez de en el nivel entero.
                int dataLevel = i < segments ? level : nextLevel;
                rampCell.Init(new TowerPlatform(dataLevel, startAngle), radius, levelHeight, basePosition);
            }

            rampInstance.transform.position = points[i];

            // Cada escalon mira hacia el siguiente para que la inclinacion sea continua; el
            // ultimo mantiene la direccion del tramo anterior, ya que no hay un punto despues.
            Vector3 lookTarget = i < segments ? points[i + 1] : points[i] + (points[i] - points[i - 1]);
            Vector3 lookDirection = lookTarget - rampInstance.transform.position;
            if (lookDirection != Vector3.zero)
                rampInstance.transform.rotation = Quaternion.LookRotation(lookDirection.normalized);
        }
    }

    Dictionary<int, List<OccupiedSlot>> CloneOccupiedSlots(Dictionary<int, List<OccupiedSlot>> source)
    {
        Dictionary<int, List<OccupiedSlot>> clone = new Dictionary<int, List<OccupiedSlot>>();

        if (source == null) return clone;

        foreach (KeyValuePair<int, List<OccupiedSlot>> entry in source)
        {
            clone[entry.Key] = new List<OccupiedSlot>(entry.Value);
        }

        return clone;
    }

    void AddOccupiedSlot(Dictionary<int, List<OccupiedSlot>> occupied, int level, float angle, float halfWidthAngle)
    {
        if (!occupied.TryGetValue(level, out List<OccupiedSlot> slots))
        {
            slots = new List<OccupiedSlot>();
            occupied[level] = slots;
        }

        slots.Add(new OccupiedSlot { angle = angle, halfWidthAngle = halfWidthAngle });
    }

    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    // Elimina todas las rampas generadas (para regenerar)
    public void ClearRamps()
    {
        if (rampContainer == null) return;

        for (int i = rampContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(rampContainer.GetChild(i).gameObject);
        }
    }

    // Dibuja cada linea start-a-end probada en la ultima generacion: verde si quedo libre (se
    // acepto ese candidato), rojo si se detecto otra plataforma en medio (se descarto y se
    // probo otro angulo), con una esfera amarilla en el punto exacto del impacto que la bloqueo.
    void OnDrawGizmos()
    {
        if (!showDebugRays || debugChecks == null) return;

        foreach (DebugLineCheck check in debugChecks)
        {
            Gizmos.color = check.blocked ? Color.red : Color.green;
            Gizmos.DrawLine(check.from, check.to);
            Gizmos.DrawSphere(check.from, 0.1f);
            Gizmos.DrawSphere(check.to, 0.1f);

            if (check.blocked)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(check.blockingPoint, 0.2f);
            }
        }
    }
}
