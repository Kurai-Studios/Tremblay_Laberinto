using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Eventos del juego que hacen reaccionar a la calabaza.
/// </summary>
public enum EventoCalabaza
{
    Neutral,
    GuardarObjeto,
    NuevaMision,
    DescifrarMision,
    CompletarMision,
    FallarMision,
    LlevarObjeto
}

/// <summary>
/// 🎃 Calabaza reactiva: cambia el sprite de su "cara" según eventos del juego
/// (guardar objeto, nueva misión, descifrar, completar, fallar, agarrar objeto),
/// con un rebote sutil, y vuelve a una cara neutral tras unos segundos.
///
/// Se mantiene SIMPLE a propósito: el último evento manda (cambia la cara y
/// reinicia el temporizador). No se coordina con el panel de narración.
///
/// Montaje: va en la calabaza (Image base sin cara) que vive en el borde superior
/// del panel de inventario. La cara es una Image hija cuyo sprite cambia aquí.
/// </summary>
public class CalabazaReactivaController : MonoBehaviour
{
    [Serializable]
    public struct ReaccionCalabaza
    {
        public EventoCalabaza evento;
        public Sprite cara;
        [Tooltip("Segundos que se muestra antes de volver a neutral (0 = usar la duración global).")]
        public float duracionOverride;
    }

    [Header("Referencias")]
    [Tooltip("La Image hija (sobre la calabaza) cuyo sprite cambia según el evento.")]
    [SerializeField] private Image imagenCara;
    [Tooltip("Cara por defecto/en reposo.")]
    [SerializeField] private Sprite caraNeutral;

    [Header("Reacciones (una por evento)")]
    [SerializeField] private ReaccionCalabaza[] reacciones;

    [Header("Tiempos")]
    [Tooltip("Segundos que dura la reacción antes de volver a neutral (si no hay override).")]
    [SerializeField] private float duracionReaccion = 1.5f;
    [Tooltip("Duración del rebote (pop de escala).")]
    [SerializeField] private float duracionRebote = 0.25f;
    [Tooltip("Escala máxima del rebote (1.15 = crece un 15%).")]
    [SerializeField] private float escalaRebote = 1.15f;
    [SerializeField] private AnimationCurve curvaRebote = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Debug")]
    [SerializeField] private bool debug = false;

    private readonly Dictionary<EventoCalabaza, ReaccionCalabaza> _mapa =
        new Dictionary<EventoCalabaza, ReaccionCalabaza>();
    private Coroutine _reaccionActual;
    private Vector3 _escalaBase = Vector3.one;
    private bool _suscrito = false;

    void Awake()
    {
        if (imagenCara != null)
            _escalaBase = imagenCara.rectTransform.localScale;

        _mapa.Clear();
        if (reacciones != null)
        {
            foreach (var r in reacciones)
            {
                if (r.cara != null)
                    _mapa[r.evento] = r;
            }
        }
    }

    void Start()
    {
        PonerCaraNeutral();
        Suscribir();
    }

    void OnDestroy()
    {
        Desuscribir();
    }

    // ================= Suscripción a eventos del juego =================

    private void Suscribir()
    {
        if (_suscrito) return;

        if (GameObjectManager.Instance != null)
            GameObjectManager.Instance.OnObjetoGuardado += HandleGuardar;

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMisionDescifrada += HandleDescifrar;
            MissionManager.Instance.OnMisionCompletada += HandleCompletar;
            MissionManager.Instance.OnMisionFallida += HandleFallar;
            MissionManager.Instance.OnNuevaMisionDisponible += HandleNuevaMision;
        }

        ObjectInfoGrabDropController.OnObjetoAgarrado += HandleAgarrar;

        _suscrito = true;

        if (debug) Debug.Log("[Calabaza] Suscrita a eventos del juego.");
    }

    private void Desuscribir()
    {
        if (GameObjectManager.Instance != null)
            GameObjectManager.Instance.OnObjetoGuardado -= HandleGuardar;

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMisionDescifrada -= HandleDescifrar;
            MissionManager.Instance.OnMisionCompletada -= HandleCompletar;
            MissionManager.Instance.OnMisionFallida -= HandleFallar;
            MissionManager.Instance.OnNuevaMisionDisponible -= HandleNuevaMision;
        }

        ObjectInfoGrabDropController.OnObjetoAgarrado -= HandleAgarrar;

        _suscrito = false;
    }

    // Handlers con nombre (para poder desuscribir con -=)
    private void HandleGuardar(string id) => ReaccionarA(EventoCalabaza.GuardarObjeto);
    private void HandleDescifrar(string id) => ReaccionarA(EventoCalabaza.DescifrarMision);
    private void HandleCompletar(string id) => ReaccionarA(EventoCalabaza.CompletarMision);
    private void HandleFallar() => ReaccionarA(EventoCalabaza.FallarMision);
    private void HandleNuevaMision(Mission m) => ReaccionarA(EventoCalabaza.NuevaMision);
    private void HandleAgarrar(string id) => ReaccionarA(EventoCalabaza.LlevarObjeto);

    // ================= Lógica de reacción =================

    /// <summary>
    /// Dispara la reacción de un evento: cambia la cara + rebote y vuelve a neutral.
    /// El último evento manda (reinicia cualquier reacción en curso).
    /// </summary>
    public void ReaccionarA(EventoCalabaza evento)
    {
        if (imagenCara == null)
        {
            if (debug) Debug.LogWarning("[Calabaza] imagenCara no asignada.");
            return;
        }

        if (!_mapa.TryGetValue(evento, out var reaccion) || reaccion.cara == null)
        {
            if (debug) Debug.LogWarning($"[Calabaza] Sin cara asignada para el evento {evento}.");
            return;
        }

        if (_reaccionActual != null)
            StopCoroutine(_reaccionActual);

        float duracion = reaccion.duracionOverride > 0f ? reaccion.duracionOverride : duracionReaccion;
        _reaccionActual = StartCoroutine(ReaccionCoroutine(reaccion.cara, duracion));

        if (debug) Debug.Log($"[Calabaza] Reacción: {evento} ({duracion}s)");
    }

    private IEnumerator ReaccionCoroutine(Sprite cara, float duracion)
    {
        // Partir siempre desde la escala base (por si se interrumpió a media animación)
        imagenCara.rectTransform.localScale = _escalaBase;

        imagenCara.sprite = cara;
        yield return Rebote();

        yield return new WaitForSeconds(duracion);

        imagenCara.sprite = caraNeutral;
        yield return Rebote();

        imagenCara.rectTransform.localScale = _escalaBase;
        _reaccionActual = null;
    }

    // Se ejecuta INLINE (yield return Rebote()) para que StopCoroutine también lo detenga.
    private IEnumerator Rebote()
    {
        RectTransform rt = imagenCara.rectTransform;
        float mitad = Mathf.Max(0.01f, duracionRebote * 0.5f);
        Vector3 pico = _escalaBase * escalaRebote;

        float t = 0f;
        while (t < mitad)
        {
            t += Time.deltaTime;
            float k = curvaRebote.Evaluate(t / mitad);
            rt.localScale = Vector3.Lerp(_escalaBase, pico, k);
            yield return null;
        }

        t = 0f;
        while (t < mitad)
        {
            t += Time.deltaTime;
            float k = curvaRebote.Evaluate(t / mitad);
            rt.localScale = Vector3.Lerp(pico, _escalaBase, k);
            yield return null;
        }

        rt.localScale = _escalaBase;
    }

    private void PonerCaraNeutral()
    {
        if (imagenCara != null && caraNeutral != null)
            imagenCara.sprite = caraNeutral;
    }

    // ================= Tests (Inspector, en Play Mode) =================

    [ContextMenu("🎃 Test Guardar Objeto")]   private void TestGuardar()   => ReaccionarA(EventoCalabaza.GuardarObjeto);
    [ContextMenu("🎃 Test Nueva Misión")]      private void TestNueva()     => ReaccionarA(EventoCalabaza.NuevaMision);
    [ContextMenu("🎃 Test Descifrar Misión")]  private void TestDescifrar() => ReaccionarA(EventoCalabaza.DescifrarMision);
    [ContextMenu("🎃 Test Completar Misión")]  private void TestCompletar() => ReaccionarA(EventoCalabaza.CompletarMision);
    [ContextMenu("🎃 Test Fallar Misión")]     private void TestFallar()    => ReaccionarA(EventoCalabaza.FallarMision);
    [ContextMenu("🎃 Test Llevar Objeto")]     private void TestLlevar()    => ReaccionarA(EventoCalabaza.LlevarObjeto);
}
