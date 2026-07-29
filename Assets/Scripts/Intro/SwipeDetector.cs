using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Detecta swipes horizontales y los reenvía al SelectorDeCuentos.
/// Poner en un panel UI con Image (Raycast Target activo).
/// </summary>
public class SwipeDetector : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private SelectorDeCuentos selector;
    [SerializeField] private float umbralSwipe = 80f;

    private Vector2 posicionInicial;

    public void OnBeginDrag(PointerEventData eventData)
    {
        posicionInicial = eventData.position;
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (selector == null) return;

        float deltaX = eventData.position.x - posicionInicial.x;
        if (Mathf.Abs(deltaX) < umbralSwipe) return;

        if (deltaX < 0)
            selector.PasarAdelante();
        else
            selector.PasarAtras();
    }
}
