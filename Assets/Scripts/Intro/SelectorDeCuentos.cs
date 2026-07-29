using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;

public class SelectorDeCuentos : MonoBehaviour
{
    [System.Serializable]
    public class Cuento
    {
        public string nombre;
        public Texture2D textura;
        public string nombreEscena;
        public AudioClip musicaCuento;
    }

    [Header("📖 Cuentos")]
    [SerializeField] private Cuento[] cuentos;

    [Header("📄 Hoja")]
    [SerializeField] private GameObject prefabHoja;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Vector3 offsetHojaDebajo = new Vector3(0, -0.01f, 0);
    [SerializeField] private string estadoAdelante = "PasarHoja";
    [SerializeField] private string estadoAtras = "RegresarHoja";
    [SerializeField] private int indiceMaterial = 0;

    [Header("🔘 Selección")]
    [SerializeField] private Button botonSeleccionar;

    [Header("🎓 Tutorial")]
    [SerializeField] private string nombreEscenaTutorial = "halloween-tuto1";
    [SerializeField] private string archivoTutorial = "tutorial_completado.json";

    [Header("⚡ Velocidad")]
    [Tooltip("Multiplica la velocidad de la animación. 2 = doble de rápido, 0.5 = mitad.")]
    [SerializeField] private float velocidadAnimacion = 1f;

    private int paginaActual = 0;
    private bool animando = false;
    private GameObject hojaVisible;
    private GameObject hojaDebajo;
    private float duracionClip;

    void Start()
    {
        if (cuentos == null || cuentos.Length == 0)
        {
            Debug.LogError("[SelectorDeCuentos] ❌ No hay cuentos configurados.");
            return;
        }

        if (botonSeleccionar != null)
            botonSeleccionar.onClick.AddListener(SeleccionarCuento);

        hojaVisible = CrearHoja(cuentos[0], Vector3.zero);
        CachearDuracionClip(hojaVisible);

        if (cuentos.Length > 1)
            hojaDebajo = CrearHoja(cuentos[1], offsetHojaDebajo);

        Debug.Log($"[SelectorDeCuentos] ✅ Iniciado con {cuentos.Length} cuentos");
    }

    private GameObject CrearHoja(Cuento cuento, Vector3 localPos)
    {
        GameObject hoja = Instantiate(prefabHoja, spawnPoint);
        hoja.transform.localPosition = localPos;
        hoja.transform.localRotation = Quaternion.identity;

        Renderer rend = hoja.GetComponentInChildren<Renderer>();
        if (rend != null && cuento.textura != null)
            rend.materials[indiceMaterial].mainTexture = cuento.textura;

        Animator anim = hoja.GetComponentInChildren<Animator>();
        if (anim != null)
            anim.speed = 0f;

        return hoja;
    }

    private void CachearDuracionClip(GameObject hoja)
    {
        Animator anim = hoja.GetComponentInChildren<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;
            if (clips.Length > 0)
                duracionClip = clips[0].length;
        }

        if (duracionClip <= 0)
            duracionClip = 1f;

        Debug.Log($"[SelectorDeCuentos] ⏱️ Duración clip: {duracionClip}s → con velocidad {velocidadAnimacion}x = {duracionClip / velocidadAnimacion}s");
    }

    // --- Pasar hojas ---

    public void PasarAdelante()
    {
        if (animando || paginaActual >= cuentos.Length - 1) return;
        StartCoroutine(AnimarAdelante());
    }

    public void PasarAtras()
    {
        if (animando || paginaActual <= 0) return;
        StartCoroutine(AnimarAtras());
    }

    private void ActualizarMusica(int indice)
    {
        if (GlobalAudioManager.Instance == null) return;
        AudioClip musica = cuentos[indice].musicaCuento;
        if (musica != null)
            GlobalAudioManager.Instance.ReproducirMusica(musica);
    }

    private void ReproducirSonidoHoja()
    {
        if (GlobalAudioManager.Instance != null)
            GlobalAudioManager.Instance.ReproducirSonidoPasarHoja();
    }

    private IEnumerator AnimarAdelante()
    {
        animando = true;
        ReproducirSonidoHoja();

        Animator anim = hojaVisible.GetComponentInChildren<Animator>();
        anim.speed = velocidadAnimacion;
        anim.Play(estadoAdelante, 0, 0f);

        yield return new WaitForSeconds(duracionClip / velocidadAnimacion);

        hojaVisible.SetActive(false);
        Destroy(hojaVisible);

        hojaVisible = hojaDebajo;
        if (hojaVisible != null)
            hojaVisible.transform.localPosition = Vector3.zero;

        paginaActual++;

        if (paginaActual < cuentos.Length - 1)
            hojaDebajo = CrearHoja(cuentos[paginaActual + 1], offsetHojaDebajo);
        else
            hojaDebajo = null;

        ActualizarMusica(paginaActual);
        animando = false;
        Debug.Log($"[SelectorDeCuentos] 📄 Página {paginaActual + 1}/{cuentos.Length}: {cuentos[paginaActual].nombre}");
    }

    private IEnumerator AnimarAtras()
    {
        animando = true;
        ReproducirSonidoHoja();

        // Bajar la hoja actual antes de animar para evitar solapamiento
        if (hojaDebajo != null)
            Destroy(hojaDebajo);

        hojaVisible.transform.localPosition = offsetHojaDebajo;
        hojaDebajo = hojaVisible;

        GameObject hojaEncima = CrearHoja(cuentos[paginaActual - 1], Vector3.zero);

        Animator anim = hojaEncima.GetComponentInChildren<Animator>();
        anim.speed = velocidadAnimacion;
        anim.Play(estadoAtras, 0, 0f);

        yield return new WaitForSeconds(duracionClip / velocidadAnimacion);

        hojaVisible = hojaEncima;

        paginaActual--;

        ActualizarMusica(paginaActual);
        animando = false;
        Debug.Log($"[SelectorDeCuentos] 📄 Página {paginaActual + 1}/{cuentos.Length}: {cuentos[paginaActual].nombre}");
    }

    // --- Selección ---

    public void SeleccionarCuento()
    {
        if (animando || cuentos == null || cuentos.Length == 0) return;

        string rutaTutorial = Path.Combine(Application.persistentDataPath, archivoTutorial);
        bool tutorialCompletado = File.Exists(rutaTutorial) &&
                                  File.ReadAllText(rutaTutorial).Contains("\"completado\":true");
        Debug.Log($"[SelectorDeCuentos] 🔍 Tutorial en: {rutaTutorial} → completado: {tutorialCompletado}");

        string escenaDestino = tutorialCompletado
            ? cuentos[paginaActual].nombreEscena
            : nombreEscenaTutorial;

        Debug.Log($"[SelectorDeCuentos] 📖 Seleccionado: {cuentos[paginaActual].nombre} " +
                  $"→ Tutorial completado: {tutorialCompletado} → '{escenaDestino}'");

        SceneManager.LoadScene(escenaDestino);
    }

    void OnDestroy()
    {
        if (botonSeleccionar != null)
            botonSeleccionar.onClick.RemoveListener(SeleccionarCuento);
    }
}
