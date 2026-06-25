using UnityEngine;

public class CylinderData : MonoBehaviour
{
    [Header("Dimensiones del Cilindro")]
    public float altura = 10f;
    public float radio = 5f;

    [Header("Punto de Referencia para Plataformas")]
    public GameObject platformStartPoint;

    [Header("Información Adicional (Opcional)")]
    public string nombreCilindro = "Cilindro Base";
    public Color colorIdentificador = Color.white;

    // Metodo para obtener la posicion base de las plataformas
    public Vector3 GetPlatformStartPosition()
    {
        if (platformStartPoint != null)
        {
            return platformStartPoint.transform.position;
        }

        // usar la posición del cilindro
        Debug.LogWarning($"No hay platformStartPoint asignado en {gameObject.name}, usando transform.position");
        return transform.position;
    }

    /*void OnDrawGizmos()
    {
        // Dibujar el cilindro base
        Gizmos.color = colorIdentificador;
        Gizmos.DrawWireCube(transform.position + Vector3.up * altura / 2f,
                           new Vector3(radio * 2f, altura, radio * 2f));

        // Dibujar el punto de inicio de plataformas
        if (platformStartPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(platformStartPoint.transform.position, 0.5f);

            // Dibujar línea desde el centro hasta el punto de inicio
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, platformStartPoint.transform.position);

            // Dibujar un cubo para destacar el punto
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawCube(platformStartPoint.transform.position, new Vector3(1, 0.1f, 1));
        }
    }*/
}
