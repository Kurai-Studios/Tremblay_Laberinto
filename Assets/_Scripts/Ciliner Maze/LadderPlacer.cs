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

    // Contenedor propio para las escaleras generadas. LadderPlacer y CylinderRender viven en el
    // mismo GameObject (comparten "transform"), asi que las escaleras NO pueden parentearse
    // directamente a "transform": ClearLadders() borraria tambien el cilindro y las plataformas
    // de CylinderRender. Este contenedor mantiene ambos sistemas aislados entre si.
    private Transform ladderContainer;

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

    // Instancia y orienta una escalera entre los anchors mas cercanos de dos plataformas
    void PlaceLadderBetween(CylinderPlatformObj lowerPlatform, CylinderPlatformObj upperPlatform)
    {
        if (lowerPlatform == null || upperPlatform == null) return;

        Transform lowerAnchor = GetClosestAnchor(lowerPlatform, upperPlatform.transform.position);
        Transform upperAnchor = GetClosestAnchor(upperPlatform, lowerPlatform.transform.position);

        if (lowerAnchor == null || upperAnchor == null) return;

        Vector3 bottom = lowerAnchor.position;
        Vector3 top = upperAnchor.position;

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
}
