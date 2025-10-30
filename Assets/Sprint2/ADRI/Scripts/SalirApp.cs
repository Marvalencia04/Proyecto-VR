using UnityEngine;
using UnityEngine.SceneManagement;

public class SalirApp : MonoBehaviour
{
    public void Salir() => Application.Quit();

    public void IrTrailer()
    {
        // Cargar la escena especificada
        SceneManager.LoadScene("Trailer");
    }

    public void IrJuego()
    {
        // Cargar la escena especificada
        SceneManager.LoadScene("JuegoARFinal");
    }
}
