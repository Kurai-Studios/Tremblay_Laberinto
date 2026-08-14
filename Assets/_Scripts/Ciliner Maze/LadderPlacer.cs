using System.Collections.Generic;
using UnityEngine;

// Coloca escaleras (o el prefab equivalente) conectando la plataforma principal de cada
// nivel del camino con la del nivel siguiente, usando los anchors L/R de cada plataforma
// para elegir el punto de conexion mas cercano a la otra plataforma.
// Que plataformas deben conectarse (todas, solo algunas, condicionado por reglas, etc.)
// se define mas adelante; por ahora conecta siempre niveles consecutivos del camino principal.
public class LadderPlacer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CylinderRender cylinderRender;

    [Header("Ladder Settings")]
    [Tooltip("Prefab de escalera (o similar) que se instancia entre dos plataformas de niveles consecutivos")]
    public GameObject ladderPrefab;
    [Tooltip("Tag que debe tener una plataforma para contar como bloqueo del raycast anchor-a-anchor. Solo las plataformas del camino principal (Normal/Trampa/Spawn/Final) la tienen, asi que otros objetos que el rayo pueda rozar no cancelan la escalera")]
    public string mainPathTag = "MainPath";
    [Tooltip("Ya no se usa para rechazar conexiones (ver PlaceLadderBetween): una escalera se coloca sin importar que tan diagonal quede la conexion entre anchors, siempre que no haya nada fisicamente en medio. Se mantiene el campo porque RuleManager todavia lo usa como referencia para decidir el paso 'alineado' del camino principal (variedad visual del espiral)")]
    [Range(0f, 45f)]
    public float maxTiltFromVertical = 5f;

    [Header("Debug")]
    [Tooltip("Dibuja en el Scene View cada linea anchor-a-anchor probada (verde = libre y se coloco la escalera, rojo = bloqueada por otra plataforma, naranja = descartada por quedar diagonal), con una esfera amarilla en el punto exacto del impacto que la bloqueo")]
    public bool showDebugRays = true;

    // Contenedor propio para las escaleras generadas. LadderPlacer y CylinderRender viven en el
    // mismo GameObject (comparten "transform"), asi que las escaleras NO pueden parentearse
    // directamente a "transform": ClearLadders() borraria tambien el cilindro y las plataformas
    // de CylinderRender. Este contenedor mantiene ambos sistemas aislados entre si.
    private Transform ladderContainer;

    private enum DebugResult { Placed, BlockedByPlatform, DestinationOccupied }

    // Registro de cada linea anchor-a-anchor probada en la ultima generacion, para dibujarla
    // como gizmo y poder ver si el raycast/chequeo de verticalidad esta funcionando como se espera.
    private struct DebugLineCheck
    {
        public Vector3 from;
        public Vector3 to;
        public DebugResult result;
        public Vector3 blockingPoint;
    }
    private readonly List<DebugLineCheck> debugChecks = new List<DebugLineCheck>();

    void EnsureContainer()
    {
        if (ladderContainer != null) return;

        GameObject containerObj = new GameObject("Ladders");
        containerObj.transform.SetParent(transform, false);
        ladderContainer = containerObj.transform;
    }

    // Coloca una escalera entre cada par de plataformas principales de niveles consecutivos, en
    // orden de nivel ascendente (ver 'incomingAnchorUsed' abajo).
    public void PlaceLadders()
    {
        ClearLadders();
        debugChecks.Clear();

        if (ladderPrefab == null || cylinderRender == null)
            return;

        EnsureContainer();

        Dictionary<int, CylinderPlatformObj> mainPathPlatforms = cylinderRender.GetMainPathPlatforms();
        if (mainPathPlatforms == null || mainPathPlatforms.Count == 0)
            return;

        // Que anchor (L o R) uso cada plataforma para SU PROPIA escalera de entrada (la que viene
        // del nivel anterior). Sin tope de inclinacion, GetClosestAnchor elige el anchor mas
        // cercano solo por distancia, y ese calculo depende unicamente de hacia que LADO gira el
        // camino en cada tramo (no de cuanto), asi que cuando el camino cambia de direccion en
        // una plataforma, su escalera de salida terminaria eligiendo el MISMO anchor que ya uso
        // su escalera de entrada (una escalera literalmente arriba de otra: el jugador sube y
        // sigue subiendo sin caminar nada). Se recorre en orden de nivel para que, al calcular la
        // escalera de salida de cada plataforma, ya se sepa que anchor reclamo su escalera de
        // entrada y forzar el otro si coinciden.
        Dictionary<CylinderPlatformObj, Transform> incomingAnchorUsed = new Dictionary<CylinderPlatformObj, Transform>();

        List<int> levels = new List<int>(mainPathPlatforms.Keys);
        levels.Sort();

        foreach (int level in levels)
        {
            int nextLevel = level + 1;

            if (!mainPathPlatforms.TryGetValue(nextLevel, out CylinderPlatformObj upperPlatform))
                continue;

            PlaceLadderBetween(mainPathPlatforms[level], upperPlatform, incomingAnchorUsed);
        }
    }

    // Instancia una escalera entre los anchors mas cercanos de dos plataformas, sin importar que
    // tan diagonal quede la conexion (ya no hay tope de inclinacion, ver maxTiltFromVertical) —
    // solo se descarta si hay algo fisicamente en medio, o si el anchor de destino (arriba) ya
    // esta ocupado (ver mas abajo). 'incomingAnchorUsed' evita que el anchor de salida de la
    // plataforma de abajo repita el anchor que ya uso su propia escalera de entrada (ver
    // comentario en PlaceLadders).
    void PlaceLadderBetween(CylinderPlatformObj lowerPlatform, CylinderPlatformObj upperPlatform, Dictionary<CylinderPlatformObj, Transform> incomingAnchorUsed)
    {
        if (lowerPlatform == null || upperPlatform == null) return;

        Transform lowerAnchor = GetClosestAnchor(lowerPlatform, upperPlatform.transform.position);
        Transform upperAnchor = GetClosestAnchor(upperPlatform, lowerPlatform.transform.position);

        if (lowerAnchor == null || upperAnchor == null) return;

        if (incomingAnchorUsed.TryGetValue(lowerPlatform, out Transform lowerIncomingAnchor) && lowerIncomingAnchor == lowerAnchor)
        {
            Transform otherLowerAnchor = OtherAnchor(lowerPlatform, lowerAnchor);
            if (otherLowerAnchor != null)
                lowerAnchor = otherLowerAnchor;
        }

        // El anchor de destino (arriba, donde el jugador llega subiendo) no puede tener ya una
        // plataforma extra encadenada pegada a el: la escalera terminaria llegando justo al
        // mismo punto que otra plataforma. Si esta ocupado, probar el otro anchor de la
        // plataforma de arriba; si ese tambien lo esta (o no existe), esta conexion se queda sin
        // escalera — no hay un tercer punto donde intentarlo.
        HashSet<Transform> occupiedAnchors = cylinderRender.GetOccupiedAnchors();
        if (occupiedAnchors != null && occupiedAnchors.Contains(upperAnchor))
        {
            Transform otherUpperAnchor = OtherAnchor(upperPlatform, upperAnchor);
            if (otherUpperAnchor == null || occupiedAnchors.Contains(otherUpperAnchor))
            {
                debugChecks.Add(new DebugLineCheck { from = lowerAnchor.position, to = upperAnchor.position, result = DebugResult.DestinationOccupied });
                return;
            }
            upperAnchor = otherUpperAnchor;
        }

        incomingAnchorUsed[upperPlatform] = upperAnchor;

        Vector3 bottom = lowerAnchor.position;
        Vector3 top = upperAnchor.position;

        // Si otra plataforma (p. ej. una extra encadenada del mismo nivel) queda fisicamente en
        // medio del tramo recto entre ambos anchors, no se coloca la escalera: quedaria
        // atravesandola o escondida detras suyo.
        bool blocked = CylinderPlatformObj.IsLineBlocked(bottom, top, out Vector3 blockingPoint, mainPathTag, lowerPlatform, upperPlatform);
        debugChecks.Add(new DebugLineCheck
        {
            from = bottom,
            to = top,
            result = blocked ? DebugResult.BlockedByPlatform : DebugResult.Placed,
            blockingPoint = blockingPoint
        });

        if (blocked)
            return;

        // Se mantiene la rotacion por defecto del prefab (sin inclinar hacia el otro anchor);
        // todas las escaleras quedan orientadas igual, tal como estan diseñadas en el prefab.
        GameObject ladderInstance = Instantiate(ladderPrefab, ladderContainer);
        ladderInstance.transform.position = (bottom + top) / 2f;

        // Nota: por ahora no se escala la escalera para que coincida exactamente con la
        // distancia entre anchors (estirar el prefab vs. longitud fija es una decision de
        // reglas que se define junto con el resto del sistema de creacion del prefab).
    }

    // De los dos anchors (L/R) de una plataforma, devuelve el mas cercano a una posicion objetivo
    Transform GetClosestAnchor(CylinderPlatformObj platform, Vector3 towardPosition)
    {
        Transform anchorL = platform.GetAnchorL();
        Transform anchorR = platform.GetAnchorR();

        if (anchorL == null) return anchorR;
        if (anchorR == null) return anchorL;

        float distL = Vector3.Distance(anchorL.position, towardPosition);
        float distR = Vector3.Distance(anchorR.position, towardPosition);

        return distL <= distR ? anchorL : anchorR;
    }

    // El anchor L o R de una plataforma que NO es el que se pasa (para probar la alternativa
    // cuando el primero elegido no sirve, por reuso o por estar ocupado).
    Transform OtherAnchor(CylinderPlatformObj platform, Transform anchor)
    {
        return anchor == platform.GetAnchorL() ? platform.GetAnchorR() : platform.GetAnchorL();
    }

    // Elimina todas las escaleras generadas (para regenerar). Solo toca su propio contenedor,
    // nunca "transform" directamente (compartido con CylinderRender).
    public void ClearLadders()
    {
        if (ladderContainer == null) return;

        for (int i = ladderContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(ladderContainer.GetChild(i).gameObject);
        }
    }

    // Dibuja cada linea anchor-a-anchor probada en la ultima generacion: verde si se coloco la
    // escalera, rojo si se detecto otra plataforma en medio, celeste si el anchor de destino ya
    // estaba ocupado por una plataforma extra; en el caso rojo se marca ademas con una esfera
    // amarilla el punto exacto del impacto que la bloqueo.
    void OnDrawGizmos()
    {
        if (!showDebugRays || debugChecks == null) return;

        foreach (DebugLineCheck check in debugChecks)
        {
            switch (check.result)
            {
                case DebugResult.Placed: Gizmos.color = Color.green; break;
                case DebugResult.BlockedByPlatform: Gizmos.color = Color.red; break;
                case DebugResult.DestinationOccupied: Gizmos.color = Color.cyan; break;
            }

            Gizmos.DrawLine(check.from, check.to);
            Gizmos.DrawSphere(check.from, 0.1f);
            Gizmos.DrawSphere(check.to, 0.1f);

            if (check.result == DebugResult.BlockedByPlatform)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(check.blockingPoint, 0.2f);
            }
        }
    }
}
