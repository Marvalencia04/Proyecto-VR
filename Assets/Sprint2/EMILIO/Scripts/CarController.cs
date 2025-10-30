using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Referencias")]
    public Joystick steeringWheel;
    public CarButtons acceleratorButton;
    public CarButtons brakeButton;

    [Header("Configuración de Movimiento")]
    public float maxSpeed = 3f;
    public float acceleration = 2f;
    public float brakeForce = 4f;
    public float rotationSpeed = 100f;

    [Header("Límites de Movimiento")]
    public float maxDistanceFromStart = 5f;

    [Header("Habilidades Especiales")]
    public float jumpForce = 5f;
    public float jumpCooldown = 2f;
    public float spinDuration = 1f;
    public float spinCooldown = 3f;
    public float spinMoveSpeed = 2f; // Velocidad de movimiento durante el spin

    private Rigidbody rb;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float currentSpeed = 0f;

    private float lastJumpTime = -999f;
    private float lastSpinTime = -999f;
    private bool isSpinning = false;
    private float spinStartTime;
    private float spinStartRotation;
    private Vector3 spinDirection; // Dirección del movimiento durante el spin

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Asegurarse de que el Rigidbody permite el salto
        if (rb != null)
        {
            rb.useGravity = true;  // Activar gravedad para el salto
        }

        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    void Update()
    {
        HandleAcceleration();
        HandleSteering();
        HandleSpin();
        LimitMovement();

        // Reset con tecla R
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCar();
        }

        // Salto con tecla Space (Espacio)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }

        // Spin con tecla E
        if (Input.GetKeyDown(KeyCode.E))
        {
            Spin360();
        }
    }

    void HandleAcceleration()
    {
        // Verifica input de teclado (W para acelerar, Q para frenar)
        bool isAccelerating = acceleratorButton.IsPressed() || Input.GetKey(KeyCode.W);
        bool isBraking = brakeButton.IsPressed() || Input.GetKey(KeyCode.Q);

        // Aceleración
        if (isAccelerating)
        {
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);
        }
        // Freno
        else if (isBraking)
        {
            currentSpeed -= brakeForce * Time.deltaTime;
            currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed * 0.5f, maxSpeed); // Marcha atrás a la mitad de velocidad
        }
        // Desaceleración natural (fricción)
        else
        {
            if (currentSpeed > 0)
            {
                currentSpeed -= acceleration * 0.5f * Time.deltaTime;
                currentSpeed = Mathf.Max(currentSpeed, 0);
            }
            else if (currentSpeed < 0)
            {
                currentSpeed += acceleration * 0.5f * Time.deltaTime;
                currentSpeed = Mathf.Min(currentSpeed, 0);
            }
        }

        // Aplica el movimiento hacia adelante
        Vector3 movement = transform.forward * currentSpeed * Time.deltaTime;
        transform.position += movement;
    }

    void HandleSteering()
    {
        // No permite girar durante el spin
        if (isSpinning)
        {
            return;
        }

        // Solo gira si el coche se está moviendo
        if (Mathf.Abs(currentSpeed) > 0.1f)
        {
            // Combina input del volante con teclado (A/D)
            float steeringInput = steeringWheel.GetSteeringInput();

            // Añade input de teclado
            if (Input.GetKey(KeyCode.A))
            {
                steeringInput = -1f;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                steeringInput = 1f;
            }

            float rotation = steeringInput * rotationSpeed * Time.deltaTime;

            // Gira más lento cuando va marcha atrás
            if (currentSpeed < 0)
            {
                rotation *= -0.5f;
            }

            transform.Rotate(0, rotation, 0);
        }
    }

    void LimitMovement()
    {
        // Calcula la distancia desde la posición inicial
        float distanceFromStart = Vector3.Distance(transform.localPosition, startPosition);

        if (distanceFromStart > maxDistanceFromStart)
        {
            // Si está muy lejos, lo empuja de vuelta suavemente
            Vector3 directionToStart = (startPosition - transform.localPosition).normalized;
            transform.localPosition += directionToStart * 0.1f;
        }
    }

    void HandleSpin()
    {
        if (!isSpinning)
        {
            return;
        }

        // Calcula el progreso del spin
        float elapsed = Time.time - spinStartTime;
        float progress = elapsed / spinDuration;

        if (progress >= 1f)
        {
            // Spin completado
            isSpinning = false;

            // Asegura que termine exactamente en 360°
            Vector3 currentRotation = transform.localEulerAngles;
            currentRotation.y = spinStartRotation + 360f;
            transform.localEulerAngles = currentRotation;
        }
        else
        {
            // Rota suavemente 360°
            Vector3 currentRotation = transform.localEulerAngles;
            currentRotation.y = spinStartRotation + (360f * progress);
            transform.localEulerAngles = currentRotation;
        }
    }

    // Método público para otros scripts (como velocímetro)
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    // Método para resetear el coche a su posición inicial
    public void ResetCar()
    {
        // Resetea posición y rotación
        transform.localPosition = startPosition;
        transform.localRotation = startRotation;

        // Resetea velocidad
        currentSpeed = 0f;

        // Resetea física del Rigidbody
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Cancela spin si está activo
        isSpinning = false;

        Debug.Log("¡Coche reseteado!");
    }

    // Método para hacer saltar el coche
    public void Jump()
    {
        // Verifica cooldown
        if (Time.time - lastJumpTime < jumpCooldown)
        {
            Debug.Log($"Salto en cooldown. Espera {jumpCooldown - (Time.time - lastJumpTime):F1}s");
            return;
        }

        // Verifica que tenga Rigidbody
        if (rb == null)
        {
            Debug.LogWarning("No se puede saltar sin Rigidbody!");
            return;
        }

        // Aplica fuerza hacia arriba
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        lastJumpTime = Time.time;
        Debug.Log("¡Coche saltando! 🚀");
    }

    // Método para hacer girar 360° el coche
    public void Spin360()
    {
        // Verifica cooldown
        if (Time.time - lastSpinTime < spinCooldown)
        {
            Debug.Log($"Spin en cooldown. Espera {spinCooldown - (Time.time - lastSpinTime):F1}s");
            return;
        }

        // No permite spin si ya está girando
        if (isSpinning)
        {
            return;
        }

        // Inicia el spin
        isSpinning = true;
        spinStartTime = Time.time;
        spinStartRotation = transform.localEulerAngles.y;

        lastSpinTime = Time.time;
        Debug.Log("¡Spin 360°! 🔄");
    }


}