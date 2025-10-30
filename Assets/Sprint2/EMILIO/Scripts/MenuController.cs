using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    // Arrastra aquí el botón desde el Inspector
    public Button botonIrTrailer;
    public Button botonSalir;
    public Button botonPlay;


    void Start()
    {
        // Añadir el listener al botón
        if (botonIrTrailer != null)
        {
            botonIrTrailer.onClick.AddListener(IrTrailer);
        }
        // Añadir el listener al botón
        if (botonPlay != null)
        {
            botonPlay.onClick.AddListener(IrJuego);
        }
        // Añadir el listener al botón
        if (botonSalir != null)
        {
            botonSalir.onClick.AddListener(SalirApp);
        }
        else
        {
            Debug.LogError("No se ha asignado ningún botón en el Inspector");
        }
    }
    public void IrTrailer()
    {
        // Cargar la escena especificada
        SceneManager.LoadScene("Trailer");
    }

    public void IrJuego()
    {
        // Cargar la escena especificada
        SceneManager.LoadScene("JuegoAR");
    }

    public void SalirApp()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
