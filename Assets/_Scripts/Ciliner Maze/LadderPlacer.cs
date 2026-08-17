using System.Collections.Generic;
using UnityEngine;

// Coloca escaleras (o el prefab equivalente) conectando la plataforma principal de cada
// nivel del camino con la del nivel siguiente. La "peticion" de escalera nace de un anchor
// LIBRE de la plataforma de ABAJO (el nivel de origen); se busca entre los anchors libres de
// la plataforma de ARRIBA (el destino) uno que quede exactamente encima (ver PlaceLadderBetween)
// — solo si existe ese par verticalmente alineado la peticion es valida, y la escalera en si se
// instancia en el anchor de destino (arriba).
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
    [Tooltip("Maxima desviacion (en grados) respecto de la vertical pura que puede tener la linea anchor-a-anchor para que se coloque una escalera. Las escaleras deben quedar verticales (90 grados, sin inclinacion): cualquier conexion mas diagonal que esto se descarta, aunque eso deje algunos niveles sin escalera de salida (ver PlaceLadderBetween). RuleManager tambien usa este mismo valor como referencia para decidir el paso 'alineado' del camino principal (variedad visual del espiral)")]
    [Range(0f, 45f)]
    public float maxTiltFromVertical = 5f;

    [Header("Debug")]
    [Tooltip("Dibuja en el Scene View cada linea probada (verde = se coloco la escalera, rojo = bloqueada por otra plataforma, celeste = sin anchors libres, naranja = descartada por quedar diagonal), con una esfera amarilla en el punto exacto del impacto que la bloqueo")]
    public bool showDebugRays = true;

    // Contenedor propio para las escaleras generadas. LadderPlacer y CylinderRender viven en el
    // mismo GameObject (comparten "transform"), asi que las escaleras NO pueden parentearse
    // directamente a "transform": ClearLadders() borraria tambien el cilindro y las plataformas
    // de CylinderRender. Este contenedor mantiene ambos sistemas aislados entre si.
    private Transform ladderContainer;

    private enum DebugResult { Placed, BlockedByPlatform, NoFreeAnchorPair, TooDiagonal }

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
        // del nivel anterior). Sin esto, la busqueda de par vertical de su escalera de SALIDA
        // podria volver a elegir ese mismo anchor (una escalera literalmente arriba de otra: el
        // jugador sube y sigue subiendo sin caminar nada). Se recorre en orden de nivel para que,
        // al buscar la escalera de salida de cada plataforma, ya se sepa que anchor reclamo su
        // escalera de entrada y excluirlo de la busqueda (ver PlaceLadderBetween).
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

    // Busca, entre TODOS los anchors LIBRES de la plataforma de abajo (la peticion nace ahi) y
    // TODOS los anchors libres de la de arriba (el destino), el par que quede mas vertical
    // posible. Un anchor "libre" es uno que ninguna plataforma extra encadenada ya esta tocando
    // (ver 'occupiedAnchors'); ademas, el anchor de abajo no puede ser el mismo que ya reclamo la
    // escalera de ENTRADA de esa plataforma (ver 'incomingAnchorUsed' / comentario en PlaceLadders).
    // La peticion solo es valida si el mejor par encontrado queda dentro de 'maxTiltFromVertical'
    // grados de la vertical pura (90 grados, sin angulo); si no hay ningun par de anchors libres,
    // o el mas vertical de ellos sigue quedando demasiado diagonal, este tramo del camino se
    // queda sin escalera de salida — intencional, aunque deje algunos niveles sin conexion (el
    // hueco resultante debera cubrirlo el sistema de puzzle path mas adelante).
    void PlaceLadderBetween(CylinderPlatformObj lowerPlatform, CylinderPlatformObj upperPlatform, Dictionary<CylinderPlatformObj, Transform> incomingAnchorUsed)
    {
        if (lowerPlatform == null || upperPlatform == null) return;

        HashSet<Transform> occupiedAnchors = cylinderRender.GetOccupiedAnchors();
        incomingAnchorUsed.TryGetValue(lowerPlatform, out Transform lowerIncomingAnchor);

        Transform[] lowerCandidates = { lowerPlatform.GetAnchorL(), lowerPlatform.GetAnchorR() };
        Transform[] upperCandidates = { upperPlatform.GetAnchorL(), upperPlatform.GetAnchorR() };

        Transform bestLower = null;
        Transform bestUpper = null;
        float bestTilt = float.MaxValue;
        bool anyFreePair = false;

        foreach (Transform lowerAnchor in lowerCandidates)
        {
            if (lowerAnchor == null) continue;
            if (occupiedAnchors != null && occupiedAnchors.Contains(lowerAnchor)) continue;
            if (lowerAnchor == lowerIncomingAnchor) continue;

            foreach (Transform upperAnchor in upperCandidates)
            {
                if (upperAnchor == null) continue;
                if (occupiedAnchors != null && occupiedAnchors.Contains(upperAnchor)) continue;

                anyFreePair = true;

                float tilt = Vector3.Angle(Vector3.up, upperAnchor.position - lowerAnchor.position);
                if (tilt < bestTilt)
                {
                    bestTilt = tilt;
                    bestLower = lowerAnchor;
                    bestUpper = upperAnchor;
                }
            }
        }

        if (!anyFreePair)
        {
            debugChecks.Add(new DebugLineCheck { from = lowerPlatform.transform.position, to = upperPlatform.transform.position, result = DebugResult.NoFreeAnchorPair });
            return;
        }

        // Regla: las escaleras deben quedar verticales (90 grados, sin ningun angulo). Aunque
        // 'bestLower'/'bestUpper' sea el par mas vertical entre los libres, si aun asi se pasa de
        // 'maxTiltFromVertical' la peticion no es valida.
        if (bestTilt > maxTiltFromVertical)
        {
            debugChecks.Add(new DebugLineCheck { from = bestLower.position, to = bestUpper.position, result = DebugResult.TooDiagonal });
            return;
        }

        incomingAnchorUsed[upperPlatform] = bestUpper;

        Vector3 bottom = bestLower.position;
        Vector3 top = bestUpper.position;

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

        // La escalera "nace" en su destino: el pivote del prefab representa su extremo SUPERIOR,
        // asi que se coloca exactamente en el anchor libre de la plataforma de arriba (a donde
        // llega el jugador subiendo) y se orienta para bajar desde ahi hacia el anchor de la
        // plataforma de abajo. Se asume que el prefab esta modelado con su eje local +Y apuntando
        // "hacia arriba" de la escalera (convencion estandar de Unity); FromToRotation alinea ese
        // eje con la direccion real de bajada (de arriba hacia abajo) para que quede colgando
        // hacia la plataforma inferior en vez de flotando en el punto medio con rotacion por defecto.
        GameObject ladderInstance = Instantiate(ladderPrefab, ladderContainer);
        ladderInstance.transform.position = top;

        Vector3 downDirection = bottom - top;
        if (downDirection.sqrMagnitude > 0.0001f)
            ladderInstance.transform.rotation = Quaternion.FromToRotation(Vector3.up, downDirection.normalized);

        // Nota: por ahora no se escala la escalera para que coincida exactamente con la
        // distancia entre anchors (estirar el prefab vs. longitud fija es una decision de
        // reglas que se define junto con el resto del sistema de creacion del prefab).
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

    // Dibuja cada linea probada en la ultima generacion: verde si se coloco la escalera, rojo si
    // se detecto otra plataforma en medio, celeste si ningun par de anchors libres quedo
    // disponible entre ambas plataformas (origen y/o destino sin anchors libres), naranja si
    // habia anchors libres pero el mas vertical de ellos igual quedaba mas diagonal que
    // 'maxTiltFromVertical'; en el caso rojo se marca ademas con una esfera amarilla el punto
    // exacto del impacto que la bloqueo.
    void OnDrawGizmos()
    {
        if (!showDebugRays || debugChecks == null) return;

        foreach (DebugLineCheck check in debugChecks)
        {
            switch (check.result)
            {
                case DebugResult.Placed: Gizmos.color = Color.green; break;
                case DebugResult.BlockedByPlatform: Gizmos.color = Color.red; break;
                case DebugResult.NoFreeAnchorPair: Gizmos.color = Color.cyan; break;
                case DebugResult.TooDiagonal: Gizmos.color = new Color(1f, 0.5f, 0f); break;
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
