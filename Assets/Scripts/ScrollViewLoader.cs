using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ScrollViewLoader : MonoBehaviour
{
    [Header("Referencias")]
    public GameObjectManager gameObjectManager;
    public Transform content;
    public GameObject itemPrefab;
    
    [Header("Debug Android")]
    public bool debugAndroid = true;
    public float tiempoEsperaAndroid = 1f;

    private bool cargaInicial = false;
    private Dictionary<string, GameObject> itemsCreados = new Dictionary<string, GameObject>(); // 👈 NUEVO

    void Start()
    {
        if (debugAndroid) Debug.Log("[ScrollViewLoader] Start - Buscando GameObjectManager...");

        if (gameObjectManager == null)
            gameObjectManager = GameObjectManager.Instance;

        if (gameObjectManager != null)
        {
            if (debugAndroid) Debug.Log("[ScrollViewLoader] GameObjectManager encontrado, suscribiéndose a eventos...");
            
            gameObjectManager.OnDatosCargados += HandleDatosCargados;
            
            // En Android, esperar un poco más antes de cargar
            if (Application.platform == RuntimePlatform.Android)
            {
                StartCoroutine(CargarSpritesAndroidCoroutine());
            }
            else
            {
                if (gameObjectManager.datosCargados && !cargaInicial)
                {
                    RefrescarSprites();
                }
            }
        }
        else
        {
            Debug.LogError("[ScrollViewLoader] No se encontró GameObjectManager.");
        }
    }

    private IEnumerator CargarSpritesAndroidCoroutine()
    {
        if (debugAndroid) Debug.Log("[ScrollViewLoader] Esperando carga inicial en Android...");
        
        // Esperar a que el GameObjectManager termine de cargar
        yield return new WaitForSeconds(tiempoEsperaAndroid);
        
        // Verificar múltiples veces si es necesario
        int intentos = 0;
        while (!gameObjectManager.datosCargados && intentos < 10)
        {
            if (debugAndroid) Debug.Log($"[ScrollViewLoader] Intento {intentos + 1} - Datos cargados: {gameObjectManager.datosCargados}");
            yield return new WaitForSeconds(0.5f);
            intentos++;
        }
        
        if (gameObjectManager.datosCargados && !cargaInicial)
        {
            if (debugAndroid) Debug.Log("[ScrollViewLoader] Datos listos, cargando sprites...");
            RefrescarSprites();
        }
        else if (gameObjectManager.listaObjetos.Count == 0)
        {
            if (debugAndroid) Debug.Log("[ScrollViewLoader] Lista vacía, agregando objetos de ejemplo...");
            gameObjectManager.AgregarObjetosEjemplo();
        }
    }

    void HandleDatosCargados()
    {
        if (debugAndroid) 
        {
            Debug.Log($"[ScrollViewLoader] HandleDatosCargados - Total objetos: {gameObjectManager?.listaObjetos?.Count ?? 0}");
        }
        
        if (cargaInicial)
        {
            // Si ya cargamos inicialmente, solo actualizar los items existentes
            ActualizarItemsExistentes();
        }
        else
        {
            // Primera carga, crear todos los items
            RefrescarSprites();
        }
    }

    // 👈 NUEVO: Actualiza solo los items existentes sin recrear
    private void ActualizarItemsExistentes()
    {
        if (debugAndroid) Debug.Log("[ScrollViewLoader] Actualizando items existentes...");
        
        if (gameObjectManager == null || gameObjectManager.listaObjetos == null)
        {
            Debug.LogWarning("[ScrollViewLoader] GameObjectManager o lista de objetos es null!");
            return;
        }

        int itemsActualizados = 0;

        foreach (var data in gameObjectManager.ObtenerObjetosDelCuentoActual())
        {
            if (data == null || string.IsNullOrEmpty(data.id)) continue;

            // Buscar el item existente en el diccionario
            if (itemsCreados.ContainsKey(data.id))
            {
                GameObject item = itemsCreados[data.id];
                if (item != null)
                {
                    ActualizarItem(item, data);
                    itemsActualizados++;
                }
                else
                {
                    // El item fue destruido, remover del diccionario
                    itemsCreados.Remove(data.id);
                }
            }
            else
            {
                // Item nuevo que no existe, crearlo
                CrearNuevoItem(data);
            }
        }

        if (debugAndroid) Debug.Log($"[ScrollViewLoader] {itemsActualizados} items actualizados.");
    }

    // 👈 NUEVO: Actualiza un item específico
    private void ActualizarItem(GameObject item, GameObjectData data)
    {
        try
        {
            // Actualizar DraggableItem
            DraggableItem draggable = item.GetComponent<DraggableItem>();
            if (draggable != null)
            {
                draggable.objetoID = data.id;
                draggable.esArrastrable = data.guardadoPorJugador;
                if (debugAndroid) Debug.Log($"[ScrollViewLoader] DraggableItem actualizado - ID: {data.id}, Arrastrable: {data.guardadoPorJugador}");
            }

            // Actualizar imagen y color
            Transform spriteTransform = item.transform.Find("SpriteImage");
            if (spriteTransform != null)
            {
                Image img = spriteTransform.GetComponent<Image>();
                if (img != null)
                {
                    // Actualizar sprite si es necesario
                    if (!string.IsNullOrEmpty(data.sprite2DPath))
                    {
                        Sprite sprite = Resources.Load<Sprite>(data.sprite2DPath);
                        if (sprite != null && img.sprite != sprite)
                        {
                            img.sprite = sprite;
                        }
                    }

                    // Actualizar color según estado guardado
                    Color nuevoColor = data.guardadoPorJugador
                        ? Color.white
                        : new Color(0.5f, 0.5f, 0.5f, 0.7f);

                    if (img.color != nuevoColor)
                    {
                        img.color = nuevoColor;
                        if (debugAndroid) Debug.Log($"[ScrollViewLoader] Color actualizado para {data.id} - Guardado: {data.guardadoPorJugador}");
                    }
                }
            }

            // 🔍 Refrescar el botón de info (aparece cuando el objeto pasa a "encontrado")
            ConfigurarBotonInfo(item, data);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScrollViewLoader] Error actualizando item {data.id}: {e.Message}");
        }
    }

    // 👈 NUEVO: Crea un item nuevo
    private void CrearNuevoItem(GameObjectData data)
    {
        try
        {
            GameObject item = Instantiate(itemPrefab, content);
            ConfigurarItem(item, data);
            itemsCreados[data.id] = item;
            
            if (debugAndroid) Debug.Log($"[ScrollViewLoader] Nuevo item creado para: {data.nombreEspanol} (ID: {data.id})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScrollViewLoader] Error creando nuevo item para {data.nombreEspanol}: {e.Message}");
        }
    }

    // 👈 NUEVO: Configura un item (usado tanto para nuevos como existentes)
    private void ConfigurarItem(GameObject item, GameObjectData data)
    {
        // Configurar DraggableItem
        DraggableItem draggable = item.GetComponent<DraggableItem>();
        if (draggable != null)
        {
            draggable.objetoID = data.id;
            draggable.esArrastrable = data.guardadoPorJugador;
        }

        // Configurar sprite e imagen
        Transform spriteTransform = item.transform.Find("SpriteImage");
        if (spriteTransform != null)
        {
            Image img = spriteTransform.GetComponent<Image>();
            if (img != null)
            {
                // Cargar sprite desde Resources
                if (!string.IsNullOrEmpty(data.sprite2DPath))
                {
                    Sprite sprite = Resources.Load<Sprite>(data.sprite2DPath);
                    if (sprite != null)
                    {
                        img.sprite = sprite;
                    }
                }

                // Configurar color según estado guardado
                img.color = data.guardadoPorJugador
                    ? Color.white
                    : new Color(0.5f, 0.5f, 0.5f, 0.7f);
            }
        }

        // 🔍 Botón de info (panel con sprite + nombre + audio)
        ConfigurarBotonInfo(item, data);
    }

    // 🔍 NUEVO: Engancha el botoncito "BotonInfo" del item. Solo visible en items ya
    // encontrados (guardadoPorJugador). Al pulsarlo abre el ItemInfoPanel compartido.
    private void ConfigurarBotonInfo(GameObject item, GameObjectData data)
    {
        Transform t = item.transform.Find("BotonInfo");
        if (t == null) return;

        Button botonInfo = t.GetComponent<Button>();
        if (botonInfo == null) return;

        // Solo disponible en objetos ya encontrados
        botonInfo.gameObject.SetActive(data.guardadoPorJugador);

        botonInfo.onClick.RemoveAllListeners();
        string id = data.id; // capturar por valor para el closure
        botonInfo.onClick.AddListener(() =>
        {
            if (gameObjectManager == null) return;
            GameObjectData d = gameObjectManager.BuscarObjetoPorId(id);
            if (d != null && ItemInfoPanel.Instance != null)
                ItemInfoPanel.Instance.Mostrar(d);
        });
    }

    public void RefrescarSprites()
    {
        if (debugAndroid) Debug.Log("[ScrollViewLoader] RefrescarSprites iniciado...");
        
        if (gameObjectManager == null)
        {
            Debug.LogError("[ScrollViewLoader] GameObjectManager es null!");
            return;
        }

        if (content == null)
        {
            Debug.LogError("[ScrollViewLoader] Content es null!");
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogError("[ScrollViewLoader] ItemPrefab es null!");
            return;
        }

        // Limpiar diccionario y contenido existente
        itemsCreados.Clear();
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        if (debugAndroid) Debug.Log($"[ScrollViewLoader] Contenido limpiado. Cargando {gameObjectManager.listaObjetos.Count} objetos...");

        CargarSprites();
        cargaInicial = true;
    }

    void CargarSprites()
    {
        if (gameObjectManager.listaObjetos == null)
        {
            Debug.LogWarning("[ScrollViewLoader] Lista de objetos es null!");
            return;
        }

        int objetosCargados = 0;

        foreach (var data in gameObjectManager.ObtenerObjetosDelCuentoActual())
        {
            if (data == null) continue;

            CrearNuevoItem(data);
            objetosCargados++;
        }

        if (debugAndroid) Debug.Log($"[ScrollViewLoader] Carga completada. {objetosCargados} objetos cargados en el ScrollView.");
        
        // Forzar actualización del layout
        StartCoroutine(ActualizarLayoutCoroutine());
    }

    private IEnumerator ActualizarLayoutCoroutine()
    {
        yield return new WaitForEndOfFrame();
        
        // Forzar reconstrucción del layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
        
        if (debugAndroid) Debug.Log("[ScrollViewLoader] Layout actualizado.");
    }

    // 👈 NUEVO: Método público para actualizar un item específico por ID
    public void ActualizarItemPorID(string objetoID)
    {
        if (debugAndroid) Debug.Log($"[ScrollViewLoader] ActualizarItemPorID llamado para: {objetoID}");
        
        if (gameObjectManager == null) return;

        GameObjectData data = gameObjectManager.BuscarObjetoPorId(objetoID);
        if (data != null && itemsCreados.ContainsKey(objetoID))
        {
            GameObject item = itemsCreados[objetoID];
            if (item != null)
            {
                ActualizarItem(item, data);
                if (debugAndroid) Debug.Log($"[ScrollViewLoader] Item {objetoID} actualizado directamente.");
            }
        }
    }

    // 👈 NUEVO: Limpiar items huérfanos del diccionario
    private void LimpiarItemsHuerfanos()
    {
        List<string> keysToRemove = new List<string>();
        
        foreach (var kvp in itemsCreados)
        {
            if (kvp.Value == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (string key in keysToRemove)
        {
            itemsCreados.Remove(key);
        }
    }

    void OnDestroy()
    {
        if (gameObjectManager != null)
        {
            gameObjectManager.OnDatosCargados -= HandleDatosCargados;
        }
    }

    // Métodos públicos para debug
    [ContextMenu("Verificar Estado Android")]
    public void VerificarEstadoAndroid()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            Debug.Log($"[ScrollViewLoader Debug] GameObjectManager: {(gameObjectManager != null ? "Encontrado" : "NULL")}");
            Debug.Log($"[ScrollViewLoader Debug] Content: {(content != null ? "Encontrado" : "NULL")}");
            Debug.Log($"[ScrollViewLoader Debug] ItemPrefab: {(itemPrefab != null ? "Encontrado" : "NULL")}");
            Debug.Log($"[ScrollViewLoader Debug] Carga inicial: {cargaInicial}");
            Debug.Log($"[ScrollViewLoader Debug] Items creados: {itemsCreados.Count}");
            
            if (gameObjectManager != null)
            {
                Debug.Log($"[ScrollViewLoader Debug] Datos cargados: {gameObjectManager.datosCargados}");
                Debug.Log($"[ScrollViewLoader Debug] Total objetos: {gameObjectManager.listaObjetos?.Count ?? 0}");
            }
            
            if (content != null)
            {
                Debug.Log($"[ScrollViewLoader Debug] Items en content: {content.childCount}");
            }
        }
    }

    [ContextMenu("Forzar Recarga")]
    public void ForzarRecarga()
    {
        cargaInicial = false;
        RefrescarSprites();
    }

    [ContextMenu("Actualizar Items Existentes")]
    public void ForzarActualizacionItems()
    {
        ActualizarItemsExistentes();
    }
}