using UnityEngine;

// Expone el/los punto/s de anclaje del prefab de escalera, con el mismo patron que
// CylinderPlatformObj.anchorL/anchorR: un Transform hijo asignado a mano en el prefab, en vez de
// depender del pivote raiz del modelo o de buscar por nombre. LadderPlacer usa 'topAnchor' para
// saber exactamente que punto del prefab debe terminar coincidiendo con el anchor de la
// plataforma de destino (arriba), sea cual sea la posicion real del pivote del mesh.
public class LadderConnector : MonoBehaviour
{
    [Header("Ladder Connection")]
    [Tooltip("Punto de la escalera que debe quedar exactamente sobre el anchor de la plataforma de destino (arriba). LadderPlacer traslada toda la escalera para que este punto coincida con ese anchor, despues de rotarla para que quede colgando hacia la plataforma de abajo.")]
    [SerializeField] Transform topAnchor;

    public Transform GetTopAnchor()
    {
        return topAnchor;
    }
}
