using UnityEngine;

public class CylinderPlatformObj : MonoBehaviour
{
    [Header("Platform Visuals")]
    [SerializeField] GameObject platformMesh;      // El mesh principal de la plataforma
    [SerializeField] GameObject platformCollider;  // El collider de la plataforma

    private TowerPlatform platformData;
    private Vector3 worldPosition;
    private Vector3 basePosition;
    private float radius;
    private float levelHeight;

    // Inicializa la plataforma con los datos generados
    public void Init(TowerPlatform data, float cylinderRadius, float heightOffset, float levelHeight, Vector3 basePosition)
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

        Debug.Log($"Calculando posición - Ángulo: {angle}, Nivel: {level}");

        // Convertir angulo a radianes
        float angleRad = angle * Mathf.Deg2Rad;

        // Calcular posicion en el circulo
        float localX = radius * Mathf.Cos(angleRad);
        float localZ = radius * Mathf.Sin(angleRad);
        float localY = level * levelHeight;

        Vector3 position = new Vector3
          ( basePosition.x + localX,
            basePosition.y + localY,
            basePosition.z + localZ );

        Debug.Log($"Posición calculada: {position}");

        return position;
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
