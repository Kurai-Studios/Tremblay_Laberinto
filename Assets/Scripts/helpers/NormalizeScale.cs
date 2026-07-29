using UnityEngine;

[ExecuteInEditMode]
public class NormalizeScale : MonoBehaviour
{
    [ContextMenu("Aplicar escala como 1:1")]
    void ApplyScale()
    {
        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogWarning("No hay MeshFilter en este objeto.");
            return;
        }

        var mesh = Instantiate(meshFilter.sharedMesh); // Duplicar la malla
        Vector3 scale = transform.localScale;

        Vector3[] verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = Vector3.Scale(verts[i], scale);
        }
        mesh.vertices = verts;
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        transform.localScale = Vector3.one;

        Debug.Log($"Escala aplicada en {name}. Ahora su escala es 1:1");
    }
}
