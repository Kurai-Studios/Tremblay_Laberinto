using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NuevaMision", menuName = "Misiones/Mision")]
public class Mission : ScriptableObject
{
    [TextArea(1, 5)]
    public string descripcion;
    
    public MissionPart[] partes;

    [Header("Sistema de Activación")]
    public TipoActivacion tipoActivacion = TipoActivacion.ActivaDesdeInicio;
    
    [Header("Condiciones de Activación")]
    [Tooltip("Para tipo 'AlGuardarObjeto': IDs de objetos que deben estar guardados")]
    public string[] objetosRequeridos = new string[0];
    
    [Tooltip("Para tipo 'AlDescifrarMision': ID de la misión que debe completarse")]
    public string misionRequeridaID = "";

    [Header("Estado de la Misión")]
    public bool bloqueada = true;
    public bool descifrada = false;
    public bool completada = false;

    [Header("Identificación")]
    public string misionID;

    [Header("Cuentos a los que pertenece")]
    [Tooltip("Cuentos en los que esta misión aparece. Vacío = todos los cuentos (compat).")]
    public string[] cuentos;

    public bool PerteneceACuento(string cuentoID)
    {
        if (string.IsNullOrEmpty(cuentoID)) return true;
        if (cuentos == null || cuentos.Length == 0) return true;
        return System.Array.IndexOf(cuentos, cuentoID) >= 0;
    }

    [Header("Descifrar Misión - UI")]
    // Las partes ya definidas arriba se usan para el descifrado en UI

    [Header("Completar Misión - AR")]
    public string idObjetoDestino;
    public List<string> idsObjetosCorrectos;

    // ========================================
    // 📖 NUEVO: INTEGRACIÓN CON STORY SYSTEM
    // ========================================
    
    [Header("📖 Fragmentos de Historia")]
    [Tooltip("Fragmento que se reproduce al descifrar esta misión")]
    public StoryFragment fragmentoAlDescifrar;

    [Tooltip("Fragmento que se reproduce al completar esta misión en AR")]
    public StoryFragment fragmentoAlCompletar;

    [Tooltip("Reproducir fragmento automáticamente o esperar confirmación")]
    public bool reproducirFragmentoAutomaticamente = true;

    [Header("🎬 Animación al Completar (AR)")]
    [Tooltip("Nombre del Timeline (PlayableDirector) a reproducir en el objeto destino al completar esta misión. Debe coincidir con el nombre del GameObject que tiene el PlayableDirector, o con el nombre del PlayableAsset asignado. Si está vacío, el fragmento (si existe) se reproduce inmediatamente.")]
    public string nombreTimelineAlCompletar;

    /// <summary>
    /// Verifica si esta misión tiene fragmentos de historia asociados
    /// </summary>
    public bool TieneFragmentos()
    {
        return fragmentoAlDescifrar != null || fragmentoAlCompletar != null;
    }

    /// <summary>
    /// Obtiene información sobre los fragmentos asociados
    /// </summary>
    public string ObtenerInfoFragmentos()
    {
        if (!TieneFragmentos())
            return "Sin fragmentos de historia";

        string info = "";
        
        if (fragmentoAlDescifrar != null)
            info += $"Al descifrar: {fragmentoAlDescifrar.fragmentID}";
        
        if (fragmentoAlCompletar != null)
        {
            if (info.Length > 0) info += " | ";
            info += $"Al completar: {fragmentoAlCompletar.fragmentID}";
        }
        
        return info;
    }
}

[System.Serializable]
public enum TipoActivacion
{
    ActivaDesdeInicio,
    AlGuardarObjeto,
    AlDescifrarMision
}