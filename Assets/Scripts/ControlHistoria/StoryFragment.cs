using UnityEngine;

/// <summary>
/// ScriptableObject que define un fragmento de historia
/// Puede ser reproducido al descifrar o completar una misión
/// </summary>
[CreateAssetMenu(fileName = "NuevoFragmento", menuName = "Historia/Story Fragment")]
public class StoryFragment : ScriptableObject
{
    [Header("📖 Identificación")]
    [Tooltip("ID único del fragmento (ej: 'intro_cocina', 'cap1_revelacion')")]
    public string fragmentID;
    
    [Tooltip("Nombre descriptivo para el inspector")]
    public string nombreFragmento;
    
    [Header("🎵 Audio Narrativo")]
    [Tooltip("Clip de audio con la narración/diálogo")]
    public AudioClip audioNarracion;
    
    [Range(0f, 1f)]
    public float volumenAudio = 0.8f;
    
    [Header("📝 Texto")]
    [TextArea(3, 10)]
    [Tooltip("Texto del fragmento (subtítulos/diálogo)")]
    public string textoFragmento;
    
    [Header("⏱️ Timing")]
    [Tooltip("Si es 0, se calcula automáticamente desde la duración del audio")]
    public float duracionTotal = 0f;
    
    [Tooltip("Tiempo de fade in al inicio (segundos)")]
    public float tiempoFadeIn = 0.5f;
    
    [Tooltip("Tiempo de fade out al final (segundos)")]
    public float tiempoFadeOut = 0.5f;
    
    [Tooltip("Si está activado, el fragmento avanza automáticamente. Si no, requiere botón 'Continuar'")]
    public bool avanceAutomatico = true;
    
    [Header("🎨 Configuración Visual")]
    [Tooltip("Color del fondo del panel")]
    public Color colorFondo = new Color(0f, 0f, 0f, 0.85f);
    
    [Tooltip("Color del texto")]
    public Color colorTexto = Color.white;
    
    [Tooltip("Tamaño de fuente")]
    [Range(14, 48)]
    public int tamañoFuente = 24;
    
    [Tooltip("Usar efecto typewriter (letra por letra)")]
    public bool usarTypewriter = true;
    
    [Tooltip("Velocidad del typewriter (caracteres por segundo)")]
    [Range(10, 100)]
    public float velocidadTypewriter = 40f;
    
    [Header("🎬 Efectos Adicionales")]
    [Tooltip("Prefab de partículas/efectos visuales (opcional)")]
    public GameObject efectoVisualPrefab;
    
    [Tooltip("Imagen de fondo opcional")]
    public Sprite imagenFondo;
    
    [Tooltip("Intensidad del blur/viñeta")]
    [Range(0f, 1f)]
    public float intensidadViñeta = 0.3f;
    
    [Header("🔗 Encadenamiento")]
    [Tooltip("Fragmento que se reproduce automáticamente después de este")]
    public StoryFragment siguienteFragmento;
    
    [Tooltip("Delay antes de reproducir el siguiente fragmento (segundos)")]
    public float delayAntesSiguiente = 1f;
    
    [Header("📊 Metadata")]
    [Tooltip("Categoría del fragmento (para organización)")]
    public CategoriaFragmento categoria = CategoriaFragmento.Principal;
    
    [Tooltip("Prioridad de reproducción (mayor = más importante)")]
    [Range(0, 10)]
    public int prioridad = 5;
    
    /// <summary>
    /// Obtiene la duración total del fragmento
    /// </summary>
    public float ObtenerDuracionTotal()
    {
        if (duracionTotal > 0f)
            return duracionTotal;
        
        if (audioNarracion != null)
            return audioNarracion.length + tiempoFadeIn + tiempoFadeOut;
        
        // Estimación basada en texto si no hay audio (150 palabras por minuto)
        if (!string.IsNullOrEmpty(textoFragmento))
        {
            float palabras = textoFragmento.Split(' ').Length;
            return (palabras / 150f) * 60f + tiempoFadeIn + tiempoFadeOut;
        }
        
        return tiempoFadeIn + tiempoFadeOut + 5f; // Duración mínima
    }
    
    /// <summary>
    /// Valida que el fragmento esté correctamente configurado
    /// </summary>
    public bool EsValido()
    {
        if (string.IsNullOrEmpty(fragmentID))
        {
            Debug.LogWarning($"[StoryFragment] Fragmento sin ID: {name}");
            return false;
        }
        
        if (audioNarracion == null && string.IsNullOrEmpty(textoFragmento))
        {
            Debug.LogWarning($"[StoryFragment] Fragmento sin audio ni texto: {fragmentID}");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Obtiene información del fragmento para debug
    /// </summary>
    public string ObtenerInfo()
    {
        string info = $"📖 {fragmentID}\n";
        info += $"   Nombre: {nombreFragmento}\n";
        info += $"   Audio: {(audioNarracion != null ? audioNarracion.name : "ninguno")}\n";
        info += $"   Duración: {ObtenerDuracionTotal():F2}s\n";
        info += $"   Typewriter: {usarTypewriter}\n";
        info += $"   Avance Auto: {avanceAutomatico}\n";
        info += $"   Categoría: {categoria}\n";
        
        if (siguienteFragmento != null)
            info += $"   → Siguiente: {siguienteFragmento.fragmentID}\n";
        
        return info;
    }
    
    [ContextMenu("📖 Mostrar Información")]
    public void MostrarInformacion()
    {
        Debug.Log(ObtenerInfo());
    }
    
    [ContextMenu("✅ Validar Fragmento")]
    public void ValidarFragmento()
    {
        if (EsValido())
        {
            Debug.Log($"✅ Fragmento válido: {fragmentID}");
        }
        else
        {
            Debug.LogError($"❌ Fragmento inválido: {fragmentID}");
        }
    }
}

[System.Serializable]
public enum CategoriaFragmento
{
    Principal,      // Historia principal
    Secundaria,     // Subtramas
    Tutorial,       // Instrucciones/ayuda
    Ambiental,      // Detalles del mundo
    Logro          // Celebraciones
}