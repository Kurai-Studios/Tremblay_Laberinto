using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// 🧪 QuickStoryTest - VERSIÓN COMPATIBLE Y CORREGIDA
/// Compatible con todas las versiones del StoryUIController
/// Sin errores de compilación
/// </summary>
public class QuickStoryTest : MonoBehaviour
{
    [Header("🎯 Fragmento de Prueba")]
    [SerializeField] private StoryFragment fragmento;
    
    [Header("🖼️ Prueba de Imágenes")]
    [SerializeField] private Sprite imagenPrueba;
    [Tooltip("Crear fragmento de prueba automáticamente si no hay uno asignado")]
    [SerializeField] private bool crearFragmentoPrueba = true;
    
    [Header("📱 Configuración de Prueba")]
    [SerializeField] private bool mostrarLogsDePrueba = true;
    [SerializeField] private Color colorFondoPrueba = new Color(0.1f, 0.1f, 0.3f, 0.9f);
    [SerializeField] private Color colorTextoPrueba = Color.white;
    
    private InputAction testAction;
    private InputAction testWithImageAction;
    
    void Awake()
    {
        // Crear acciones de input
        testAction = new InputAction("TestStory", InputActionType.Button, "<Keyboard>/p");
        testWithImageAction = new InputAction("TestStoryWithImage", InputActionType.Button, "<Keyboard>/i");
        
        testAction.Enable();
        testWithImageAction.Enable();
        
        testAction.performed += ctx => ProbarFragmento();
        testWithImageAction.performed += ctx => ProbarFragmentoConImagen();
    }
    
    void OnDestroy()
    {
        if (testAction != null)
        {
            testAction.Disable();
            testAction.Dispose();
        }
        
        if (testWithImageAction != null)
        {
            testWithImageAction.Disable();
            testWithImageAction.Dispose();
        }
    }
    
    /// <summary>
    /// Probar el fragmento asignado o crear uno de prueba
    /// </summary>
    public void ProbarFragmento()
    {
        StoryFragment fragmentoAUsar = fragmento;
        
        // Si no hay fragmento asignado, crear uno de prueba
        if (fragmentoAUsar == null && crearFragmentoPrueba)
        {
            fragmentoAUsar = CrearFragmentoPruebaSinImagen();
        }
        
        if (fragmentoAUsar == null)
        {
            Debug.LogError("⚠️ No hay fragmento asignado y crearFragmentoPrueba está desactivado");
            return;
        }
        
        EjecutarPruebaFragmento(fragmentoAUsar, "Prueba básica");
    }
    
    /// <summary>
    /// Probar fragmento con imagen de fondo
    /// </summary>
    public void ProbarFragmentoConImagen()
    {
        StoryFragment fragmentoConImagen;
        
        if (fragmento != null && fragmento.imagenFondo != null)
        {
            // Usar el fragmento asignado si ya tiene imagen
            fragmentoConImagen = fragmento;
        }
        else
        {
            // Crear fragmento de prueba con imagen
            fragmentoConImagen = CrearFragmentoPruebaConImagen();
        }
        
        if (fragmentoConImagen == null)
        {
            Debug.LogError("⚠️ No se pudo crear fragmento con imagen");
            return;
        }
        
        EjecutarPruebaFragmento(fragmentoConImagen, "Prueba con imagen");
    }
    
    /// <summary>
    /// Ejecutar la prueba del fragmento
    /// </summary>
    private void EjecutarPruebaFragmento(StoryFragment fragmentoAUsar, string tipoPrueba)
    {
        if (StoryManager.Instance == null)
        {
            Debug.LogError("⚠️ StoryManager no encontrado");
            return;
        }
        
        if (mostrarLogsDePrueba)
        {
            Debug.Log($"🎬 === {tipoPrueba.ToUpper()} ===");
            Debug.Log($"Fragmento: {fragmentoAUsar.fragmentID}");
            Debug.Log($"Nombre: {fragmentoAUsar.nombreFragmento}");
            Debug.Log($"Tiene imagen: {(fragmentoAUsar.imagenFondo != null ? "SÍ" : "NO")}");
            if (fragmentoAUsar.imagenFondo != null)
            {
                Debug.Log($"Imagen: {fragmentoAUsar.imagenFondo.name}");
            }
            Debug.Log($"Dispositivo: {(EsMobil() ? "MÓVIL" : "TABLET")}");
            Debug.Log($"Resolución: {Screen.width}x{Screen.height}");
        }
        
        // Forzar reproducción
        StoryManager.Instance.ReproducirFragmento(fragmentoAUsar, true);
    }
    
    /// <summary>
    /// Crear fragmento de prueba sin imagen de fondo
    /// </summary>
    private StoryFragment CrearFragmentoPruebaSinImagen()
    {
        StoryFragment nuevo = ScriptableObject.CreateInstance<StoryFragment>();
        
        nuevo.fragmentID = "prueba_sin_imagen";
        nuevo.nombreFragmento = "Prueba Sin Imagen";
        nuevo.textoFragmento = "Este es un fragmento de prueba SIN imagen de fondo. " +
                              "Deberías ver solo el color de fondo sólido configurado en el fragmento.";
        
        nuevo.colorFondo = colorFondoPrueba;
        nuevo.colorTexto = colorTextoPrueba;
        nuevo.usarTypewriter = true;
        nuevo.velocidadTypewriter = 45f;
        nuevo.avanceAutomatico = false;
        nuevo.tamañoFuente = EsMobil() ? 26 : 24;
        
        if (mostrarLogsDePrueba)
        {
            Debug.Log("✅ Fragmento de prueba sin imagen creado");
        }
        
        return nuevo;
    }
    
    /// <summary>
    /// Crear fragmento de prueba con imagen de fondo
    /// </summary>
    private StoryFragment CrearFragmentoPruebaConImagen()
    {
        StoryFragment nuevo = ScriptableObject.CreateInstance<StoryFragment>();
        
        nuevo.fragmentID = "prueba_con_imagen";
        nuevo.nombreFragmento = "Prueba Con Imagen";
        nuevo.textoFragmento = "Este fragmento INCLUYE una imagen de fondo. " +
                              "Deberías ver la imagen asignada detrás de este texto, " +
                              "con la transparencia adecuada para dispositivos móviles.";
        
        // Asignar imagen de prueba
        if (imagenPrueba != null)
        {
            nuevo.imagenFondo = imagenPrueba;
        }
        else
        {
            // Intentar cargar una imagen por defecto
            nuevo.imagenFondo = Resources.Load<Sprite>("ImagenPrueba");
            
            if (nuevo.imagenFondo == null)
            {
                Debug.LogWarning("⚠️ No se encontró imagen de prueba. Asigna 'imagenPrueba' o coloca una imagen llamada 'ImagenPrueba' en Resources/");
                return null;
            }
        }
        
        nuevo.colorFondo = new Color(0f, 0f, 0f, 0.3f); // Fondo más transparente para que se vea la imagen
        nuevo.colorTexto = Color.white;
        nuevo.usarTypewriter = true;
        nuevo.velocidadTypewriter = 40f;
        nuevo.avanceAutomatico = false;
        nuevo.tamañoFuente = EsMobil() ? 28 : 26;
        nuevo.intensidadViñeta = 0.4f;
        
        if (mostrarLogsDePrueba)
        {
            Debug.Log($"✅ Fragmento de prueba con imagen creado: {nuevo.imagenFondo.name}");
        }
        
        return nuevo;
    }
    
    /// <summary>
    /// Detectar si el dispositivo es móvil
    /// </summary>
    private bool EsMobil()
    {
        float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float diagonal = Mathf.Sqrt(screenWidth * screenWidth + screenHeight * screenHeight) / dpi;
        
        float aspectRatio = (float)Screen.width / Screen.height;
        bool esAspectRatioMovil = aspectRatio < 0.75f || aspectRatio > 1.5f;
        
        return diagonal < 7.0f || esAspectRatioMovil || Screen.width < 1200;
    }
    
    /// <summary>
    /// Probar múltiples fragmentos en secuencia
    /// </summary>
    [ContextMenu("🧪 Prueba Secuencial")]
    public void PruebaSecuencial()
    {
        StartCoroutine(EjecutarPruebaSecuencial());
    }
    
    private IEnumerator EjecutarPruebaSecuencial()
    {
        if (StoryManager.Instance == null)
        {
            Debug.LogError("⚠️ StoryManager no encontrado para prueba secuencial");
            yield break;
        }
        
        Debug.Log("🧪 === INICIANDO PRUEBA SECUENCIAL ===");
        
        // Prueba 1: Sin imagen
        StoryFragment sinImagen = CrearFragmentoPruebaSinImagen();
        if (sinImagen != null)
        {
            Debug.Log("🎬 Prueba 1: Sin imagen");
            StoryManager.Instance.ReproducirFragmento(sinImagen, true);
            yield return new WaitForSeconds(3f); // Esperar un poco antes de la siguiente
        }
        
        // Prueba 2: Con imagen
        StoryFragment conImagen = CrearFragmentoPruebaConImagen();
        if (conImagen != null)
        {
            Debug.Log("🎬 Prueba 2: Con imagen");
            StoryManager.Instance.ReproducirFragmento(conImagen, true);
            yield return new WaitForSeconds(3f);
        }
        
        Debug.Log("✅ Prueba secuencial completada");
    }
    
    /// <summary>
    /// Validar configuración actual - VERSIÓN COMPATIBLE
    /// </summary>
    [ContextMenu("🔍 Validar Configuración")]
    public void ValidarConfiguracion()
    {
        Debug.Log("🔍 === VALIDACIÓN DE CONFIGURACIÓN (COMPATIBLE) ===");
        
        // Verificar StoryManager
        if (StoryManager.Instance != null)
        {
            Debug.Log("✅ StoryManager encontrado");
        }
        else
        {
            Debug.LogError("❌ StoryManager NO encontrado");
        }
        
        // Verificar StoryUIController - VERSIÓN SEGURA
        StoryUIController uiController = FindObjectOfType<StoryUIController>();
        if (uiController != null)
        {
            Debug.Log("✅ StoryUIController encontrado");
            LlamarMetodoDebugSeguro(uiController);
        }
        else
        {
            Debug.LogWarning("⚠️ StoryUIController NO encontrado en la escena");
        }
        
        // Verificar fragmento asignado
        if (fragmento != null)
        {
            Debug.Log($"✅ Fragmento asignado: {fragmento.fragmentID}");
            if (fragmento.imagenFondo != null)
            {
                Debug.Log($"✅ Imagen asignada: {fragmento.imagenFondo.name}");
            }
            else
            {
                Debug.Log("ℹ️ Sin imagen asignada en el fragmento");
            }
        }
        else
        {
            Debug.Log("ℹ️ Sin fragmento asignado (se creará automáticamente si está habilitado)");
        }
        
        // Verificar imagen de prueba
        if (imagenPrueba != null)
        {
            Debug.Log($"✅ Imagen de prueba asignada: {imagenPrueba.name}");
        }
        else
        {
            Debug.Log("ℹ️ Sin imagen de prueba asignada");
        }
        
        // Info del dispositivo
        Debug.Log($"📱 Dispositivo detectado: {(EsMobil() ? "MÓVIL" : "TABLET")}");
        Debug.Log($"📱 Resolución: {Screen.width}x{Screen.height} | DPI: {Screen.dpi}");
        
        // Verificar otros sistemas
        InventarioToggleController inventario = FindObjectOfType<InventarioToggleController>();
        if (inventario != null)
        {
            Debug.Log("✅ InventarioToggleController encontrado");
        }
        else
        {
            Debug.Log("ℹ️ InventarioToggleController no encontrado");
        }
        
        ObjectInfoUIManager objectInfo = ObjectInfoUIManager.Instance;
        if (objectInfo != null)
        {
            Debug.Log("✅ ObjectInfoUIManager encontrado");
        }
        else
        {
            Debug.Log("ℹ️ ObjectInfoUIManager no encontrado");
        }
        
        Debug.Log("🔍 === FIN VALIDACIÓN ===");
    }
    
    /// <summary>
    /// Llamar método de debug de forma segura usando reflexión
    /// </summary>
    private void LlamarMetodoDebugSeguro(StoryUIController controller)
    {
        try
        {
            // Intentar métodos de debug en orden de preferencia
            var tipo = controller.GetType();
            
            // 1. Versión integrada: DebugEstadoCompleto
            var metodoCompleto = tipo.GetMethod("DebugEstadoCompleto");
            if (metodoCompleto != null)
            {
                metodoCompleto.Invoke(controller, null);
                Debug.Log("✅ Llamado DebugEstadoCompleto (versión integrada)");
                return;
            }
            
            // 2. Versión anterior: DebugEstadoUI
            var metodoBasico = tipo.GetMethod("DebugEstadoUI");
            if (metodoBasico != null)
            {
                metodoBasico.Invoke(controller, null);
                Debug.Log("✅ Llamado DebugEstadoUI (versión anterior)");
                return;
            }
            
            // 3. Si no hay métodos, mostrar info básica
            Debug.Log("ℹ️ StoryUIController no tiene método de debug conocido");
            Debug.Log($"ℹ️ Tipo de controller: {tipo.Name}");
            Debug.Log($"ℹ️ GameObject: {controller.gameObject.name}");
            
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Error al llamar método de debug: {e.Message}");
        }
    }
    
    /// <summary>
    /// Test de emergencia - crear y probar fragmento mínimo
    /// </summary>
    [ContextMenu("🚨 Test de Emergencia")]
    public void TestEmergencia()
    {
        Debug.Log("🚨 === TEST DE EMERGENCIA ===");
        
        // Crear fragmento mínimo
        StoryFragment emergencia = ScriptableObject.CreateInstance<StoryFragment>();
        emergencia.fragmentID = "emergencia_test";
        emergencia.nombreFragmento = "Test Emergencia";
        emergencia.textoFragmento = "🚨 TEST DE EMERGENCIA: Si puedes leer esto, el sistema básico funciona.";
        emergencia.usarTypewriter = false;
        emergencia.avanceAutomatico = false;
        emergencia.tiempoFadeIn = 0.2f;
        emergencia.tiempoFadeOut = 0.2f;
        
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.ReproducirFragmento(emergencia, true);
            Debug.Log("✅ Test de emergencia enviado");
        }
        else
        {
            Debug.LogError("❌ StoryManager no disponible para test de emergencia");
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 320, 250));
        
        GUILayout.Label("🧪 PRUEBAS DE HISTORIA (COMPATIBLE)", GUI.skin.box);
        
        if (GUILayout.Button("🎬 PROBAR FRAGMENTO (P)"))
        {
            ProbarFragmento();
        }
        
        if (GUILayout.Button("🖼️ PROBAR CON IMAGEN (I)"))
        {
            ProbarFragmentoConImagen();
        }
        
        if (GUILayout.Button("🧪 PRUEBA SECUENCIAL"))
        {
            PruebaSecuencial();
        }
        
        if (GUILayout.Button("🔍 VALIDAR CONFIG"))
        {
            ValidarConfiguracion();
        }
        
        if (GUILayout.Button("🚨 TEST EMERGENCIA"))
        {
            TestEmergencia();
        }
        
        GUILayout.Space(10);
        
        // Info del fragmento actual
        if (fragmento != null)
        {
            GUILayout.Label($"Fragmento: {fragmento.fragmentID}");
            GUILayout.Label($"Imagen: {(fragmento.imagenFondo != null ? "✅" : "❌")}");
        }
        else
        {
            GUILayout.Label("⚠️ Sin fragmento asignado");
        }
        
        GUILayout.Label($"Dispositivo: {(EsMobil() ? "📱 Móvil" : "🖥️ Tablet")}");
        GUILayout.Label($"Resolución: {Screen.width}x{Screen.height}");
        
        // Estado de otros sistemas
        GUILayout.Space(5);
        GUILayout.Label("--- ESTADO SISTEMAS ---", GUI.skin.box);
        
        bool storyManager = StoryManager.Instance != null;
        GUILayout.Label($"StoryManager: {(storyManager ? "✅" : "❌")}");
        
        bool storyUI = FindObjectOfType<StoryUIController>() != null;
        GUILayout.Label($"StoryUI: {(storyUI ? "✅" : "❌")}");
        
        bool inventario = FindObjectOfType<InventarioToggleController>() != null;
        GUILayout.Label($"Inventario: {(inventario ? "✅" : "❌")}");
        
        GUILayout.EndArea();
    }
}