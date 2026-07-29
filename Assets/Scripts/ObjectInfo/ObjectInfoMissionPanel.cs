using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Controlador del panel de misiones para ObjectInfo - Maneja visualización y sprites de misiones
/// </summary>
public class ObjectInfoMissionPanel : MonoBehaviour
{
    [Header("Mission Panel Configuration")]
    [SerializeField] private Transform panelMisiones;
    [SerializeField] private Transform contenedorTextosMision;
    [SerializeField] private Transform contenedorSpritesColocados;
    
    [Header("Mission Prefabs")]
    [SerializeField] private GameObject textoMisionPrefab;
    [SerializeField] private GameObject textoMisionMultiplePrefab;
    [SerializeField] private GameObject tituloMultiplesPrefab;
    [SerializeField] private GameObject spriteObjetoPrefab;
    
    [Header("Visual Configuration")]
    [SerializeField] private Vector2 tamanoSpriteConfigurable = new Vector2(50, 50);
    [SerializeField] private Color colorSpriteColocado = Color.white;
    [SerializeField] private GameObject imagenObjetoColocadoPrefab; // Fallback prefab
    
    // Referencias a otros managers
    private ObjectInfoUIManager mainManager;
    private MissionManager missionManager;
    private GameObjectManager gameObjectManager;
    
    void Awake()
    {
        mainManager = GetComponent<ObjectInfoUIManager>();
        missionManager = MissionManager.Instance;
        gameObjectManager = GameObjectManager.Instance;
    }
    
    /// <summary>
    /// Configura el panel de misiones con información visual
    /// </summary>
    public void ConfigurarPanelMisionesVisual(string objetoID, string nombreObjeto, Transform panelRef)
    {
        if (panelRef != null) 
            panelMisiones = panelRef;
            
        if (panelMisiones == null || missionManager == null) return;

        bool tieneMisiones = missionManager.TieneMisionesDescifradasPendientes(objetoID);
        panelMisiones.gameObject.SetActive(tieneMisiones);
        if (!tieneMisiones) return;

        // Limpiar contenido anterior
        LimpiarPanelMisiones();

        // Mostrar texto de la misión descifrada
        MostrarTextoMisionDescifrada(objetoID);

        // Mostrar objetos colocados como sprites
        MostrarObjetosColocadosComoSprites(objetoID);
    }
    
    /// <summary>
    /// Busca y configura los contenedores del panel de misiones
    /// </summary>
    public void BuscarContenedoresPanelMisiones(Transform panelRef)
    {
        if (panelRef != null)
            panelMisiones = panelRef;
            
        if (panelMisiones == null) return;
        
        // Buscar contenedores con búsqueda directa y profunda
        contenedorTextosMision = panelMisiones.Find("ContenedorTextosMision");
        if (contenedorTextosMision == null)
            contenedorTextosMision = FindDeepChild(panelMisiones, "ContenedorTextosMision");
        
        contenedorSpritesColocados = panelMisiones.Find("ContenedorSpritesColocados");
        if (contenedorSpritesColocados == null)
            contenedorSpritesColocados = FindDeepChild(panelMisiones, "ContenedorSpritesColocados");
        
        Debug.Log($"[MissionPanel] ContenedorTextosMision encontrado: {contenedorTextosMision != null}");
        Debug.Log($"[MissionPanel] ContenedorSpritesColocados encontrado: {contenedorSpritesColocados != null}");
        
        if (contenedorTextosMision == null)
            Debug.LogWarning("[MissionPanel] ContenedorTextosMision no encontrado en PanelMisiones");
        
        if (contenedorSpritesColocados == null)
            Debug.LogWarning("[MissionPanel] ContenedorSpritesColocados no encontrado en PanelMisiones");
    }
    
    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform result = FindDeepChild(child, childName);
            if (result != null) return result;
        }
        return null;
    }
    
    private void MostrarTextoMisionDescifrada(string objetoDestinoID)
    {
        if (missionManager == null) return;

        // Obtener TODAS las misiones descifradas para este destino
        List<Mission> misionesDescifradas = missionManager.ObtenerMisionesPorObjetoDestino(objetoDestinoID)
            .Where(m => missionManager.MisionesDescifradas.Contains(m.misionID) && 
                       !missionManager.MisionesCompletadas.Contains(m.misionID))
            .ToList();

        if (misionesDescifradas.Count == 0) return;

        if (misionesDescifradas.Count == 1)
        {
            // Caso simple: una sola misión
            CrearTextoMision(misionesDescifradas[0]);
        }
        else
        {
            // Caso múltiple: varias misiones
            Debug.Log($"📋 [MissionPanel] Múltiples misiones descifradas ({misionesDescifradas.Count}) para {objetoDestinoID}");
            
            // Crear título "Misiones disponibles"
            CrearTituloMultiplesMisiones();
            
            // Crear una entrada para cada misión
            for (int i = 0; i < misionesDescifradas.Count; i++)
            {
                CrearTextoMisionIndividual(misionesDescifradas[i], i, misionesDescifradas.Count);
            }
        }
    }
    
    private void CrearTituloMultiplesMisiones()
    {
        Transform contenedor = contenedorTextosMision != null ? contenedorTextosMision : panelMisiones;
        if (contenedor == null) return;

        GameObject tituloItem;
        
        if (tituloMultiplesPrefab != null)
        {
            tituloItem = Instantiate(tituloMultiplesPrefab, contenedor);
        }
        else
        {
            // Crear título básico si no hay prefab
            tituloItem = new GameObject("TituloMultiples");
            tituloItem.transform.SetParent(contenedor, false);
            
            TextMeshProUGUI texto = tituloItem.AddComponent<TextMeshProUGUI>();
            texto.text = "Available missions:";
            texto.fontSize = 18;
            texto.fontStyle = FontStyles.Bold;
        }

        Debug.Log($"📋 [MissionPanel] Título múltiples misiones creado");
    }
    
    private void CrearTextoMisionIndividual(Mission mision, int indice, int total)
    {
        Transform contenedor = contenedorTextosMision != null ? contenedorTextosMision : panelMisiones;
        if (contenedor == null) return;

        GameObject textoItem;
        
        if (textoMisionMultiplePrefab != null)
        {
            textoItem = Instantiate(textoMisionMultiplePrefab, contenedor);
        }
        else if (textoMisionPrefab != null)
        {
            textoItem = Instantiate(textoMisionPrefab, contenedor);
        }
        else
        {
            // Crear texto básico si no hay prefabs
            textoItem = new GameObject("TextoMision");
            textoItem.transform.SetParent(contenedor, false);
            
            TextMeshProUGUI texto = textoItem.AddComponent<TextMeshProUGUI>();
            texto.fontSize = 14;
        }

        // Configurar el texto
        TextMeshProUGUI tmp = textoItem.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = $"• {mision.descripcion}";
        }

        Debug.Log($"📋 [MissionPanel] Texto misión individual {indice + 1}/{total}: {mision.descripcion}");
    }
    
    private void CrearTextoMision(Mission mision)
    {
        if (mision == null) return;
        
        Debug.Log($"📦 [MissionPanel] Creando texto para misión: {mision.descripcion}");

        Transform contenedor = contenedorTextosMision != null ? contenedorTextosMision : panelMisiones;
        if (contenedor == null) return;

        GameObject textoItem;
        
        if (textoMisionPrefab != null)
        {
            textoItem = Instantiate(textoMisionPrefab, contenedor);
            Debug.Log($"📦 [MissionPanel] Prefab instanciado desde textoMisionPrefab");
        }
        else
        {
            // Crear elemento básico si no hay prefab
            textoItem = new GameObject("TextoMision");
            textoItem.transform.SetParent(contenedor, false);
            
            TextMeshProUGUI texto = textoItem.AddComponent<TextMeshProUGUI>();
            texto.text = mision.descripcion;
            texto.fontSize = 16;
            
            Debug.Log($"📦 [MissionPanel] Texto básico creado: {mision.descripcion}");
            return;
        }

        // Configurar el prefab instanciado
        TextMeshProUGUI tmp = textoItem.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = mision.descripcion;
            Debug.Log($"📦 [MissionPanel] Texto configurado en prefab: {mision.descripcion}");
        }
        else
        {
            Debug.LogWarning($"📦 [MissionPanel] No se encontró TextMeshProUGUI en el prefab instanciado");
        }
    }
    
    private void MostrarObjetosColocadosComoSprites(string objetoDestinoID)
    {
        if (mainManager == null) return;
        
        List<string> objetosColocados = mainManager.ObtenerObjetosColocados(objetoDestinoID);
        
        if (objetosColocados.Count == 0)
        {
            Debug.Log($"[MissionPanel] No hay objetos colocados para mostrar en {objetoDestinoID}");
            return;
        }

        Debug.Log($"[MissionPanel] Mostrando {objetosColocados.Count} objetos colocados como sprites");

        // Obtener sprites existentes para evitar duplicados
        Transform contenedor = contenedorSpritesColocados != null ? contenedorSpritesColocados : panelMisiones;
        HashSet<string> spritesExistentes = new HashSet<string>();
        
        if (contenedor != null)
        {
            foreach (Transform child in contenedor)
            {
                if (child.name.StartsWith("Sprite_"))
                {
                    string nombreObjeto = child.name.Replace("Sprite_", "");
                    spritesExistentes.Add(nombreObjeto);
                }
            }
        }

        // Solo crear sprites que no existan
        foreach (string objetoID in objetosColocados)
        {
            if (gameObjectManager == null) continue;
            
            GameObjectData datos = gameObjectManager.BuscarObjetoPorId(objetoID);
            if (datos != null && !spritesExistentes.Contains(datos.nombreEspanol))
            {
                CrearSpriteObjetoColocado(objetoID);
            }
        }

        Debug.Log($"[MissionPanel] Sprites actualizados para {objetosColocados.Count} objetos");
    }
    
    /// <summary>
    /// Crea un sprite visual para un objeto colocado
    /// </summary>
    public void CrearSpriteObjetoColocado(string objetoID)
    {
        if (gameObjectManager == null) return;

        GameObjectData datos = gameObjectManager.BuscarObjetoPorId(objetoID);
        if (datos == null) return;

        Transform contenedor = contenedorSpritesColocados != null ? contenedorSpritesColocados : panelMisiones;
        if (contenedor == null) return;

        GameObject spriteItem;
        
        if (spriteObjetoPrefab != null)
        {
            spriteItem = Instantiate(spriteObjetoPrefab, contenedor);
        }
        else if (imagenObjetoColocadoPrefab != null)
        {
            spriteItem = Instantiate(imagenObjetoColocadoPrefab, contenedor);
        }
        else
        {
            // Crear sprite básico
            spriteItem = new GameObject($"Sprite_{datos.nombreEspanol}");
            spriteItem.transform.SetParent(contenedor, false);
            Image img = spriteItem.AddComponent<Image>();
            RectTransform rect = spriteItem.GetComponent<RectTransform>();
            rect.sizeDelta = tamanoSpriteConfigurable;
        }

        RectTransform spriteRect = spriteItem.GetComponent<RectTransform>();
        if (spriteRect != null)
        {
            spriteRect.sizeDelta = tamanoSpriteConfigurable;
        }

        // Configurar imagen
        Image imagen = spriteItem.GetComponentInChildren<Image>();
        if (imagen != null && !string.IsNullOrEmpty(datos.sprite2DPath))
        {
            Sprite sprite = Resources.Load<Sprite>(datos.sprite2DPath);
            if (sprite != null)
            {
                imagen.sprite = sprite;
                imagen.color = colorSpriteColocado;
                Debug.Log($"✅ [MissionPanel] Sprite asignado a {datos.nombreEspanol}");
            }
        }

        // Configurar texto
        TextMeshProUGUI texto = spriteItem.GetComponentInChildren<TextMeshProUGUI>();
        if (texto != null)
        {
            texto.text = datos.nombreEspanol;
        }

        // 🗑️ Botón "X" para retirar este objeto del panel de comprobación (si el prefab lo tiene)
        ConfigurarBotonQuitarSprite(spriteItem, objetoID);

        Debug.Log($"[MissionPanel] Sprite creado para {datos.nombreEspanol}");
    }

    /// <summary>
    /// Engancha el botón "BotonQuitar" del sprite colocado para retirar ESE objeto del panel.
    /// El destino es el objeto que se está mostrando ahora (mainManager.ObjetoActualID).
    /// </summary>
    private void ConfigurarBotonQuitarSprite(GameObject spriteItem, string objetoID)
    {
        if (spriteItem == null) return;

        Transform btnT = spriteItem.transform.Find("BotonQuitar");
        if (btnT == null) return;

        Button botonQuitar = btnT.GetComponent<Button>();
        if (botonQuitar == null) return;

        string idCapturado = objetoID;
        GameObject spriteRef = spriteItem;

        botonQuitar.onClick.RemoveAllListeners();
        botonQuitar.onClick.AddListener(() =>
        {
            string destino = mainManager != null ? mainManager.ObjetoActualID : null;
            if (mainManager != null && !string.IsNullOrEmpty(destino))
                mainManager.RetirarObjetoColocado(destino, idCapturado);

            if (spriteRef != null) Destroy(spriteRef);
        });
    }
    
    /// <summary>
    /// Limpia todo el contenido del panel de misiones
    /// </summary>
    public void LimpiarPanelMisiones()
    {
        // Limpiar contenedor de textos
        if (contenedorTextosMision != null)
        {
            for (int i = contenedorTextosMision.childCount - 1; i >= 0; i--)
            {
                Transform child = contenedorTextosMision.GetChild(i);
                Debug.Log($"[MissionPanel] Eliminando texto del contenedor: {child.name}");
                Destroy(child.gameObject);
            }
        }
        
        // Limpiar contenedor de sprites
        if (contenedorSpritesColocados != null)
        {
            for (int i = contenedorSpritesColocados.childCount - 1; i >= 0; i--)
            {
                Transform child = contenedorSpritesColocados.GetChild(i);
                Debug.Log($"[MissionPanel] Eliminando sprite del contenedor: {child.name}");
                Destroy(child.gameObject);
            }
        }
        
        // Fallback: limpiar panelMisiones directamente solo si NO hay contenedores
        if (panelMisiones != null && contenedorTextosMision == null && contenedorSpritesColocados == null)
        {
            for (int i = panelMisiones.childCount - 1; i >= 0; i--)
            {
                Transform child = panelMisiones.GetChild(i);
                
                // NO destruir los contenedores si existen
                if (child.name == "ContenedorTextosMision" || child.name == "ContenedorSpritesColocados")
                    continue;
                
                if (child.name.StartsWith("Sprite_") || 
                    child.name == "TextoMision" ||
                    child.name == "TituloMultiples" ||
                    child.name.Contains("Mision"))
                {
                    Debug.Log($"[MissionPanel] Eliminando del panel principal: {child.name}");
                    Destroy(child.gameObject);
                }
            }
        }
        
        Debug.Log($"[MissionPanel] Panel de misiones limpiado");
    }
    
    // Getters públicos para compatibilidad
    public Transform PanelMisiones => panelMisiones;
    public Transform ContenedorTextosMision => contenedorTextosMision;
    public Transform ContenedorSpritesColocados => contenedorSpritesColocados;
}
