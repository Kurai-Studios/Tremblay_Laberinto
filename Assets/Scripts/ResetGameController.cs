using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResetGameController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Button botonReset;
    [SerializeField] private GameObjectManager gameObjectManager;
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private ScrollViewLoader scrollViewLoader;
    [SerializeField] private ObjectInfoUIManager objectInfoUIManager;
    [SerializeField] private StoryManager storyManager;
    
    [Header("Confirmación")]
    [SerializeField] private GameObject panelConfirmacion;
    [SerializeField] private Button botonConfirmar;
    [SerializeField] private Button botonCancelar;
    [SerializeField] private TextMeshProUGUI textoConfirmacion;
    
    [Header("Configuración")]
    [SerializeField] private bool requierConfirmacion = true;
    [SerializeField] private string mensajeConfirmacion = "Are you sure you want to reset the game? All progress will be lost.";

    // 🆕 NUEVOS ELEMENTOS PARA PANEL DE CONFIGURACIÓN
    [Header("🎛️ Panel de Configuración")]
    [SerializeField] private Button botonResetCompleto;
    [SerializeField] private Button botonResetObjetos;
    [SerializeField] private Button botonResetMisiones;
    [SerializeField] private Button botonMostrarEstadisticas;
    
    [Header("📊 Información")]
    [SerializeField] private TextMeshProUGUI textoEstadisticas;
    [SerializeField] private TextMeshProUGUI textoUltimaAccion;
    
    [Header("🔊 Audio")]
    [SerializeField] private bool usarSonidos = true;

    // Variables para confirmación
    private System.Action accionPendiente;

    void Start()
    {
        BuscarReferenciasAutomaticamente();
        ConfigurarBotones();
        ActualizarEstadisticas();
        
        if (panelConfirmacion != null)
        {
            panelConfirmacion.SetActive(false);
        }
    }

    private void BuscarReferenciasAutomaticamente()
    {
        // Buscar automáticamente si no están asignadas
        if (gameObjectManager == null)
            gameObjectManager = GameObjectManager.Instance;

        if (missionManager == null)
            missionManager = MissionManager.Instance;
            
        if (scrollViewLoader == null)
            scrollViewLoader = FindObjectOfType<ScrollViewLoader>();
            
        if (objectInfoUIManager == null)
            objectInfoUIManager = FindObjectOfType<ObjectInfoUIManager>();

        if (storyManager == null)
            storyManager = StoryManager.Instance != null ? StoryManager.Instance : FindObjectOfType<StoryManager>();
    }

    private void ConfigurarBotones()
    {
        // 🔧 BOTÓN ORIGINAL (mantener compatibilidad)
        if (botonReset != null)
        {
            botonReset.onClick.RemoveAllListeners();
            botonReset.onClick.AddListener(IniciarReset);
        }

        // 🆕 NUEVOS BOTONES ESPECÍFICOS
        if (botonResetCompleto != null)
        {
            botonResetCompleto.onClick.RemoveAllListeners();
            botonResetCompleto.onClick.AddListener(() => SolicitarConfirmacion(
                "🔄 Reset ALL game progress?\n\nThis will delete:\n• All saved objects\n• Mission progress\n• Settings",
                () => EjecutarReset("Full", () => EjecutarResetCompleto())
            ));
        }

        if (botonResetObjetos != null)
        {
            botonResetObjetos.onClick.RemoveAllListeners();
            botonResetObjetos.onClick.AddListener(() => SolicitarConfirmacion(
                "📦 Reset only saved objects?\n\nThis will delete:\n• Objects marked as saved\n• Collection progress",
                () => EjecutarReset("Objects", () => ResetSoloObjetos())
            ));
        }

        if (botonResetMisiones != null)
        {
            botonResetMisiones.onClick.RemoveAllListeners();
            botonResetMisiones.onClick.AddListener(() => SolicitarConfirmacion(
                "🧩 Reset only mission progress?\n\nThis will delete:\n• Deciphered missions\n• Completed missions",
                () => EjecutarReset("Missions", () => ResetSoloMisiones())
            ));
        }

        if (botonMostrarEstadisticas != null)
        {
            botonMostrarEstadisticas.onClick.RemoveAllListeners();
            botonMostrarEstadisticas.onClick.AddListener(ActualizarEstadisticas);
        }

        // Botones de confirmación
        if (botonConfirmar != null)
        {
            botonConfirmar.onClick.RemoveAllListeners();
            botonConfirmar.onClick.AddListener(ConfirmarReset);
        }
        
        if (botonCancelar != null)
        {
            botonCancelar.onClick.RemoveAllListeners();
            botonCancelar.onClick.AddListener(CancelarReset);
        }
        
        if (textoConfirmacion != null)
        {
            textoConfirmacion.text = mensajeConfirmacion;
        }
    }

    // 🔧 MÉTODO ORIGINAL (mantener compatibilidad)
    public void IniciarReset()
    {
        Debug.Log("[ResetGame] Iniciando proceso de reset");
        
        if (requierConfirmacion && panelConfirmacion != null)
        {
            SolicitarConfirmacion(mensajeConfirmacion, () => EjecutarResetCompleto());
        }
        else
        {
            EjecutarResetCompleto();
        }
    }

    // 🆕 NUEVO SISTEMA DE CONFIRMACIÓN
    private void SolicitarConfirmacion(string mensaje, System.Action accion)
    {
        if (panelConfirmacion != null)
        {
            accionPendiente = accion;
            
            if (textoConfirmacion != null)
            {
                textoConfirmacion.text = mensaje;
            }
            
            panelConfirmacion.SetActive(true);
            
            // 🎵 Sonido de notificación
            if (usarSonidos && GlobalAudioManager.Instance != null)
            {
                GlobalAudioManager.Instance.ReproducirSonidoNotificacion();
            }
            
            Debug.Log("[ResetGame] Panel de confirmación mostrado");
        }
        else
        {
            // Ejecutar directamente si no hay panel de confirmación
            accion?.Invoke();
        }
    }

    private void ConfirmarReset()
    {
        if (panelConfirmacion != null)
        {
            panelConfirmacion.SetActive(false);
        }
        
        accionPendiente?.Invoke();
        accionPendiente = null;
    }

    private void CancelarReset()
    {
        if (panelConfirmacion != null)
        {
            panelConfirmacion.SetActive(false);
        }
        
        accionPendiente = null;
        MostrarUltimaAccion("❌ Action cancelled");
        
        Debug.Log("[ResetGame] Reset cancelado por el usuario");
        
        // 🎵 Sonido de cancelación
        if (usarSonidos && GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoClickBoton();
        }
    }

    // 🆕 NUEVO WRAPPER PARA EJECUCIÓN CON FEEDBACK
    private void EjecutarReset(string tipo, System.Action resetAction)
    {
        try
        {
            Debug.Log($"[ResetGame] ••• EJECUTANDO RESET {tipo.ToUpper()} •••");
            
            resetAction?.Invoke();
            
            MostrarUltimaAccion($"✅ {tipo} reset completed successfully");
            ActualizarEstadisticas();
            
            // 🎵 Sonido de éxito
            if (usarSonidos && GlobalAudioManager.Instance != null)
            {
                GlobalAudioManager.Instance.ReproducirSonidoExitoGeneral();
            }
            
            Debug.Log($"[ResetGame] ✅ RESET {tipo.ToUpper()} EXITOSO");
        }
        catch (System.Exception e)
        {
            string errorMsg = $"❌ {tipo} reset error: {e.Message}";
            MostrarUltimaAccion(errorMsg);
            Debug.LogError($"[ResetGame] {errorMsg}");
            
            // 🎵 Sonido de error
            if (usarSonidos && GlobalAudioManager.Instance != null)
            {
                GlobalAudioManager.Instance.ReproducirSonidoErrorGeneral();
            }
        }
    }

    // 🔧 MÉTODO ORIGINAL MEJORADO
    public void EjecutarResetCompleto()
    {
        Debug.Log("[ResetGame] ••••• INICIANDO RESET COMPLETO •••••");
        
        try
        {
            // 1. Reset de objetos guardados
            ResetObjetosGuardados();
            
            // 2. Reset de misiones
            ResetMisiones();

            // 3. Limpiar objetos colocados en UI
            LimpiarObjetosColocados();

            // 4. Reset del progreso de historia (fragmentos vistos)
            ResetHistoria();

            // 5. Actualizar interfaces
            ActualizarInterfaces();
            
            Debug.Log("[ResetGame] ✅ RESET COMPLETO EXITOSO");
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ResetGame] ❌ Error durante el reset: {e.Message}");
        }
    }

    private void ResetObjetosGuardados()
    {
        if (gameObjectManager == null)
        {
            Debug.LogWarning("[ResetGame] GameObjectManager no encontrado");
            return;
        }

        Debug.Log("[ResetGame] Reseteando objetos guardados...");
        
        int objetosReseteados = 0;
        foreach (var objeto in gameObjectManager.listaObjetos)
        {
            if (objeto != null && objeto.guardadoPorJugador)
            {
                objeto.guardadoPorJugador = false;
                objetosReseteados++;
                Debug.Log($"[ResetGame] - {objeto.nombreEspanol} marcado como NO guardado");
            }
        }
        
        // Guardar cambios
        gameObjectManager.GuardarDatos();
        Debug.Log($"[ResetGame] ✅ {objetosReseteados} objetos guardados reseteados");
    }

    private void ResetMisiones()
    {
        if (gameObjectManager == null)
        {
            Debug.LogWarning("[ResetGame] GameObjectManager no encontrado para reset de misiones");
            return;
        }

        Debug.Log("[ResetGame] Reseteando misiones...");
        
        // Crear datos de misión limpios
        MissionSaveData datosMisionesLimpios = new MissionSaveData
        {
            descifradas = new System.Collections.Generic.List<string>(),
            completadas = new System.Collections.Generic.List<string>()
        };
        
        // Guardar datos limpios
        gameObjectManager.GuardarMisiones(datosMisionesLimpios);

        // Reset de misiones en MissionManager si existe.
        // ⚠️ IMPORTANTE: ReiniciarProgresoMisiones limpia el estado EN MEMORIA (HashSets + bools),
        // no solo el JSON. Con ReevaluarMisiones el estado en memoria seguía "completo" tras el
        // reset y el panel de fin de juego se disparaba en bucle.
        if (missionManager != null)
        {
            missionManager.ReiniciarProgresoMisiones();
            Debug.Log("[ResetGame] MissionManager: progreso reiniciado en memoria");
        }
        
        Debug.Log("[ResetGame] ✅ Misiones reseteadas");
    }

    private void ResetHistoria()
    {
        if (storyManager == null)
            storyManager = StoryManager.Instance;

        if (storyManager != null)
        {
            storyManager.ResetearProgreso();

            // Volver a lanzar el prólogo/intro (la escena no se recarga, así que su Start no corre).
            // Como ResetearProgreso limpió los fragmentos vistos, el prólogo se reproduce de nuevo.
            storyManager.ReproducirPrologo();

            Debug.Log("[ResetGame] ✅ Historia reseteada y prólogo relanzado");
        }
        else
        {
            Debug.LogWarning("[ResetGame] StoryManager no encontrado; no se reseteó la historia");
        }
    }

    private void LimpiarObjetosColocados()
    {
        Debug.Log("[ResetGame] Limpiando objetos colocados...");
        
        // Limpiar objetos colocados en ObjectInfoUIManager
        if (objectInfoUIManager != null)
        {
            objectInfoUIManager.LimpiarObjetosColocados();
            Debug.Log("[ResetGame] ObjectInfoUIManager limpiado");
        }
        
        Debug.Log("[ResetGame] ✅ Objetos colocados limpiados");
    }

    private void ActualizarInterfaces()
    {
        Debug.Log("[ResetGame] Actualizando interfaces...");
        
        // Actualizar ScrollView
        if (scrollViewLoader != null)
        {
            scrollViewLoader.RefrescarSprites();
            Debug.Log("[ResetGame] ScrollViewLoader refrescado");
        }
        
        // Cerrar canvas de ObjectInfo si está abierto
        if (objectInfoUIManager != null)
        {
            objectInfoUIManager.CerrarCanvas();
            Debug.Log("[ResetGame] Canvas de ObjectInfo cerrado");
        }
        
        // Actualizar MissionManager
        if (missionManager != null)
        {
            missionManager.ReevaluarMisiones();
            Debug.Log("[ResetGame] MissionManager actualizado");
        }
        
        Debug.Log("[ResetGame] ✅ Interfaces actualizadas");
    }

    // 🆕 NUEVOS MÉTODOS ESPECÍFICOS
    public void ResetSoloObjetos()
    {
        Debug.Log("[ResetGame] Reset solo objetos");
        ResetObjetosGuardados();
        
        if (scrollViewLoader != null)
        {
            scrollViewLoader.RefrescarSprites();
        }
    }

    public void ResetSoloMisiones()
    {
        Debug.Log("[ResetGame] Reset solo misiones");
        ResetMisiones();
        
        if (missionManager != null)
        {
            missionManager.ReevaluarMisiones();
        }
    }

    // 🆕 NUEVO SISTEMA DE ESTADÍSTICAS
    private void ActualizarEstadisticas()
    {
        if (textoEstadisticas == null) 
        {
            // Si no hay campo de estadísticas, usar el método original
            if (gameObjectManager != null)
            {
                gameObjectManager.MostrarEstadisticas();
            }
            return;
        }

        if (gameObjectManager != null)
        {
            int totalObjetos = gameObjectManager.listaObjetos?.Count ?? 0;
            int objetosGuardados = gameObjectManager.ObtenerObjetosGuardados()?.Count ?? 0;
            float progreso = totalObjetos > 0 ? (objetosGuardados * 100f / totalObjetos) : 0f;
            
            string estadisticas = $"📦 OBJECTS\n";
            estadisticas += $"Total: {totalObjetos}\n";
            estadisticas += $"Saved: {objetosGuardados}\n";
            estadisticas += $"Progress: {progreso:F1}%\n\n";

            if (missionManager != null)
            {
                estadisticas += $"🧩 MISSIONS\n";
                estadisticas += $"Deciphered: {missionManager.MisionesDescifradas?.Count ?? 0}\n";
                estadisticas += $"Completed: {missionManager.MisionesCompletadas?.Count ?? 0}\n";
                estadisticas += $"Available: {missionManager.MisionesDisponibles?.Count ?? 0}\n\n";

                if (missionManager.MisionesCompletadas?.Count > 0)
                {
                    estadisticas += $"🏆 {missionManager.MisionesCompletadas.Count} missions completed!";
                }
            }

            textoEstadisticas.text = estadisticas;
        }
        else
        {
            textoEstadisticas.text = "❌ Could not load statistics\n\nMake sure GameObjectManager is in the scene.";
        }

        MostrarUltimaAccion("📊 Statistics updated");
        
        // 🎵 Sonido de progreso
        if (usarSonidos && GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.ReproducirSonidoProgreso();
        }
    }

    private void MostrarUltimaAccion(string mensaje)
    {
        if (textoUltimaAccion != null)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            textoUltimaAccion.text = $"[{timestamp}] {mensaje}";
        }
        
        Debug.Log($"[ResetGame] {mensaje}");
    }

    // Métodos de configuración
    public void ActivarConfirmacion(bool activar)
    {
        requierConfirmacion = activar;
    }

    public void CambiarMensajeConfirmacion(string nuevoMensaje)
    {
        mensajeConfirmacion = nuevoMensaje;
        if (textoConfirmacion != null)
        {
            textoConfirmacion.text = nuevoMensaje;
        }
    }

    // 🆕 MÉTODOS PÚBLICOS PARA EL PANEL
    public void RefrescarPanel()
    {
        ActualizarEstadisticas();
    }

    public string ObtenerEstadisticasTexto()
    {
        if (gameObjectManager == null) return "No data available";

        int totalObjetos = gameObjectManager.listaObjetos?.Count ?? 0;
        int objetosGuardados = gameObjectManager.ObtenerObjetosGuardados()?.Count ?? 0;
        int misionesDescifradas = missionManager?.MisionesDescifradas?.Count ?? 0;
        int misionesCompletadas = missionManager?.MisionesCompletadas?.Count ?? 0;

        return $"Objects: {objetosGuardados}/{totalObjetos} | Missions: {misionesCompletadas}/{misionesDescifradas}";
    }

    // Context menu para testing
    [ContextMenu("🔄 Reset Completo (Testing)")]
    public void TestResetCompleto()
    {
        EjecutarResetCompleto();
    }

    [ContextMenu("📦 Reset Solo Objetos")]
    public void TestResetObjetos()
    {
        ResetSoloObjetos();
    }

    [ContextMenu("🧩 Reset Solo Misiones")]
    public void TestResetMisiones()
    {
        ResetSoloMisiones();
    }

    [ContextMenu("📊 Mostrar Estadísticas Actuales")]
    public void TestMostrarEstadisticas()
    {
        ActualizarEstadisticas();
    }

    [ContextMenu("🔍 Debug Estado Sistema")]
    public void DebugEstadoSistema()
    {
        Debug.Log("=== 🔍 ESTADO DEL SISTEMA DE RESET ===");
        Debug.Log($"GameObjectManager: {gameObjectManager != null}");
        Debug.Log($"MissionManager: {missionManager != null}");
        Debug.Log($"ScrollViewLoader: {scrollViewLoader != null}");
        Debug.Log($"ObjectInfoUIManager: {objectInfoUIManager != null}");
        Debug.Log($"Panel Confirmación: {panelConfirmacion != null}");
        Debug.Log($"Requiere Confirmación: {requierConfirmacion}");
        Debug.Log($"Usar Sonidos: {usarSonidos}");
        
        if (gameObjectManager != null)
        {
            Debug.Log($"Total Objetos: {gameObjectManager.listaObjetos?.Count ?? 0}");
            Debug.Log($"Objetos Guardados: {gameObjectManager.ObtenerObjetosGuardados()?.Count ?? 0}");
        }
    }
}