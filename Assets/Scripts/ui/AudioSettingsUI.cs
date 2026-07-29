using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 🔊 UI del panel de configuración de audio. Enlaza dos sliders (música y efectos) con
/// GlobalAudioManager, muestra el porcentaje, permite silenciar (mute) y restablecer, y
/// reproduce un sonido de prueba al ajustar los efectos.
///
/// Va en el GameObject del panel de audio (una entrada más de SettingsPanelManager.panelesSettings).
/// Los volúmenes se guardan en PlayerPrefs desde GlobalAudioManager, así que persisten entre sesiones.
/// </summary>
public class AudioSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider sliderMusica;
    [SerializeField] private Slider sliderEfectos;

    [Header("Porcentaje (opcional)")]
    [SerializeField] private TextMeshProUGUI textoPorcentajeMusica;
    [SerializeField] private TextMeshProUGUI textoPorcentajeEfectos;

    [Header("Mute (opcional)")]
    [SerializeField] private Button botonMuteMusica;
    [SerializeField] private Button botonMuteEfectos;
    [SerializeField] private TextMeshProUGUI iconoMuteMusica;    // muestra 🔊 / 🔇
    [SerializeField] private TextMeshProUGUI iconoMuteEfectos;

    [Header("Restablecer (opcional)")]
    [SerializeField] private Button botonRestablecer;

    [Header("Sonido de prueba")]
    [SerializeField] private bool sonidoPruebaAlAjustar = true;
    [SerializeField] private float cooldownSonidoPrueba = 0.12f;

    private float _ultimoBip = -1f;

    void Awake()
    {
        if (sliderMusica != null)     sliderMusica.onValueChanged.AddListener(OnMusica);
        if (sliderEfectos != null)    sliderEfectos.onValueChanged.AddListener(OnEfectos);
        if (botonMuteMusica != null)  botonMuteMusica.onClick.AddListener(OnMuteMusica);
        if (botonMuteEfectos != null) botonMuteEfectos.onClick.AddListener(OnMuteEfectos);
        if (botonRestablecer != null) botonRestablecer.onClick.AddListener(OnRestablecer);
    }

    // Cada vez que se muestra el panel, sincroniza los sliders con el estado real del audio.
    void OnEnable() => RefrescarUI();

    // Al cerrar el panel, forzar el guardado a disco.
    void OnDisable() => PlayerPrefs.Save();

    private void RefrescarUI()
    {
        var am = GlobalAudioManager.Instance;
        if (am == null) return;

        if (sliderMusica != null)  sliderMusica.SetValueWithoutNotify(am.VolumenMusica);
        if (sliderEfectos != null) sliderEfectos.SetValueWithoutNotify(am.VolumenEfectos);

        ActualizarPorcentajes();
        ActualizarIconosMute();
    }

    private void OnMusica(float v)
    {
        GlobalAudioManager.Instance?.CambiarVolumenMusica(v);
        ActualizarPorcentajes();
    }

    private void OnEfectos(float v)
    {
        var am = GlobalAudioManager.Instance;
        am?.CambiarVolumenEfectos(v);
        ActualizarPorcentajes();

        // Sonido de prueba al ajustar (con cooldown), al volumen de efectos actual.
        if (sonidoPruebaAlAjustar && am != null &&
            Time.unscaledTime - _ultimoBip >= cooldownSonidoPrueba)
        {
            _ultimoBip = Time.unscaledTime;
            am.ReproducirSonidoClickBoton();
        }
    }

    private void OnMuteMusica()  { GlobalAudioManager.Instance?.ToggleMuteMusica();  ActualizarIconosMute(); }
    private void OnMuteEfectos() { GlobalAudioManager.Instance?.ToggleMuteEfectos(); ActualizarIconosMute(); }
    private void OnRestablecer() { GlobalAudioManager.Instance?.RestablecerVolumenes(); RefrescarUI(); }

    private void ActualizarPorcentajes()
    {
        var am = GlobalAudioManager.Instance;
        if (am == null) return;

        if (textoPorcentajeMusica != null)
            textoPorcentajeMusica.text = Mathf.RoundToInt(am.VolumenMusica * 100f) + "%";
        if (textoPorcentajeEfectos != null)
            textoPorcentajeEfectos.text = Mathf.RoundToInt(am.VolumenEfectos * 100f) + "%";
    }

    private void ActualizarIconosMute()
    {
        var am = GlobalAudioManager.Instance;
        if (am == null) return;

        if (iconoMuteMusica != null)  iconoMuteMusica.text  = am.MusicaSilenciada   ? "🔇" : "🔊";
        if (iconoMuteEfectos != null) iconoMuteEfectos.text = am.EfectosSilenciados ? "🔇" : "🔊";
    }

    void OnDestroy()
    {
        if (sliderMusica != null)     sliderMusica.onValueChanged.RemoveListener(OnMusica);
        if (sliderEfectos != null)    sliderEfectos.onValueChanged.RemoveListener(OnEfectos);
        if (botonMuteMusica != null)  botonMuteMusica.onClick.RemoveListener(OnMuteMusica);
        if (botonMuteEfectos != null) botonMuteEfectos.onClick.RemoveListener(OnMuteEfectos);
        if (botonRestablecer != null) botonRestablecer.onClick.RemoveListener(OnRestablecer);
    }
}
