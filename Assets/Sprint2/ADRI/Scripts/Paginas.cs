using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Paginas : MonoBehaviour
{
    public TMP_Text Titulo;
    public TMP_Text Cuerpo;
    public Image Imagen;

    public void Set(string titulo, string cuerpo, Sprite imagen = null)
    {
        if (Titulo) Titulo.text = titulo;
        if (Cuerpo) Cuerpo.text = cuerpo;

        if (Imagen)
        {
            Imagen.sprite = imagen;
            Imagen.enabled = imagen != null;
            if (imagen == null) Imagen.color = Color.clear;
            else Imagen.color = Color.white;
        }
    }
}
