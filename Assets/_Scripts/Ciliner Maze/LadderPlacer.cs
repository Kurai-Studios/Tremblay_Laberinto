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
    [Tooltip("Desviacion maxima (grados) que la conexion entre anchors puede tener respecto a la vertical pura para seguir considerandose valida. Una escalera solo conecta anchors practicamente uno encima del otro; si el angulo real es mayor a esto, se considera diagonal y no se coloca (esa conexion queda para el puzzle path)")]
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

    private enum DebugResult { Placed, BlockedByPlatform, TooDiagonal }

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

    // Coloca una escalera entre cada par de plataformas principales de niveles consecutivos
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

        foreach (KeyValuePair<int, CylinderPlatformObj> entry in mainPathPlatforms)
        {
            int nextLevel = entry.Key + 1;

            if (!mainPathPlatforms.TryGetValue(nextLevel, out CylinderPlatformObj upperPlatform))
                continue;

            PlaceLadderBetween(entry.Value, upperPlatform);
        }
    }

    // Instancia y orienta una escalera entre los anchors mas cercanos de dos plataformas, si la
    // conexion resulta suficientemente vertical y no hay nada fisicamente en medio. El camino
    // principal (RuleManager.GeneratePath) ya limita cuanto puede cambiar el angulo entre un
    // nivel y el siguiente para que esto casi siempre sea posible; no hay fallback aqui si de
    // todos modos no lo es (p. ej. por quedar bloqueada), esa conexion simplemente queda sin escalera.
    void PlaceLadderBetween(CylinderPlatformObj lowerPlatform, CylinderPlatformObj upperPlatform)
    {
        if (lowerPlatform == null || upperPlatform == null) return;

        Transform lowerAnchor = GetClosestAnchor(lowerPlatform, upperPlatform.transform.position);
        Transform upperAnchor = GetClosestAnchor(upperPlatform, lowerPlatform.transform.position);

        if (lowerAnchor == null || upperAnchor == null) return;

        Vector3 bottom = lowerAnchor.position;
        Vector3 top = upperAnchor.position;

        // Una escalera solo conecta anchors casi uno encima del otro: si la conexion se desvia
        // demasiado de la vertical, se descarta; esa conexion diagonal le corresponde al puzzle
        // path, no a una escalera.
        float tiltFromVertical = Vector3.Angle(top - bottom, Vector3.up);
        if (tiltFromVertical > maxTiltFromVertical)
        {
            debugChecks.Add(new DebugLineCheck { from = bottom, to = top, result = DebugResult.TooDiagonal });
            return;
        }

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
    // escalera, rojo si se detecto otra plataforma en medio, naranja si se descarto por quedar
    // diagonal (fuera de maxTiltFromVertical); en el caso rojo se marca ademas con una esfera
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
