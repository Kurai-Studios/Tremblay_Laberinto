using UnityEngine;

public enum Idioma { Espanol, Ingles }

/// <summary>
/// Controlador de audio para ObjectInfo - Maneja reproducción de nombres y colores
/// </summary>
public class ObjectInfoAudioController : MonoBehaviour
{
    [Header("Audio System")]
    [SerializeField] private bool usarSistemaAudio = true;
    [SerializeField] private float volumenAudioObjetos = 0.8f;
    [SerializeField] private bool debugAudio = true;
    [SerializeField] private Idioma idiomaActual = Idioma.Espanol;

    public enum TipoAudio
    {
        Nombre,
        Color
    }

    /// <summary>
    /// Verifica si el objeto tiene audio disponible para el tipo especificado
    /// </summary>
    public bool TieneAudio(GameObjectData datos, TipoAudio tipo)
    {
        if (!usarSistemaAudio || datos == null) return false;

        string rutaAudio = ObtenerRutaAudio(datos, tipo);
        return !string.IsNullOrEmpty(rutaAudio);
    }

    /// <summary>
    /// Obtiene la ruta del audio según el idioma actual y tipo
    /// </summary>
    private string ObtenerRutaAudio(GameObjectData datos, TipoAudio tipo)
    {
        if (datos == null) return "";

        switch (tipo)
        {
            case TipoAudio.Nombre:
                return idiomaActual == Idioma.Ingles ? datos.audioNombreIngles : datos.audioNombreEspanol;
            
            case TipoAudio.Color:
                return idiomaActual == Idioma.Ingles ? datos.audioColorIngles : datos.audioColorEspanol;
            
            default:
                return "";
        }
    }

    /// <summary>
    /// Reproduce el audio del nombre del objeto
    /// </summary>
    public void ReproducirAudioNombre(GameObjectData datos) =>
        ReproducirAudio(datos, TipoAudio.Nombre);

    /// <summary>
    /// Reproduce el audio del color del objeto
    /// </summary>
    public void ReproducirAudioColor(GameObjectData datos) =>
        ReproducirAudio(datos, TipoAudio.Color);

    private void ReproducirAudio(GameObjectData datos, TipoAudio tipo)
    {
        if (!usarSistemaAudio || datos == null) return;

        string etiqueta   = tipo == TipoAudio.Nombre ? "AudioNombre" : "AudioColor";
        string rutaAudio  = ObtenerRutaAudio(datos, tipo);
        string etiquetaObj = tipo == TipoAudio.Nombre
            ? (idiomaActual == Idioma.Ingles ? datos.nombreIngles : datos.nombreEspanol)
            : (idiomaActual == Idioma.Ingles ? datos.colorIngles  : datos.colorEspanol);

        if (debugAudio)
        {
            Debug.Log($"🎵 [{etiqueta}] Intentando reproducir: {etiquetaObj}");
            Debug.Log($"🎵 [{etiqueta}] Ruta: '{rutaAudio}'");
        }

        if (string.IsNullOrEmpty(rutaAudio))
        {
            if (debugAudio) Debug.LogWarning($"🎵 [{etiqueta}] ⚠️ Ruta vacía para: {etiquetaObj}");
            return;
        }

        string rutaLimpia = LimpiarRutaAudio(rutaAudio);
        AudioClip clip = Resources.Load<AudioClip>(rutaLimpia);

        if (clip != null)
        {
            if (GlobalAudioManager.Instance != null)
            {
                GlobalAudioManager.Instance.ReproducirSonidoSFX(clip, volumenAudioObjetos);
                if (debugAudio) Debug.Log($"🎵 [{etiqueta}] ✅ Reproducido: {etiquetaObj}");
            }
            else
            {
                if (debugAudio) Debug.LogWarning($"🎵 [{etiqueta}] ⚠️ GlobalAudioManager no disponible");
            }
        }
        else
        {
            if (debugAudio) Debug.LogError($"🎵 [{etiqueta}] ❌ No se pudo cargar: '{rutaLimpia}'");
        }
    }

    private static string LimpiarRutaAudio(string ruta) =>
        ruta.Replace(".mp3", "").Replace(".wav", "").Replace(".ogg", "");

    // Getters públicos para acceder a configuración
    public bool UsarSistemaAudio => usarSistemaAudio;
    public float VolumenAudioObjetos => volumenAudioObjetos;
    public bool DebugAudio => debugAudio;
    public Idioma IdiomaActual => idiomaActual;

    /// <summary>
    /// Cambia el idioma del sistema de audio
    /// </summary>
    public void CambiarIdiomaAudio(Idioma nuevoIdioma)
    {
        idiomaActual = nuevoIdioma;
        if (debugAudio) Debug.Log($"🎵 [Sistema] Idioma cambiado a: {idiomaActual}");
    }

    /// <summary>
    /// Configura el volumen del sistema de audio de objetos
    /// </summary>
    public void ConfigurarVolumenAudio(float nuevoVolumen)
    {
        volumenAudioObjetos = Mathf.Clamp01(nuevoVolumen);
        if (debugAudio) Debug.Log($"🎵 [Sistema] Volumen configurado a: {volumenAudioObjetos}");
    }

    /// <summary>
    /// Activa o desactiva el sistema de audio
    /// </summary>
    public void ActivarSistemaAudio(bool activar)
    {
        usarSistemaAudio = activar;
        if (debugAudio) Debug.Log($"🎵 [Sistema] Sistema de audio: {(activar ? "ACTIVADO" : "DESACTIVADO")}");
    }

    // ==============================================
    // 🧪 MÉTODOS DE TESTING PARA AUDIO
    // ==============================================

    [ContextMenu("🎵 Test Cambiar a Inglés")]
    public void TestCambiarIngles()
    {
        CambiarIdiomaAudio(Idioma.Ingles);
    }

    [ContextMenu("🎵 Test Cambiar a Español")]
    public void TestCambiarEspanol()
    {
        CambiarIdiomaAudio(Idioma.Espanol);
    }

    [ContextMenu("🔍 Debug Sistema Audio")]
    public void DebugSistemaAudio()
    {
        Debug.Log("=== 🎵 ESTADO SISTEMA AUDIO ===");
        Debug.Log($"Sistema activo: {usarSistemaAudio}");
        Debug.Log($"Idioma actual: {idiomaActual}");
        Debug.Log($"Volumen objetos: {volumenAudioObjetos}");
        Debug.Log($"Debug activo: {debugAudio}");
        Debug.Log($"GlobalAudioManager: {GlobalAudioManager.Instance != null}");
    }
}
