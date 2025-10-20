using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Unity.Cinemachine;

public class CasinoStoryboardSequence : MonoBehaviour
{
    [Header("=== VIRTUAL CAMERAS ===")]
    [Tooltip("Cámara dentro del coche (primera persona) - sigue al coche")]
    public CinemachineCamera vcamInteriorCoche;

    [Tooltip("Cámara tercera persona siguiendo el coche")]
    public CinemachineCamera vcamTerceraPersonaCoche;

    [Tooltip("Cámara primera persona caminando hacia puertas")]
    public CinemachineCamera vcamAcercamientoPuertas;

    [Tooltip("Cámara mirando hacia arriba la fachada")]
    public CinemachineCamera vcamFachadaCasino;

    [Header("=== OBJETOS DE ESCENA ===")]
    public Transform coche;
    public Transform puntoPuertaCoche;
    public Transform puntoFrentePuertas;
    public Transform logoFachada;
    public GameObject canvasLogoJuego; // Canvas completo con el logo


    [Header("=== EFECTOS VISUALES ===")]
    public Image pantallaFlash; // Image UI blanco para el flashazo

    [Header("=== CONFIGURACIÓN DE MOVIMIENTO ===")]
    public float velocidadCoche = 10f;
    public float velocidadCaminando = 2f;

    [Header("=== DURACIONES ===")]
    [Tooltip("Duración en vcamInteriorCoche (primera vez)")]
    public float duracionInterior1 = 3f;

    [Tooltip("Duración en vcamTerceraPersonaCoche")]
    public float duracionTerceraPersona = 4f;

    [Tooltip("Duración en vcamInteriorCoche (segunda vez)")]
    public float duracionInterior2 = 3f;

    [Tooltip("Duración caminando hacia las puertas")]
    public float duracionCaminandoPuertas = 4f;

    [Tooltip("Duración de la inclinación hacia arriba")]
    public float duracionInclinacionArriba = 3f;

    [Tooltip("Tiempo mirando el logo antes del flash")]
    public float duracionMirandoLogo = 2f;

    [Tooltip("Duración del flashazo blanco")]
    public float duracionFlash = 0.5f;

    [Tooltip("Duración del logo del juego visible")]
    public float duracionLogoVisible = 3f;

    [Header("=== BLEND TIMES ===")]
    public float tiempoBlendCamaras = 1f;

    [Header("=== AUDIO ===")]

    [Tooltip("AudioSource para la música de fondo")]
    public AudioSource audioMusica;

    [Tooltip("AudioSource para el sonido del motor del coche")]
    public AudioSource audioMotorCoche;

    [Tooltip("Clip de audio para cerrar la puerta del coche")]
    public AudioClip audioPuertaCoche;

    [Tooltip("AudioSource para los pasos de la persona caminando")]
    public AudioSource audioPasos;

    [Tooltip("AudioSource para efectos únicos (puerta)")]
    public AudioSource audioEfectos;

    private Vector3 posicionInicialCoche;
    private Quaternion rotacionInicialCoche;
    private float duracionTotalViajeCoche;

    [SerializeField] private Transform Rueda1;
    [SerializeField] private Transform Rueda2;
    [SerializeField] private float velocidadRotacion = 50f;


    void Start()
    {
        // Configurar las cámaras que siguen al coche
        ConfigurarCamarasSiguenCoche();

        // Guardar posición inicial
        posicionInicialCoche = coche.position;
        rotacionInicialCoche = coche.rotation;
        

        // Calcular duración total del viaje en coche
        duracionTotalViajeCoche = duracionInterior1 + duracionTerceraPersona + duracionInterior2;

        // Ocultar elementos UI
        if (pantallaFlash != null)
            pantallaFlash.color = new Color(1, 1, 1, 0);

        if (canvasLogoJuego != null)
            canvasLogoJuego.SetActive(false);


        // Preparar audio
        PrepararAudio();

        // Iniciar secuencia
        StartCoroutine(SecuenciaCompleta());
    }

    void Update()
    {
        if (Rueda1 != null && Rueda2 != null)
        {
            Rueda1.Rotate(0f, 0f, velocidadRotacion * Time.deltaTime);
            Rueda2.Rotate(0f, 0f, velocidadRotacion * Time.deltaTime);
        }
    }

    void ConfigurarCamarasSiguenCoche()
    {
        // vcamInteriorCoche sigue al coche
        vcamInteriorCoche.Follow = coche;
        //vcamInteriorCoche.LookAt = null; // Mira hacia adelante según orientación del coche

        // vcamTerceraPersonaCoche sigue y mira al coche
        vcamTerceraPersonaCoche.Follow = coche;
        vcamTerceraPersonaCoche.LookAt = coche;
    }

    IEnumerator SecuenciaCompleta()
    {
        // ========== FASE 1: Viaje en coche con cambios de cámara ==========

        // Iniciar sonido del motor
        IniciarMotorCoche();

        // Iniciar movimiento del coche en paralelo
        StartCoroutine(MoverCoche(puntoPuertaCoche.position, duracionTotalViajeCoche));

        // Primera cámara: Interior del coche
        Debug.Log("Cámara 1: Interior del coche (primera vez)");
        ActivarCamara(vcamInteriorCoche);
        yield return new WaitForSeconds(duracionInterior1);

        // Segunda cámara: Tercera persona
        Debug.Log("Cámara 2: Tercera persona");
        ActivarCamara(vcamTerceraPersonaCoche);
        yield return new WaitForSeconds(duracionTerceraPersona);

        // Tercera cámara: Interior del coche de nuevo
        Debug.Log("Cámara 3: Interior del coche (segunda vez)");
        ActivarCamara(vcamInteriorCoche);
        yield return new WaitForSeconds(duracionInterior2);

        // Esperar a que el coche termine de llegar (por si acaso)
        yield return new WaitForSeconds(0.5f);

        // Detener motor del coche
        DetenerMotorCoche();

        // ========== FASE 2: Cambio a primera persona caminando ==========
        Debug.Log("Cámara 4: Caminando hacia las puertas");

        // Sonido de puerta del coche cerrándose
        ReproducirPuertaCoche();

        // Copiar posición y rotación de la cámara anterior (vcamInteriorCoche)
        vcamAcercamientoPuertas.transform.position = vcamInteriorCoche.transform.position;
        vcamAcercamientoPuertas.transform.rotation = vcamInteriorCoche.transform.rotation;

        ActivarCamara(vcamAcercamientoPuertas);
        yield return new WaitForSeconds(tiempoBlendCamaras);

        // Girar suavemente hacia las puertas
        yield return GirarHaciaPuertas(vcamAcercamientoPuertas.transform, 1f);

        // Iniciar sonido de pasos
        IniciarPasos();

        // Caminar hacia las puertas
        yield return MoverCaminando(vcamAcercamientoPuertas.transform, puntoFrentePuertas.position, duracionCaminandoPuertas);

        // Detener sonido de pasos
        DetenerPasos();

        // ========== FASE 3: Inclinación hacia arriba para ver logo ==========
        Debug.Log("Cámara 5: Inclinando hacia arriba para ver logo");

        // Posicionar vcamFachadaCasino donde terminó vcamAcercamientoPuertas
        vcamFachadaCasino.transform.position = vcamAcercamientoPuertas.transform.position;
        vcamFachadaCasino.transform.rotation = vcamAcercamientoPuertas.transform.rotation;

        ActivarCamara(vcamFachadaCasino);
        yield return new WaitForSeconds(tiempoBlendCamaras * 0.5f); // Blend más rápido

        // Inclinar hacia arriba mirando al logo
        yield return InclinarHaciaLogo(duracionInclinacionArriba);

        // Mantener mirando el logo unos segundos
        yield return new WaitForSeconds(duracionMirandoLogo);

        // ========== FASE 4: Flashazo blanco ==========
        Debug.Log("Efecto: Flashazo blanco");
       yield return FlashazoBlanco(duracionFlash);

        // ========== FASE 5: Mostrar logo del juego ==========
        Debug.Log("Mostrando logo del juego");
        if (canvasLogoJuego != null)
        {
            canvasLogoJuego.SetActive(true);
            yield return FadeInCanvas(canvasLogoJuego, 1f);
            yield return new WaitForSeconds(duracionLogoVisible);
        }

        Debug.Log("¡Secuencia completada!");
    }

    void ActivarCamara(CinemachineCamera camara)
    {
        // Desactivar todas
        vcamInteriorCoche.Priority = 0;
        vcamTerceraPersonaCoche.Priority = 0;
        vcamAcercamientoPuertas.Priority = 0;
        vcamFachadaCasino.Priority = 0;

        // Activar la deseada
        camara.Priority = 10;
    }

    IEnumerator MoverCoche(Vector3 destino, float duracion)
    {
        Vector3 inicio = coche.position;
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / duracion;

            // Usar curva de animación suave
            t = Mathf.SmoothStep(0, 1, t);

            coche.position = Vector3.Lerp(inicio, destino, t);

            // Hacer que el coche mire en la dirección del movimiento
            /*Vector3 direccion = destino - inicio;
            if (direccion != Vector3.zero)
            {
                coche.rotation = Quaternion.LookRotation(direccion);
            }*/

            yield return null;
        }

        coche.position = destino;
    }

    // ========== FUNCIONES DE AUDIO ==========

    void PrepararAudio()
    {
        // Iniciar música de fondo
        if (audioMusica != null)
        {
            audioMusica.loop = true;
            audioMusica.Play();
            Debug.Log("Música de fondo iniciada");
        }

        // Asegurar que todos los audios estén detenidos al inicio
        if (audioMotorCoche != null)
        {
            audioMotorCoche.loop = true;
            audioMotorCoche.Stop();
        }

        if (audioPasos != null)
        {
            audioPasos.loop = true;
            audioPasos.Stop();
        }

        if (audioEfectos != null)
        {
            audioEfectos.loop = false;
            audioEfectos.Stop();
        }
    }

    void IniciarMotorCoche()
    {
        if (audioMotorCoche != null && !audioMotorCoche.isPlaying)
        {
            audioMotorCoche.Play();
            Debug.Log("Motor del coche iniciado");
        }
    }

    void DetenerMotorCoche()
    {
        if (audioMotorCoche != null && audioMotorCoche.isPlaying)
        {
            // Fade out suave
            StartCoroutine(FadeOutAudio(audioMotorCoche, 0.5f));
            Debug.Log("Motor del coche detenido");
        }
    }

    void ReproducirPuertaCoche()
    {
        if (audioEfectos != null && audioPuertaCoche != null)
        {
            StartCoroutine(ReproducirPuertaConDelay(1f));
        }
    }

    IEnumerator ReproducirPuertaConDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        audioEfectos.PlayOneShot(audioPuertaCoche);
        Debug.Log("Puerta del coche cerrada");
    }

    void IniciarPasos()
    {
        if (audioPasos != null && !audioPasos.isPlaying)
        {
            audioPasos.Play();
            Debug.Log("Pasos iniciados");
        }
    }

    void DetenerPasos()
    {
        if (audioPasos != null && audioPasos.isPlaying)
        {
            // Fade out suave
            StartCoroutine(FadeOutAudio(audioPasos, 0.3f));
            Debug.Log("Pasos detenidos");
        }
    }

    IEnumerator FadeOutAudio(AudioSource audio, float duracion)
    {
        float volumenInicial = audio.volume;
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            audio.volume = Mathf.Lerp(volumenInicial, 0f, tiempoTranscurrido / duracion);
            yield return null;
        }

        audio.Stop();
        audio.volume = volumenInicial; // Restaurar volumen para próxima vez
    }


    IEnumerator GirarHaciaPuertas(Transform camara, float duracion)
    {
        Quaternion rotacionInicial = camara.rotation;
        Vector3 direccionPuertas = (puntoFrentePuertas.position - camara.position).normalized;
        Quaternion rotacionFinal = Quaternion.LookRotation(direccionPuertas);

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / duracion;
            t = Mathf.SmoothStep(0, 1, t);

            camara.rotation = Quaternion.Lerp(rotacionInicial, rotacionFinal, t);
            yield return null;
        }

        camara.rotation = rotacionFinal;
    }

    IEnumerator MoverCaminando(Transform camara, Vector3 destino, float duracion)
    {
        Vector3 inicio = camara.position;
        Vector3 destinoAjustado = new Vector3(destino.x, camara.position.y, destino.z); // Mantener altura
        Quaternion rotacionFija = camara.rotation; // Mantener la rotación fija durante el movimiento

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / duracion;
            t = Mathf.SmoothStep(0, 1, t);

            // Mover la cámara
            camara.position = Vector3.Lerp(inicio, destinoAjustado, t);

            // Mantener la rotación fija (sin girar)
            camara.rotation = rotacionFija;

            yield return null;
        }

        camara.position = destinoAjustado;
    }

    IEnumerator InclinarHaciaLogo(float duracion)
    {
        Transform cam = vcamFachadaCasino.transform;
        Quaternion rotacionInicial = cam.rotation;

        // Calcular rotación para mirar al logo
        Vector3 direccionAlLogo = (logoFachada.position - cam.position).normalized;
        Quaternion rotacionFinal = Quaternion.LookRotation(direccionAlLogo);

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / duracion;
            t = Mathf.SmoothStep(0, 1, t);

            cam.rotation = Quaternion.Lerp(rotacionInicial, rotacionFinal, t);
            yield return null;
        }

        cam.rotation = rotacionFinal;
    }

    IEnumerator FlashazoBlanco(float duracion)
    {
        if (pantallaFlash == null) yield break;

        // Fade in rápido a blanco
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, t / duracion);
            pantallaFlash.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        pantallaFlash.color = Color.white;
    }

    IEnumerator FadeInCanvas(GameObject canvas, float duracion)
    {
        CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = canvas.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        float t = 0f;

        while (t < duracion)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, t / duracion);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    // Método para reiniciar la secuencia desde el editor
    [ContextMenu("Reiniciar Secuencia")]
    public void ReiniciarSecuencia()
    {
        StopAllCoroutines();

        coche.position = posicionInicialCoche;
        coche.rotation = rotacionInicialCoche;

        if (pantallaFlash != null)
            pantallaFlash.color = new Color(1, 1, 1, 0);

        if (canvasLogoJuego != null)
            canvasLogoJuego.SetActive(false);

        StartCoroutine(SecuenciaCompleta());
    }
}