using UnityEngine;

public class GlobalAudioManager : MonoBehaviour
{
    public static GlobalAudioManager Instance;

    [Header("AudioSources")]
    [SerializeField] private AudioSource audioSourceUI;      // Para efectos UI
    [SerializeField] private AudioSource audioSourceSFX;     // Para efectos de sonido general
    [SerializeField] private AudioSource audioSourceAmbient; // Para sonidos ambientales

    [Header("Sonidos de Drag & Drop")]
    [SerializeField] private AudioClip sonidoAgarrarItem;
    [SerializeField] private AudioClip sonidoSoltarExitoso;
    [SerializeField] private AudioClip sonidoSoltarFallido;

    [Header("🧩 Sonidos de Misiones")]
    [SerializeField] private AudioClip sonidoMisionDescifradaExito;    // 🎉 Misión descifrada correctamente
    [SerializeField] private AudioClip sonidoMisionDescifradaError;    // ❌ Error al descifrar misión
    [SerializeField] private AudioClip sonidoMisionCompletadaAR;       // 🏆 Misión completada en AR
    [SerializeField] private AudioClip sonidoNuevaMisionDisponible;    // 🆕 Nueva misión desbloqueada
    
    [Header("📖 Sonidos de Libro")]
    [SerializeField] private AudioClip sonidoPasarHoja;

    [Header("🎮 Sonidos de UI")]
    [SerializeField] private AudioClip sonidoClickBoton;
    [SerializeField] private AudioClip sonidoTogglePanel;
    [SerializeField] private AudioClip sonidoObjetoGuardado;
    [SerializeField] private AudioClip sonidoSeleccionarMision;        // 🎯 Al seleccionar misión de la lista
    
    [Header("🔊 Sonidos de Feedback")]
    [SerializeField] private AudioClip sonidoExitoGeneral;             // ✅ Éxito general
    [SerializeField] private AudioClip sonidoErrorGeneral;             // ❌ Error general
    [SerializeField] private AudioClip sonidoNotificacion;             // 🔔 Notificación
    [SerializeField] private AudioClip sonidoProgreso;                 // 📊 Progreso/avance

    [Header("Configuración")]
    [SerializeField] private float volumenMaster = 1f;
    [SerializeField] private float volumenUI = 0.8f;
    [SerializeField] private float volumenSFX = 1f;
    [SerializeField] private float volumenMusica = 0.7f;
    [SerializeField] private float volumenMisiones = 0.9f; // 🆕 Volumen específico para misiones

    [Header("Debug")]
    [SerializeField] private bool mostrarDebugLogs = true;

    // 🔊 Estado de silencio (mute) y persistencia del panel de configuración de audio
    private bool musicaSilenciada = false;
    private bool efectosSilenciados = false;
    private float _defMusica, _defEfectos;   // defaults del Inspector (para "Restablecer")
    private const string KEY_MUS      = "audio_vol_musica";
    private const string KEY_EF       = "audio_vol_efectos";
    private const string KEY_MUS_MUTE = "audio_mute_musica";
    private const string KEY_EF_MUTE  = "audio_mute_efectos";

    void Awake()
    {
        // Singleton pattern "last wins": la nueva escena reemplaza al viejo DDOL
        // para evitar referencias colgantes tras una transición de escena.
        if (Instance != null && Instance != this)
        {
            Debug.Log("🎵 [GlobalAudioManager] Reemplazando instancia anterior (last-wins)");
            Destroy(Instance.gameObject);
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        gameObject.tag = "GlobalAudio";

        Debug.Log("🎵 [GlobalAudioManager] Inicializado correctamente");

        CargarPreferenciasAudio();   // 🔊 cargar volúmenes/mute guardados antes de aplicar
        ConfigurarAudioSources();
    }

    private void ConfigurarAudioSources()
    {
        // Crear AudioSources si no existen
        if (audioSourceUI == null)
        {
            GameObject uiAudioObj = new GameObject("UIAudioSource");
            uiAudioObj.transform.SetParent(transform);
            audioSourceUI = uiAudioObj.AddComponent<AudioSource>();
            audioSourceUI.playOnAwake = false;
        }

        if (audioSourceSFX == null)
        {
            GameObject sfxAudioObj = new GameObject("SFXAudioSource");
            sfxAudioObj.transform.SetParent(transform);
            audioSourceSFX = sfxAudioObj.AddComponent<AudioSource>();
            audioSourceSFX.playOnAwake = false;
        }

        if (audioSourceAmbient == null)
        {
            GameObject ambientObj = new GameObject("AmbientAudioSource");
            ambientObj.transform.SetParent(transform);
            audioSourceAmbient = ambientObj.AddComponent<AudioSource>();
            audioSourceAmbient.playOnAwake = false;
            audioSourceAmbient.loop = true;
        }

        ActualizarVolumenes();
        
        if (mostrarDebugLogs)
        {
            Debug.Log($"🎵 [GlobalAudioManager] AudioSources configurados - UI: {audioSourceUI != null}, SFX: {audioSourceSFX != null}");
        }
    }

    private void ActualizarVolumenes()
    {
        float efMul = efectosSilenciados ? 0f : 1f;

        if (audioSourceUI != null)
            audioSourceUI.volume = volumenMaster * volumenUI * efMul;

        if (audioSourceSFX != null)
            audioSourceSFX.volume = volumenMaster * volumenSFX * efMul;

        if (audioSourceAmbient != null)
            audioSourceAmbient.volume = musicaSilenciada ? 0f : volumenMaster * volumenMusica;
    }

    // ==============================================
    // 🧩 MÉTODOS PARA SONIDOS DE MISIONES
    // ==============================================
    
    /// <summary>
    /// Reproducir cuando una misión se descifra correctamente
    /// </summary>
    public void ReproducirSonidoMisionDescifradaExito()
    {
        if (mostrarDebugLogs) Debug.Log("🎉 [GlobalAudioManager] Misión descifrada con éxito!");
        ReproducirSonidoMision(sonidoMisionDescifradaExito);
    }
    
    /// <summary>
    /// Reproducir cuando hay error al descifrar misión (objetos incorrectos/faltantes)
    /// </summary>
    public void ReproducirSonidoMisionDescifradaError()
    {
        if (mostrarDebugLogs) Debug.Log("❌ [GlobalAudioManager] Error al descifrar misión");
        ReproducirSonidoMision(sonidoMisionDescifradaError);
    }
    
    /// <summary>
    /// Reproducir cuando una misión se completa en AR
    /// </summary>
    public void ReproducirSonidoMisionCompletadaAR()
    {
        if (mostrarDebugLogs) Debug.Log("🏆 [GlobalAudioManager] Misión completada en AR!");
        ReproducirSonidoMision(sonidoMisionCompletadaAR);
    }
    
    /// <summary>
    /// Reproducir cuando se desbloquea una nueva misión
    /// </summary>
    public void ReproducirSonidoNuevaMisionDisponible()
    {
        if (mostrarDebugLogs) Debug.Log("🆕 [GlobalAudioManager] Nueva misión disponible");
        ReproducirSonidoMision(sonidoNuevaMisionDisponible);
    }

    // ==============================================
    // 🎮 MÉTODOS PARA SONIDOS DE UI
    // ==============================================
    
    public void ReproducirSonidoSeleccionarMision()
    {
        if (mostrarDebugLogs) Debug.Log("🎯 [GlobalAudioManager] Misión seleccionada");
        ReproducirSonidoUI(sonidoSeleccionarMision);
    }
    
    // ==============================================
    // 🔊 MÉTODOS PARA FEEDBACK GENERAL
    // ==============================================
    
    public void ReproducirSonidoExitoGeneral()
    {
        if (mostrarDebugLogs) Debug.Log("✅ [GlobalAudioManager] Éxito general");
        ReproducirSonidoSFX(sonidoExitoGeneral);
    }
    
    public void ReproducirSonidoErrorGeneral()
    {
        if (mostrarDebugLogs) Debug.Log("❌ [GlobalAudioManager] Error general");
        ReproducirSonidoSFX(sonidoErrorGeneral);
    }
    
    public void ReproducirSonidoNotificacion()
    {
        if (mostrarDebugLogs) Debug.Log("🔔 [GlobalAudioManager] Notificación");
        ReproducirSonidoUI(sonidoNotificacion);
    }
    
    public void ReproducirSonidoProgreso()
    {
        if (mostrarDebugLogs) Debug.Log("📊 [GlobalAudioManager] Progreso");
        ReproducirSonidoSFX(sonidoProgreso);
    }

    // ==============================================
    // 🎵 MÉTODOS EXISTENTES DE DRAG & DROP
    // ==============================================
    
    public void ReproducirSonidoAgarrarItem()
    {
        if (mostrarDebugLogs) Debug.Log("🎵 [GlobalAudioManager] ReproducirSonidoAgarrarItem llamado");
        ReproducirSonidoUI(sonidoAgarrarItem);
    }

    public void ReproducirSonidoSoltarExitoso()
    {
        if (mostrarDebugLogs) Debug.Log("🎵 [GlobalAudioManager] ReproducirSonidoSoltarExitoso llamado");
        ReproducirSonidoUI(sonidoSoltarExitoso);
    }

    public void ReproducirSonidoSoltarFallido()
    {
        if (mostrarDebugLogs) Debug.Log("🎵 [GlobalAudioManager] ReproducirSonidoSoltarFallido llamado");
        ReproducirSonidoUI(sonidoSoltarFallido);
    }

    // ==============================================
    // 🎵 MÉTODOS DE MÚSICA
    // ==============================================

    public void ReproducirMusica(AudioClip clip)
    {
        if (audioSourceAmbient == null || clip == null) return;
        if (audioSourceAmbient.clip == clip && audioSourceAmbient.isPlaying) return;

        audioSourceAmbient.clip = clip;
        audioSourceAmbient.loop = true;
        audioSourceAmbient.volume = musicaSilenciada ? 0f : volumenMaster * volumenMusica;
        audioSourceAmbient.Play();

        if (mostrarDebugLogs) Debug.Log($"🎵 [GlobalAudioManager] Música: {clip.name}");
    }

    public void DetenerMusica()
    {
        if (audioSourceAmbient != null)
            audioSourceAmbient.Stop();
    }

    public void CambiarVolumenMusica(float nuevoVolumen)
    {
        volumenMusica = Mathf.Clamp01(nuevoVolumen);
        ActualizarVolumenes();
        GuardarPreferenciasAudio();
    }

    public void ReproducirSonidoPasarHoja()
    {
        ReproducirSonidoSFX(sonidoPasarHoja);
    }

    public void ReproducirSonidoClickBoton()
    {
        ReproducirSonidoUI(sonidoClickBoton);
    }

    public void ReproducirSonidoObjetoGuardado()
    {
        ReproducirSonidoSFX(sonidoObjetoGuardado);
    }

    public void ReproducirSonidoTogglePanel()
    {
        ReproducirSonidoUI(sonidoTogglePanel);
    }

    // ==============================================
    // 🔧 MÉTODOS BASE DE REPRODUCCIÓN
    // ==============================================
    
    /// <summary>
    /// Método específico para sonidos de misiones con volumen ajustado
    /// </summary>
    private void ReproducirSonidoMision(AudioClip clip)
    {
        if (clip != null && audioSourceSFX != null)
        {
            float volumenMision = volumenMaster * volumenMisiones;
            audioSourceSFX.PlayOneShot(clip, volumenMision);
            if (mostrarDebugLogs) Debug.Log($"🧩 [GlobalAudioManager] ✅ Sonido misión reproducido: {clip.name}");
        }
        else
        {
            if (mostrarDebugLogs) Debug.LogWarning($"🧩 [GlobalAudioManager] ❌ No se pudo reproducir sonido misión - Clip: {clip?.name ?? "null"}");
        }
    }

    public void ReproducirSonidoUI(AudioClip clip, float volumen = 1f)
    {
        if (clip != null && audioSourceUI != null)
        {
            audioSourceUI.PlayOneShot(clip, volumen);
            if (mostrarDebugLogs) Debug.Log($"🎮 [GlobalAudioManager] ✅ Sonido UI reproducido: {clip.name}");
        }
        else
        {
            if (mostrarDebugLogs) Debug.LogWarning($"🎮 [GlobalAudioManager] ❌ No se pudo reproducir UI - Clip: {clip?.name ?? "null"}");
        }
    }

    public void ReproducirSonidoSFX(AudioClip clip, float volumen = 1f)
    {
        if (clip != null && audioSourceSFX != null)
        {
            audioSourceSFX.PlayOneShot(clip, volumen);
            if (mostrarDebugLogs) Debug.Log($"🔊 [GlobalAudioManager] ✅ Sonido SFX reproducido: {clip.name}");
        }
        else
        {
            if (mostrarDebugLogs) Debug.LogWarning($"🔊 [GlobalAudioManager] ❌ No se pudo reproducir SFX - Clip: {clip?.name ?? "null"}");
        }
    }

    // ==============================================
    // ⚙️ CONFIGURACIÓN DE VOLUMEN
    // ==============================================
    
    public void CambiarVolumenMaster(float nuevoVolumen)
    {
        volumenMaster = Mathf.Clamp01(nuevoVolumen);
        ActualizarVolumenes();
    }

    public void CambiarVolumenUI(float nuevoVolumen)
    {
        volumenUI = Mathf.Clamp01(nuevoVolumen);
        ActualizarVolumenes();
    }

    public void CambiarVolumenSFX(float nuevoVolumen)
    {
        volumenSFX = Mathf.Clamp01(nuevoVolumen);
        ActualizarVolumenes();
    }
    
    public void CambiarVolumenMisiones(float nuevoVolumen)
    {
        volumenMisiones = Mathf.Clamp01(nuevoVolumen);
    }

    // ==============================================
    // 🔊 PANEL DE CONFIGURACIÓN DE AUDIO (música / efectos)
    // ==============================================

    /// <summary>
    /// Un solo control de "efectos": mueve UI + SFX + misiones a la vez.
    /// </summary>
    public void CambiarVolumenEfectos(float nuevoVolumen)
    {
        float c = Mathf.Clamp01(nuevoVolumen);
        volumenUI = c;
        volumenSFX = c;
        volumenMisiones = c;
        ActualizarVolumenes();
        GuardarPreferenciasAudio();
    }

    public void SilenciarMusica(bool silenciar)
    {
        musicaSilenciada = silenciar;
        ActualizarVolumenes();
        GuardarPreferenciasAudio(true);
    }

    public void SilenciarEfectos(bool silenciar)
    {
        efectosSilenciados = silenciar;
        ActualizarVolumenes();
        GuardarPreferenciasAudio(true);
    }

    public void ToggleMuteMusica()  => SilenciarMusica(!musicaSilenciada);
    public void ToggleMuteEfectos() => SilenciarEfectos(!efectosSilenciados);

    public void RestablecerVolumenes()
    {
        volumenMusica = _defMusica;
        volumenUI = _defEfectos;
        volumenSFX = _defEfectos;
        volumenMisiones = _defEfectos;
        musicaSilenciada = false;
        efectosSilenciados = false;
        ActualizarVolumenes();
        GuardarPreferenciasAudio(true);
    }

    // Getters para inicializar la UI del panel de audio
    public float VolumenMusica      => volumenMusica;
    public float VolumenEfectos     => volumenSFX;
    public bool  MusicaSilenciada   => musicaSilenciada;
    public bool  EfectosSilenciados => efectosSilenciados;

    private void CargarPreferenciasAudio()
    {
        // Capturar los valores del Inspector como "por defecto" ANTES de sobreescribir
        _defMusica  = volumenMusica;
        _defEfectos = volumenSFX;

        volumenMusica = PlayerPrefs.GetFloat(KEY_MUS, volumenMusica);
        float ef = PlayerPrefs.GetFloat(KEY_EF, volumenSFX);
        volumenUI = ef;
        volumenSFX = ef;
        volumenMisiones = ef;

        musicaSilenciada   = PlayerPrefs.GetInt(KEY_MUS_MUTE, 0) == 1;
        efectosSilenciados = PlayerPrefs.GetInt(KEY_EF_MUTE, 0) == 1;
    }

    /// <summary>
    /// Guarda en PlayerPrefs. Por defecto solo en memoria (barato); con flush=true escribe a disco.
    /// Los sliders llaman sin flush (evita escribir en cada frame de arrastre); mute/reset con flush.
    /// </summary>
    private void GuardarPreferenciasAudio(bool flush = false)
    {
        PlayerPrefs.SetFloat(KEY_MUS, volumenMusica);
        PlayerPrefs.SetFloat(KEY_EF, volumenSFX);
        PlayerPrefs.SetInt(KEY_MUS_MUTE, musicaSilenciada ? 1 : 0);
        PlayerPrefs.SetInt(KEY_EF_MUTE, efectosSilenciados ? 1 : 0);
        if (flush) PlayerPrefs.Save();
    }

    void OnApplicationPause(bool pausado)
    {
        if (pausado) PlayerPrefs.Save();
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    // ==============================================
    // 🧪 MÉTODOS DE TESTING
    // ==============================================
    
    [ContextMenu("🧩 Test Misión Descifrada Éxito")]
    public void TestMisionDescifradaExito()
    {
        Debug.Log("🧩 [Testing] Misión descifrada con éxito");
        ReproducirSonidoMisionDescifradaExito();
    }

    [ContextMenu("🧩 Test Misión Descifrada Error")]
    public void TestMisionDescifradaError()
    {
        Debug.Log("🧩 [Testing] Error al descifrar misión");
        ReproducirSonidoMisionDescifradaError();
    }

    [ContextMenu("🏆 Test Misión Completada AR")]
    public void TestMisionCompletadaAR()
    {
        Debug.Log("🏆 [Testing] Misión completada en AR");
        ReproducirSonidoMisionCompletadaAR();
    }

    [ContextMenu("🆕 Test Nueva Misión Disponible")]
    public void TestNuevaMisionDisponible()
    {
        Debug.Log("🆕 [Testing] Nueva misión disponible");
        ReproducirSonidoNuevaMisionDisponible();
    }

    [ContextMenu("🎯 Test Seleccionar Misión")]
    public void TestSeleccionarMision()
    {
        Debug.Log("🎯 [Testing] Seleccionar misión");
        ReproducirSonidoSeleccionarMision();
    }

    [ContextMenu("✅ Test Éxito General")]
    public void TestExitoGeneral()
    {
        Debug.Log("✅ [Testing] Éxito general");
        ReproducirSonidoExitoGeneral();
    }

    [ContextMenu("❌ Test Error General")]
    public void TestErrorGeneral()
    {
        Debug.Log("❌ [Testing] Error general");
        ReproducirSonidoErrorGeneral();
    }

    [ContextMenu("🔔 Test Notificación")]
    public void TestNotificacion()
    {
        Debug.Log("🔔 [Testing] Notificación");
        ReproducirSonidoNotificacion();
    }

    [ContextMenu("📊 Test Progreso")]
    public void TestProgreso()
    {
        Debug.Log("📊 [Testing] Progreso");
        ReproducirSonidoProgreso();
    }

    [ContextMenu("🔍 Debug Estado Completo")]
    public void DebugEstadoCompleto()
    {
        Debug.Log("=== 🎵 ESTADO COMPLETO GLOBALAUDIOMANAGER ===");
        Debug.Log($"Instance: {Instance != null}");
        Debug.Log($"AudioSource UI: {audioSourceUI != null} - Vol: {audioSourceUI?.volume}");
        Debug.Log($"AudioSource SFX: {audioSourceSFX != null} - Vol: {audioSourceSFX?.volume}");
        
        Debug.Log("--- DRAG & DROP ---");
        Debug.Log($"Agarrar: {sonidoAgarrarItem?.name ?? "null"}");
        Debug.Log($"Soltar Éxito: {sonidoSoltarExitoso?.name ?? "null"}");
        Debug.Log($"Soltar Fallo: {sonidoSoltarFallido?.name ?? "null"}");
        
        Debug.Log("--- MISIONES ---");
        Debug.Log($"Misión Éxito: {sonidoMisionDescifradaExito?.name ?? "null"}");
        Debug.Log($"Misión Error: {sonidoMisionDescifradaError?.name ?? "null"}");
        Debug.Log($"Misión AR: {sonidoMisionCompletadaAR?.name ?? "null"}");
        Debug.Log($"Nueva Misión: {sonidoNuevaMisionDisponible?.name ?? "null"}");
        
        Debug.Log("--- UI ---");
        Debug.Log($"Click Botón: {sonidoClickBoton?.name ?? "null"}");
        Debug.Log($"Seleccionar Misión: {sonidoSeleccionarMision?.name ?? "null"}");
        Debug.Log($"Toggle Panel: {sonidoTogglePanel?.name ?? "null"}");
        Debug.Log($"Objeto Guardado: {sonidoObjetoGuardado?.name ?? "null"}");
        
        Debug.Log("--- FEEDBACK ---");
        Debug.Log($"Éxito General: {sonidoExitoGeneral?.name ?? "null"}");
        Debug.Log($"Error General: {sonidoErrorGeneral?.name ?? "null"}");
        Debug.Log($"Notificación: {sonidoNotificacion?.name ?? "null"}");
        Debug.Log($"Progreso: {sonidoProgreso?.name ?? "null"}");
        
        Debug.Log("--- VOLÚMENES ---");
        Debug.Log($"Master: {volumenMaster}, UI: {volumenUI}, SFX: {volumenSFX}, Misiones: {volumenMisiones}");
    }

    // Configuración dinámica de sonidos
    public void ConfigurarSonidosDragDrop(AudioClip agarrar, AudioClip soltarExito, AudioClip soltarFallo)
    {
        sonidoAgarrarItem = agarrar;
        sonidoSoltarExitoso = soltarExito;
        sonidoSoltarFallido = soltarFallo;
    }

    public void ConfigurarSonidosMisiones(AudioClip exito, AudioClip error, AudioClip completadaAR, AudioClip nuevaDisponible)
    {
        sonidoMisionDescifradaExito = exito;
        sonidoMisionDescifradaError = error;
        sonidoMisionCompletadaAR = completadaAR;
        sonidoNuevaMisionDisponible = nuevaDisponible;
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            ActualizarVolumenes();
        }
    }
}