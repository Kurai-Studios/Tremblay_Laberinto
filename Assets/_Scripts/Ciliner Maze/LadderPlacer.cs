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

    // Niveles (el inferior de cada conexion, mismo criterio de 'level' que PlaceLadders) donde NO
    // se pudo colocar una escalera de salida hacia el nivel siguiente -- por cualquiera de los
    // motivos de PlaceLadderBetween (sin anchors, demasiado diagonal, o bloqueada por otra
    // plataforma). Con el camino apuntando siempre a garantizar escalera (ver RuleManager.GeneratePath)
    // esto deberia quedar vacio en una generacion sana; se expone igual como diagnostico.
    private readonly HashSet<int> failedConnections = new HashSet<int>();

    // Por cada conexion (misma clave 'level' que 'failedConnections'), el par de anchors mas
    // vertical que se encontro -- aunque la escalera se haya terminado descartando por quedar
    // demasiado diagonal o bloqueada. Diagnostico: permite inspeccionar el mejor candidato incluso
    // en una conexion fallida.
    private readonly Dictionary<int, (Transform lower, Transform upper)> connectorAnchorsByLevel =
        new Dictionary<int, (Transform lower, Transform upper)>();

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

            bool placed = PlaceLadderBetween(level, mainPathPlatforms[level], upperPlatform, incomingAnchorUsed);
            if (!placed)
                failedConnections.Add(level);
        }
    }

    // Busca, entre los anchors de la plataforma de abajo (la peticion nace ahi) y los de la de
    // arriba (el destino), el par que quede mas vertical posible. Cada CylinderPlatformObj ya
    // devuelve el anchor EFECTIVO (ver CylinderPlatformObj.GetAnchorL/R): si tiene una cadena de
    // extras encadenada de ese lado, es el extremo libre del ultimo eslabon, no el suyo propio
    // (que esta tocado por la cadena) -- asi que aca no hace falta saber nada de eso. El anchor de
    // abajo tampoco puede ser el mismo que ya reclamo la escalera de ENTRADA de esa plataforma (ver
    // 'incomingAnchorUsed' / comentario en PlaceLadders). La peticion solo es valida si el mejor
    // par encontrado queda dentro de 'maxTiltFromVertical' grados de la vertical pura (90 grados,
    // sin angulo); si no hay ningun par de anchors, o el mas vertical de ellos sigue quedando
    // demasiado diagonal, este tramo del camino se queda sin escalera de salida.
    // Devuelve true si la escalera de salida de 'lowerPlatform' quedo colocada; false en
    // cualquiera de los casos que dejan ese tramo sin escalera (ver los distintos 'return' de
    // abajo), para que PlaceLadders() pueda registrarlo en 'failedConnections'.
    bool PlaceLadderBetween(int level, CylinderPlatformObj lowerPlatform, CylinderPlatformObj upperPlatform, Dictionary<CylinderPlatformObj, Transform> incomingAnchorUsed)
    {
        if (lowerPlatform == null || upperPlatform == null) return false;

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
            if (lowerAnchor == lowerIncomingAnchor) continue;

            foreach (Transform upperAnchor in upperCandidates)
            {
                if (upperAnchor == null) continue;

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
            return false;
        }

        // Se guarda el mejor par libre encontrado ANTES de descartarlo por angulo/bloqueo, como
        // diagnostico de que hubiera sido el candidato mas cercano aunque la conexion haya fallado.
        connectorAnchorsByLevel[level] = (bestLower, bestUpper);

        // Regla: las escaleras deben quedar verticales (90 grados, sin ningun angulo). Aunque
        // 'bestLower'/'bestUpper' sea el par mas vertical entre los libres, si aun asi se pasa de
        // 'maxTiltFromVertical' la peticion no es valida.
        if (bestTilt > maxTiltFromVertical)
        {
            debugChecks.Add(new DebugLineCheck { from = bestLower.position, to = bestUpper.position, result = DebugResult.TooDiagonal });
            return false;
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
            return false;

        // Regla: si la escalera termino conectando el mismo lado (L con L, o R con R) en vez del
        // caso habitual (un anchor L con un R), se agrega una plataforma extra pegada a ese mismo
        // anchor de la plataforma de ABAJO (el nivel que "pidio" la escalera) -- puramente de
        // relleno, no afecta la escalera recien colocada ni ninguna otra.
        if (bestLower == lowerPlatform.GetAnchorL() && bestUpper == upperPlatform.GetAnchorL())
            cylinderRender.SpawnExtraBesideAnchor(lowerPlatform, true);
        else if (bestLower == lowerPlatform.GetAnchorR() && bestUpper == upperPlatform.GetAnchorR())
            cylinderRender.SpawnExtraBesideAnchor(lowerPlatform, false);

        // La escalera "nace" en su destino: se orienta para que su cuerpo cuelgue desde el Top
        // Anchor hacia abajo, y despues se traslada entera para que ese Top Anchor (ver
        // LadderConnector, asignado a mano en el prefab -- no se asume que el pivote raiz del mesh
        // sea ese punto) quede exactamente sobre el anchor libre de la plataforma de arriba, a
        // donde llega el jugador subiendo. El prefab esta modelado con su eje local +Y apuntando
        // desde la base del modelo HACIA el Top Anchor (confirmado empiricamente: con el prefab
        // sin rotar, el Top Anchor tiene Y local positivo respecto del pivote) -- por eso ese eje
        // debe alinearse con la direccion real de SUBIDA (de abajo hacia arriba), no de bajada:
        // alinearlo con la bajada (como se hizo en un intento anterior) deja el cuerpo de la
        // escalera flotando arriba del Top Anchor en vez de colgando hacia la plataforma inferior.
        //
        // FromToRotation solo fija ese eje Y: deja un "giro" (roll) arbitrario alrededor suyo, asi
        // que la cara ANCHA del prefab (medida por bounds: ~0.09 de espesor en Z local contra ~1.07
        // y ~2.77 en X/Y -- el eje local +Z es la normal de esa cara ancha) terminaba apuntando en
        // cualquier direccion, a veces derecho hacia el cilindro. Se usa LookRotation en su lugar
        // para fijar tambien ese giro: el forward (+Z, la normal de la cara ancha) se fija a la
        // direccion TANGENCIAL (perpendicular al radio, "a lo largo" de la curva del cilindro en
        // ese punto -- Cross(up, radial)), NO a la direccion radial -- la cara ancha no debe
        // apuntar ni hacia adentro ni hacia afuera del cilindro, sino de costado. El upwards sigue
        // siendo la direccion real de subida (LookRotation ortogonaliza el upwards contra el
        // forward, asi que el eje Y de subida queda correcto igual).
        GameObject ladderInstance = Instantiate(ladderPrefab, ladderContainer);
        ladderInstance.transform.localPosition = Vector3.zero;

        Vector3 upDirection = top - bottom;
        Vector3 radialDirection = (top + bottom) * 0.5f;
        radialDirection.y = 0f;
        Vector3 tangentDirection = Vector3.Cross(Vector3.up, radialDirection);

        if (tangentDirection.sqrMagnitude > 0.0001f && upDirection.sqrMagnitude > 0.0001f)
            ladderInstance.transform.rotation = Quaternion.LookRotation(tangentDirection.normalized, upDirection.normalized);
        else if (upDirection.sqrMagnitude > 0.0001f)
            ladderInstance.transform.rotation = Quaternion.FromToRotation(Vector3.up, upDirection.normalized);

        // Alinear el Top Anchor real (ya rotado) con el punto de destino, en vez de asumir que el
        // pivote raiz del prefab es ese punto. Sin LadderConnector (prefab viejo/sin configurar),
        // cae de vuelta al comportamiento anterior: el propio pivote raiz se usa como "top".
        LadderConnector connector = ladderInstance.GetComponent<LadderConnector>();
        Transform topAnchor = connector != null ? connector.GetTopAnchor() : null;
        if (topAnchor != null)
            ladderInstance.transform.position += top - topAnchor.position;
        else
            ladderInstance.transform.position = top;

        // Nota: por ahora no se escala la escalera para que coincida exactamente con la
        // distancia entre anchors (estirar el prefab vs. longitud fija es una decision de
        // reglas que se define junto con el resto del sistema de creacion del prefab).
        return true;
    }

    // Elimina todas las escaleras generadas (para regenerar). Solo toca su propio contenedor,
    // nunca "transform" directamente (compartido con CylinderRender). Tambien limpia el
    // diagnostico ('failedConnections'/'connectorAnchorsByLevel') para no dejar datos de una
    // generacion anterior si las escaleras se desactivan sin volver a llamar a PlaceLadders().
    public void ClearLadders()
    {
        failedConnections.Clear();
        connectorAnchorsByLevel.Clear();

        if (ladderContainer == null) return;

        for (int i = ladderContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(ladderContainer.GetChild(i).gameObject);
        }
    }

    // Niveles (el inferior de cada conexion) sin escalera de salida hacia el nivel siguiente en la
    // ultima generacion. Diagnostico: deberia quedar vacio salvo error de generacion.
    public HashSet<int> GetLevelsWithoutLadder()
    {
        return failedConnections;
    }

    // Anchors mas cercanos a la vertical (uno de la plataforma de abajo, uno de la de arriba) que
    // se encontraron para la conexion 'level' -> 'level+1', aunque no haya llegado a colocarse
    // escalera ahi. Devuelve false si esa conexion nunca tuvo ni siquiera un par de anchors
    // (NoFreeAnchorPair). Diagnostico.
    public bool TryGetConnectorAnchors(int level, out Transform lowerAnchor, out Transform upperAnchor)
    {
        if (connectorAnchorsByLevel.TryGetValue(level, out (Transform lower, Transform upper) pair))
        {
            lowerAnchor = pair.lower;
            upperAnchor = pair.upper;
            return true;
        }

        lowerAnchor = null;
        upperAnchor = null;
        return false;
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
