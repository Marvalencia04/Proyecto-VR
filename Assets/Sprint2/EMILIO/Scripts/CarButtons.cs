using UnityEngine;
using UnityEngine.EventSystems;

public class CarButtons : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonType
    {
        Accelerator,
        Brake
    }

    public ButtonType buttonType;

    private bool isPressed = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    public bool IsPressed()
    {
        return isPressed;
    }

    // Para que funcione también cuando el dedo sale del botón
    void OnDisable()
    {
        isPressed = false;
    }
}