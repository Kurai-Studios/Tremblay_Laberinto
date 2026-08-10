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
    [Tooltip("Angulo (grados) que recorre la rampa mientras sube de un nivel al siguiente; le da forma de rampa/espiral en vez de subir en linea recta")]
    public float rampAngleSpan = 40f;
    [Tooltip("Cuantos angulos candidatos se prueban por conexion de niveles antes de rendirse si no encuentra espacio libre")]
    public int placementAttempts = 12;

    // Contenedor propio para las rampas generadas, por la misma razon que LadderPlacer usa el
    // suyo: no se puede parentear directamente a "transform" (compartido con CylinderRender) o
    // ClearRamps() borraria tambien el cilindro y las plataformas.
    private Transform rampContainer;

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

        // Copia de trabajo de los espacios ocupados: arranca con lo que ya ocupa el camino
        // principal y se le suma cada rampa colocada, para que tampoco se superpongan entre si.
        Dictionary<int, List<OccupiedSlot>> occupied = CloneOccupiedSlots(cylinderRender.GetOccupiedSlots());

        Dictionary<int, CylinderPlatformObj> mainPath = cylinderRender.GetMainPathPlatforms();

        List<int> levels = new List<int>(mainPath.Keys);
        levels.Sort();

        foreach (int level in levels)
        {
            int nextLevel = level + 1;
            if (!mainPath.ContainsKey(nextLevel)) continue;

            if (!TryFindFreeConnection(occupied, level, nextLevel, halfWidthAngle, out float startAngle, out float endAngle))
                continue;

            PlaceRamp(rampPrefab, level, nextLevel, startAngle, endAngle, radius, levelHeight, basePosition, platformScale);

            AddOccupiedSlot(occupied, level, startAngle, halfWidthAngle);
            AddOccupiedSlot(occupied, nextLevel, endAngle, halfWidthAngle);
        }
    }

    // Busca un par de angulos (uno por nivel) donde quepa la rampa sin superponerse con nada ya
    // colocado en ninguno de los dos niveles, probando candidatos al azar
    bool TryFindFreeConnection(Dictionary<int, List<OccupiedSlot>> occupied, int level, int nextLevel,
                                float halfWidthAngle, out float startAngle, out float endAngle)
    {
        for (int attempt = 0; attempt < placementAttempts; attempt++)
        {
            float candidateStart = Random.Range(0f, 360f);
            float direction = Random.value < 0.5f ? 1f : -1f;
            float candidateEnd = NormalizeAngle(candidateStart + rampAngleSpan * direction);

            bool startFree = IsAngleFree(occupied, level, candidateStart, halfWidthAngle);
            bool endFree = IsAngleFree(occupied, nextLevel, candidateEnd, halfWidthAngle);

            if (startFree && endFree)
            {
                startAngle = candidateStart;
                endAngle = candidateEnd;
                return true;
            }
        }

        startAngle = 0f;
        endAngle = 0f;
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

    // Instancia la rampa entre las posiciones de los dos niveles y la inclina orientando su
    // "forward" hacia el otro extremo (en vez de solo horizontal, como las plataformas planas)
    void PlaceRamp(GameObject rampPrefab, int level, int nextLevel, float startAngle, float endAngle,
                    float radius, float levelHeight, Vector3 basePosition, float platformScale)
    {
        Vector3 startPos = CylinderPlatformObj.ComputeWorldPosition(startAngle, level, radius, levelHeight, basePosition);
        Vector3 endPos = CylinderPlatformObj.ComputeWorldPosition(endAngle, nextLevel, radius, levelHeight, basePosition);

        GameObject rampInstance = Instantiate(rampPrefab, rampContainer);
        rampInstance.transform.localScale = Vector3.one * platformScale;

        CylinderPlatformObj rampCell = rampInstance.GetComponent<CylinderPlatformObj>();
        if (rampCell != null)
        {
            // Reutiliza Init() para activar visuales/collider y guardar los datos base; la
            // posicion/rotacion final se sobreescribe justo despues para que quede inclinada
            // entre los dos niveles en vez de plana en uno solo.
            rampCell.Init(new TowerPlatform(level, startAngle), radius, levelHeight, basePosition);
        }

        rampInstance.transform.position = (startPos + endPos) / 2f;

        Vector3 direction = endPos - startPos;
        if (direction != Vector3.zero)
            rampInstance.transform.rotation = Quaternion.LookRotation(direction.normalized);
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
}
