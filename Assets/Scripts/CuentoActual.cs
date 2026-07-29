using UnityEngine;

/// <summary>
/// Identifica el cuento al que pertenece la escena actual.
/// Se coloca en el GameController de cada escena. GameObjectManager,
/// ScrollViewLoader y MissionManager lo consultan para filtrar contenido.
/// Si no existe instancia, los sistemas no filtran (compat hacia atrás).
/// </summary>
public class CuentoActual : MonoBehaviour
{
    public static CuentoActual Instance;

    [SerializeField] private string cuentoID = "halloween";

    public string CuentoID => cuentoID;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static string GetCuentoActual()
    {
        return Instance != null ? Instance.cuentoID : null;
    }
}
