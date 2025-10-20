using UnityEngine;
using System.Collections;

public class CameraPathController : MonoBehaviour
{
    [Header("Objetos a controlar")]
    public Transform camara;
    public Transform objetoA;
    public Transform objetoB;

    [Header("Puntos de destino")]
    public Transform puntoX;
    public Transform puntoY;
    public Transform puntoZ;

    [Header("Configuración de movimiento")]
    public float velocidadMovimiento = 5f;
    public float velocidadRotacion = 90f; // grados por segundo
    public float duracionPaneo = 2f;
    public float duracionInclinacion = 3f;
    public float toleranciaDistancia = 0.1f;

    private bool moviendose = false;

    void Start()
    {
        // Iniciar la secuencia automáticamente
        StartCoroutine(SecuenciaCamara());
    }

    IEnumerator SecuenciaCamara()
    {
        moviendose = true;

        // === FASE 1: Avanzar hasta punto X ===
        Debug.Log("Fase 1: Avanzando hacia punto X");
        yield return MoverHacia(puntoX.position);

        // === FASE 2: Girar 90º a la derecha (cámara y objeto A) ===
        Debug.Log("Fase 2: Girando 90º a la derecha");
        yield return GirarObjetos(90f, camara, objetoA);

        // === FASE 3: Avanzar hasta punto Y ===
        Debug.Log("Fase 3: Avanzando hacia punto Y");
        yield return MoverHacia2(puntoY.position);

        // === FASE 4: Paneo izquierda y derecha (45º cada lado) ===
        Debug.Log("Fase 4: Paneo izquierda-derecha");
        yield return RealizarPaneo(45f);

        // === FASE 5: Inclinar 45º hacia arriba ===
        Debug.Log("Fase 5: Inclinando hacia arriba");
        yield return InclinarCamara(-45f, duracionInclinacion);

        // === FASE 6: Volver a posición original de rotación ===
        Debug.Log("Fase 6: Volviendo a posición original");
        yield return InclinarCamara(45f, duracionInclinacion);

        // === FASE 7: Avanzar hasta punto Z ===
        Debug.Log("Fase 7: Avanzando hacia punto Z");
        yield return MoverHacia2(puntoZ.position);

        Debug.Log("Secuencia completada");
        moviendose = false;
    }

    IEnumerator MoverHacia(Vector3 destino)
    {
        Vector3 destinoPlano = new Vector3(destino.x, camara.position.y, destino.z);

        while (Vector3.Distance(camara.position, destinoPlano) > toleranciaDistancia)
        {
            Vector3 direccion = (destinoPlano - camara.position).normalized;

            // Mover cámara, objetoA y objetoB
            camara.position += direccion * velocidadMovimiento * Time.deltaTime;
            objetoA.position += direccion * velocidadMovimiento * Time.deltaTime;
            objetoB.position += direccion * velocidadMovimiento * Time.deltaTime;

            yield return null;
        }
    }

    IEnumerator MoverHacia2(Vector3 destino)
    {
        Vector3 destinoPlano = new Vector3(destino.x, camara.position.y, destino.z);

        while (Vector3.Distance(camara.position, destinoPlano) > toleranciaDistancia)
        {
            Vector3 direccion = (destinoPlano - camara.position).normalized;

            // Mover cámara, objetoA y objetoB
            camara.position += direccion * velocidadMovimiento * Time.deltaTime;
            objetoA.position += direccion * velocidadMovimiento * Time.deltaTime;
            

            yield return null;
        }
    }


    IEnumerator GirarObjetos(float grados, params Transform[] objetos)
    {
        float rotacionTotal = 0f;

        while (rotacionTotal < grados)
        {
            float rotacionFrame = velocidadRotacion * Time.deltaTime;
            if (rotacionTotal + rotacionFrame > grados)
                rotacionFrame = grados - rotacionTotal;

            foreach (Transform obj in objetos)
            {
                obj.Rotate(0, rotacionFrame, 0);
            }

            rotacionTotal += rotacionFrame;
            yield return null;
        }
    }

    IEnumerator RealizarPaneo(float anguloMaximo)
    {
        // Guardar rotación inicial
        Quaternion rotacionInicial = camara.rotation;

        // Paneo a la izquierda
        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < duracionPaneo)
        {
            float angulo = Mathf.Lerp(0, -anguloMaximo, tiempoTranscurrido / duracionPaneo);
            camara.rotation = rotacionInicial * Quaternion.Euler(0, angulo, 0);
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        // Paneo a la derecha
        tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < duracionPaneo * 2)
        {
            float angulo = Mathf.Lerp(-anguloMaximo, anguloMaximo, tiempoTranscurrido / (duracionPaneo * 2));
            camara.rotation = rotacionInicial * Quaternion.Euler(0, angulo, 0);
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        // Volver al centro
        tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < duracionPaneo)
        {
            float angulo = Mathf.Lerp(anguloMaximo, 0, tiempoTranscurrido / duracionPaneo);
            camara.rotation = rotacionInicial * Quaternion.Euler(0, angulo, 0);
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        camara.rotation = rotacionInicial;
    }

    IEnumerator InclinarCamara(float angulo, float duracion)
    {
        Quaternion rotacionInicial = camara.rotation;
        Quaternion rotacionFinal = rotacionInicial * Quaternion.Euler(angulo, 0, 0);

        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < duracion)
        {
            camara.rotation = Quaternion.Lerp(rotacionInicial, rotacionFinal, tiempoTranscurrido / duracion);
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        camara.rotation = rotacionFinal;
    }

    // Método para reiniciar la secuencia
    public void ReiniciarSecuencia()
    {
        if (!moviendose)
        {
            StartCoroutine(SecuenciaCamara());
        }
    }
}

