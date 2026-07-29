using UnityEngine;
using UnityEngine.EventSystems;

public class DropAreaLista : MonoBehaviour, IDropHandler
{
    [Tooltip("El contenedor donde van los ítems (normalmente el Content del ScrollView)")]
    public Transform content;

    private RectTransform viewportRect;

    void Awake()
    {
        if (content == null)
            content = transform; // Si no asignas nada, usa este mismo

        // Buscar el Viewport del ScrollView para usar sus límites visibles
        var scrollRect = GetComponentInParent<UnityEngine.UI.ScrollRect>();
        if (scrollRect != null)
            viewportRect = scrollRect.viewport;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var item = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<DraggableItem>() : null;
        if (item == null || !item.esArrastrable) return;

        // Verificar que el drop ocurrió dentro del área visible del ScrollView
        if (viewportRect != null)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewportRect, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                if (!viewportRect.rect.Contains(localPoint))
                {
                    // Drop fuera del área visible → no hacer nada
                    return;
                }
            }
        }

        // Colocar el objeto en la lista
        item.transform.SetParent(content, true);

        // Actualizar su posición de referencia
        item.startParent = content;
        item.startPosition = item.transform.position;

        Debug.Log($"Objeto {item.objetoID} devuelto a la lista.");
    }
}
