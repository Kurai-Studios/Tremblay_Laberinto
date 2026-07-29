using UnityEngine;

/// <summary>
/// Recibe el hit del InputRouter centralizado y delega al ObjectInfoUIManager global.
/// Ya no maneja InputActions propios — InputRouter hace UN solo raycast para toda la escena.
/// </summary>
public class ObjectDisplayController : MonoBehaviour
{
    [Header("ID del Objeto en JSON")]
    [SerializeField] private string objetoID;

    [Header("Debug")]
    [SerializeField] private bool mostrarDebug = true;

    private GameObjectManager gameObjectManager;

    void Start()
    {
        gameObjectManager = GameObjectManager.Instance;
        if (gameObjectManager == null)
        {
            Debug.LogError("No se encontró GameObjectManager en la escena!");
            return;
        }

        if (GetComponent<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            if (mostrarDebug) Debug.Log($"Collider agregado automáticamente a {gameObject.name}");
        }
    }

    /// <summary>
    /// Llamado por InputRouter cuando el raycast golpea el collider de este objeto.
    /// </summary>
    public void OnHit()
    {
        if (mostrarDebug) Debug.Log($"Hit detectado en {gameObject.name}!");

        var datos = gameObjectManager?.BuscarObjetoPorId(objetoID);
        if (datos != null)
            MostrarInformacionObjeto(datos);
        else
            Debug.LogError($"No se encontraron datos para ID: {objetoID}");
    }

    /// <summary>
    /// ✅ NUEVO MÉTODO: Delega al ObjectInfoUIManager sin manejar instancias locales
    /// </summary>
    private void MostrarInformacionObjeto(GameObjectData datos)
    {
        if (mostrarDebug) Debug.Log($"🖼 Mostrando información para {datos.nombreEspanol} (ID: {objetoID})");

        // ✅ Simplemente delegar al ObjectInfoUIManager global
        if (ObjectInfoUIManager.Instance != null)
        {
            GameObject canvasResult = ObjectInfoUIManager.Instance.MostrarSobreObjeto(gameObject, datos);
            
            if (canvasResult != null)
            {
                if (mostrarDebug) Debug.Log($"✅ Canvas global actualizado para {gameObject.name}");
            }
            else
            {
                Debug.LogError($"❌ Error al mostrar información para {gameObject.name}");
            }
        }
        else
        {
            Debug.LogError("❌ ObjectInfoUIManager.Instance no encontrado!");
        }
    }

    /// <summary>
    /// ✅ SIMPLIFICADO: Solo verifica si el canvas global está mostrando este objeto
    /// </summary>
    public bool TieneCanvasActivo()
    {
        if (ObjectInfoUIManager.Instance != null)
        {
            // Verificar si el canvas global está mostrando información de este objeto
            return ObjectInfoUIManager.Instance.TieneCanvasActivo() && 
                   ObjectInfoUIManager.Instance.ObjetoActualID == objetoID;
        }
        return false;
    }

    /// <summary>
    /// ✅ SIMPLIFICADO: Delega el cierre al ObjectInfoUIManager global
    /// </summary>
    public void CerrarCanvasExterno()
    {
        if (mostrarDebug) Debug.Log($"🚫 Solicitud de cierre para {gameObject.name}");
        
        // ✅ Solo cerrar si este objeto está siendo mostrado actualmente
        if (ObjectInfoUIManager.Instance != null && TieneCanvasActivo())
        {
            ObjectInfoUIManager.Instance.CerrarCanvas();
            if (mostrarDebug) Debug.Log($"✅ Canvas global cerrado desde {gameObject.name}");
        }
    }

    void OnDestroy()
    {
        if (TieneCanvasActivo())
            CerrarCanvasExterno();
    }

    // ✅ MÉTODOS DE DEBUG ACTUALIZADOS
    [ContextMenu("🎯 Debug Estado Objeto")]
    public void DebugEstadoObjeto()
    {
        Debug.Log("=== 🎯 ESTADO OBJETO ===");
        Debug.Log($"Objeto ID: {objetoID}");
        Debug.Log($"Nombre GameObject: {gameObject.name}");
        Debug.Log($"Canvas activo para este objeto: {TieneCanvasActivo()}");
        
        if (ObjectInfoUIManager.Instance != null)
        {
            Debug.Log($"Canvas global activo: {ObjectInfoUIManager.Instance.TieneCanvasActivo()}");
            Debug.Log($"Objeto actual en canvas: {ObjectInfoUIManager.Instance.ObjetoActualID}");
        }
        else
        {
            Debug.LogError("ObjectInfoUIManager.Instance no encontrado!");
        }
    }

    [ContextMenu("🧪 Test Mostrar Información")]
    public void TestMostrarInformacion()
    {
        var datos = gameObjectManager?.BuscarObjetoPorId(objetoID);
        if (datos != null)
        {
            MostrarInformacionObjeto(datos);
        }
        else
        {
            Debug.LogError($"No se encontraron datos para ID: {objetoID}");
        }
    }
}