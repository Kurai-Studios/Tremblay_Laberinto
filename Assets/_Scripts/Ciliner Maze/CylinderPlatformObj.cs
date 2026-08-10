using UnityEngine;

public class CylinderPlatformObj : MonoBehaviour
{
    [Header("Platform Tags")]
    [Tooltip("Tags usados por el sistema de reglas para identificar el tipo/categoria de esta plataforma")]
    [SerializeField] private string[] tags;

    [Header("Platform Visuals")]
    [SerializeField] GameObject platformMesh;      // El mesh principal de la plataforma
    [SerializeField] GameObject platformCollider;  // El collider de la plataforma

    [Header("Ladder Connection")]
    [Tooltip("Puntos de anclaje L/R donde puede conectar una escalera hacia/desde esta plataforma. El LadderPlacer elige el mas cercano a la otra plataforma a conectar.")]
    [SerializeField] Transform anchorL;
    [SerializeField] Transform anchorR;

    private TowerPlatform platformData;
    private Vector3 worldPosition;
    private Vector3 basePosition;
    private float radius;
    private float levelHeight;

    // Inicializa la plataforma con los datos generados
    public void Init(TowerPlatform data, float cylinderRadius, float levelHeight, Vector3 basePosition)
    {
        // Guardar referencia a los datos
        platformData = data;
        this.radius = cylinderRadius;
        this.levelHeight = levelHeight;
        this.basePosition = basePosition;

        // Calcular posicion en el mundo
        worldPosition = CalculateWorldPosition(data.angle, data.level);

        // Posicionar la plataforma
        transform.position = worldPosition;

        // Orientar la plataforma hacia afuera del cilindro
        OrientPlatform(data.angle);

        // Activar/desactivar elementos visuales segun el estado
        UpdateVisuals();
    }

    // Calcula la posicion en el mundo de la plataforma
    Vector3 CalculateWorldPosition(float angle, int level)
    {
        return ComputeWorldPosition(angle, level, radius, levelHeight, basePosition);
    }

    // Formula compartida angulo/nivel -> posicion en el mundo. Publica y estatica para que otros
    // sistemas (como PuzzlePathPlacer) la reutilicen sin necesitar una instancia de esta clase.
    public static Vector3 ComputeWorldPosition(float angle, int level, float radius, float levelHeight, Vector3 basePosition)
    {
        // Convertir angulo a radianes
        float angleRad = angle * Mathf.Deg2Rad;

        // Calcular posicion en el circulo
        float localX = radius * Mathf.Cos(angleRad);
        float localZ = radius * Mathf.Sin(angleRad);
        float localY = level * levelHeight;

        return new Vector3(
            basePosition.x + localX,
            basePosition.y + localY,
            basePosition.z + localZ);
    }

    // Orienta la plataforma para que mire hacia afuera del cilindro
    void OrientPlatform(float angle)
    {
        // Hacer que la plataforma mire hacia afuera del cilindro
        Vector3 direction = (transform.position - Vector3.zero).normalized;
        direction.y = 0; // Mantener la plataforma horizontal

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        else
        {
            // Fallback: usar el angulo
            transform.rotation = Quaternion.Euler(0, angle, 0);
        }

        // Asegurar que la plataforma este horizontal
        Vector3 currentRotation = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0, currentRotation.y, 0);
    }

    void UpdateVisuals()
    {
        // Activar mesh principal
        if (platformMesh != null)
            platformMesh.SetActive(true);

        // Activar collider
        if (platformCollider != null)
            platformCollider.SetActive(true);
    }

    // Obtiene la posicion mundial de la plataforma
    public Vector3 GetWorldPosition()
    {
        return worldPosition;
    }

    // Obtiene los datos de la plataforma
    public TowerPlatform GetPlatformData()
    {
        return platformData;
    }

    // Verifica si la plataforma tiene un tag especifico (usado por el sistema de reglas)
    public bool HasTag(string tag)
    {
        if (tags == null || string.IsNullOrEmpty(tag)) return false;

        foreach (string t in tags)
        {
            if (string.Equals(t, tag, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    // Obtiene todos los tags de la plataforma
    public string[] GetTags()
    {
        return tags;
    }

    // Anchors L/R usados por LadderPlacer para conectar escaleras
    public Transform GetAnchorL()
    {
        return anchorL;
    }

    public Transform GetAnchorR()
    {
        return anchorR;
    }

    // Ancho tangencial (mitad) de la plataforma, medido entre sus anchors L/R en espacio local
    // (sin escala aplicada). Usado por CylinderRender para encadenar plataformas del mismo nivel
    // por sus anchors, sin superponerse, sea cual sea el tamaño del prefab.
    public float GetTangentialHalfWidth()
    {
        if (anchorL == null || anchorR == null) return 0f;
        return Vector3.Distance(anchorL.localPosition, anchorR.localPosition) / 2f;
    }

    /*void OnDrawGizmosSelected()
    {
        if (platformData != null)
        {
            // Dibujar un gizmo para visualizar la plataforma en el editor
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(worldPosition, 0.5f);

            // Dibujar linea desde el centro del cilindro hasta la plataforma
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(Vector3.zero, worldPosition);
        }
    }*/
}
