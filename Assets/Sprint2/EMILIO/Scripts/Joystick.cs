using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    private RectTransform wheelBackground;
    private RectTransform wheelStick;

    public float moveRadius = 50f;
    public float returnSpeed = 5f; // Velocidad de retorno al centro

    private float horizontalInput;
    private bool isDragging = false;

    void Start()
    {
        wheelBackground = GetComponent<RectTransform>();
        wheelStick = transform.GetChild(0).GetComponent<RectTransform>();
    }

    void Update()
    {
        // Si no está arrastrando, vuelve suavemente al centro
        if (!isDragging)
        {
            horizontalInput = Mathf.Lerp(horizontalInput, 0, returnSpeed * Time.deltaTime);
            wheelStick.anchoredPosition = new Vector2(horizontalInput * moveRadius, 0);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        isDragging = true;
        Vector2 position;

        // Calcula la posición relativa del toque
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            wheelBackground,
            eventData.position,
            eventData.pressEventCamera,
            out position))
        {
            // Solo movimiento horizontal (como un volante)
            position.x = (position.x / wheelBackground.sizeDelta.x);

            // Normaliza y limita
            horizontalInput = position.x * 2;
            horizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f);

            // Mueve el stick solo horizontalmente
            wheelStick.anchoredPosition = new Vector2(
                horizontalInput * moveRadius,
                0
            );
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    public float GetSteeringInput()
    {
        return horizontalInput;
    }
}