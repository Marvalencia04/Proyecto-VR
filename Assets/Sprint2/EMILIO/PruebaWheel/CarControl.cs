using UnityEngine;

public class CarControl : MonoBehaviour
{
    [Header("Referencias UI")]
    public Joystick steeringWheel;
    public CarButtons acceleratorButton;
    public CarButtons brakeButton;

    [Header("Car Properties")]
    public float motorTorque = 2000f;
    public float brakeTorque = 2000f;
    public float maxSpeed = 20f;
    public float steeringRange = 30f;
    public float steeringRangeAtMaxSpeed = 10f;
    public float centreOfGravityOffset = -1f;

    [Header("Habilidades Especiales")]
    public float jumpForce = 500f;
    public float jumpCooldown = 2f;
    public float spinDuration = 1f;
    public float spinCooldown = 3f;
    public float spinMoveSpeed = 5f;

    [Header("Modo Edicion - Solo Teclado")]
    public bool editMode = false;
    public float scaleSpeed = 0.5f;
    public float rotationSpeed = 50f;
    public float minScale = 0.3f;
    public float maxScale = 3f;

    private WheelControl[] wheels;
    private Rigidbody rigidBody;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private float lastJumpTime = -999f;
    private float lastSpinTime = -999f;
    private bool isSpinning = false;
    private float spinStartTime;
    private float spinStartRotation;
    private Vector3 spinDirection;

    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();

        // Adjust center of mass to improve stability and prevent rolling
        Vector3 centerOfMass = rigidBody.centerOfMass;
        centerOfMass.y += centreOfGravityOffset;
        rigidBody.centerOfMass = centerOfMass;

        // Get all wheel components attached to the car
        wheels = GetComponentsInChildren<WheelControl>();

        // Guarda posicion inicial
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;

        if (wheels.Length == 0)
        {
            Debug.LogWarning("No se encontraron WheelControl en el coche!");
        }
    }

    void Update()
    {
        HandleSpin();
        HandleEditMode();

        // Controles de teclado para habilidades especiales (solo si no esta en modo edicion)
        if (!editMode)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetCar();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                Spin360();
            }
        }
    }

    void FixedUpdate()
    {
        // Si esta en modo edicion o en medio de un spin, no permite control normal
        if (editMode || isSpinning)
        {
            return;
        }

        // Obtiene input del joystick/botones y teclado
        float vInput = GetVerticalInput();
        float hInput = GetHorizontalInput();

        // Calculate current speed along the car's forward axis
        float forwardSpeed = Vector3.Dot(transform.forward, rigidBody.linearVelocity);
        float speedFactor = Mathf.InverseLerp(0, maxSpeed, Mathf.Abs(forwardSpeed));

        // Reduce motor torque and steering at high speeds for better handling
        float currentMotorTorque = Mathf.Lerp(motorTorque, 0, speedFactor);
        float currentSteerRange = Mathf.Lerp(steeringRange, steeringRangeAtMaxSpeed, speedFactor);

        // Determine if the player is accelerating or trying to reverse
        bool isAccelerating = Mathf.Sign(vInput) == Mathf.Sign(forwardSpeed);

        foreach (var wheel in wheels)
        {
            // Apply steering to wheels that support steering
            if (wheel.steerable)
            {
                wheel.WheelCollider.steerAngle = hInput * currentSteerRange;
            }

            if (isAccelerating)
            {
                // Apply torque to motorized wheels
                if (wheel.motorized)
                {
                    wheel.WheelCollider.motorTorque = vInput * currentMotorTorque;
                }
                // Release brakes when accelerating
                wheel.WheelCollider.brakeTorque = 0f;
            }
            else
            {
                // Apply brakes when reversing direction
                wheel.WheelCollider.motorTorque = 0f;
                wheel.WheelCollider.brakeTorque = Mathf.Abs(vInput) * brakeTorque;
            }
        }
    }

    float GetVerticalInput()
    {
        // Prioriza input de teclado, luego botones
        if (Input.GetKey(KeyCode.W))
        {
            return 1f;
        }
        else if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.S))
        {
            return -1f;
        }
        else if (acceleratorButton != null && acceleratorButton.IsPressed())
        {
            return 1f;
        }
        else if (brakeButton != null && brakeButton.IsPressed())
        {
            return -1f;
        }

        return 0f;
    }

    float GetHorizontalInput()
    {
        // Prioriza input de teclado, luego joystick
        if (Input.GetKey(KeyCode.A))
        {
            return -1f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            return 1f;
        }
        else if (steeringWheel != null)
        {
            return steeringWheel.GetSteeringInput();
        }

        return 0f;
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

            // Asegura que termine exactamente en 360 grados
            Vector3 currentRotation = transform.localEulerAngles;
            currentRotation.y = spinStartRotation + 360f;
            transform.localEulerAngles = currentRotation;
        }
        else
        {
            // Rota suavemente 360 grados
            Vector3 currentRotation = transform.localEulerAngles;
            currentRotation.y = spinStartRotation + (360f * progress);
            transform.localEulerAngles = currentRotation;

            // Mueve el coche en la direccion capturada durante el spin
            if (spinDirection.magnitude > 0.1f && rigidBody != null)
            {
                rigidBody.linearVelocity = spinDirection * spinMoveSpeed;
            }
        }
    }

    void HandleEditMode()
    {
        // Toggle modo edicion con Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            editMode = !editMode;

            if (editMode)
            {
                EnableEditMode();
            }
            else
            {
                DisableEditMode();
            }
        }

        // Solo permite edicion si el modo esta activo
        if (!editMode)
        {
            return;
        }

        // ESCALADO con rueda del raton
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            ScaleCar(scroll);
        }

        // ROTACION con teclas U e I
        if (Input.GetKey(KeyCode.U))
        {
            RotateCar(-1f);
        }
        else if (Input.GetKey(KeyCode.I))
        {
            RotateCar(1f);
        }
    }

    void EnableEditMode()
    {
        Debug.Log("MODO EDICION ACTIVADO");
        Debug.Log("- Rueda del raton: Escalar coche");
        Debug.Log("- U/I: Rotar coche");
        Debug.Log("- Tab: Salir del modo edicion");

        // Detiene el coche
        if (rigidBody != null)
        {
            rigidBody.linearVelocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
            rigidBody.isKinematic = true; // Desactiva fisica temporalmente
        }

        // Detiene las ruedas
        if (wheels != null)
        {
            foreach (var wheel in wheels)
            {
                if (wheel.WheelCollider != null)
                {
                    wheel.WheelCollider.motorTorque = 0f;
                    wheel.WheelCollider.brakeTorque = 1000f;
                }
            }
        }
    }

    void DisableEditMode()
    {
        Debug.Log("MODO EDICION DESACTIVADO - Modo conduccion activo");

        // Reactiva fisica
        if (rigidBody != null)
        {
            rigidBody.isKinematic = false;
        }
    }

    void ScaleCar(float scrollDelta)
    {
        // Obtiene la escala actual
        Vector3 currentScale = transform.localScale;

        // Calcula nueva escala
        float scaleChange = scrollDelta * scaleSpeed;
        float newScaleValue = currentScale.x + scaleChange;

        // Limita la escala
        newScaleValue = Mathf.Clamp(newScaleValue, minScale, maxScale);

        // Aplica escala uniforme
        transform.localScale = new Vector3(newScaleValue, newScaleValue, newScaleValue);

        Debug.Log("Escala: " + newScaleValue.ToString("F2") + "x");
    }

    void RotateCar(float direction)
    {
        // Rota el coche alrededor del eje Y (vertical)
        float rotationAmount = direction * rotationSpeed * Time.deltaTime;
        transform.Rotate(0, rotationAmount, 0, Space.World);

        Debug.Log("Rotacion: " + transform.localEulerAngles.y.ToString("F1") + " grados");
    }

    public float GetCurrentSpeed()
    {
        if (rigidBody != null)
        {
            return Vector3.Dot(transform.forward, rigidBody.linearVelocity);
        }
        return 0f;
    }

    public void ResetCar()
    {
        // Resetea posicion y rotacion
        transform.localPosition = startPosition;
        transform.localRotation = startRotation;

        // Resetea fisica del Rigidbody
        if (rigidBody != null)
        {
            rigidBody.linearVelocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
        }

        // Resetea las ruedas
        if (wheels != null)
        {
            foreach (var wheel in wheels)
            {
                if (wheel.WheelCollider != null)
                {
                    wheel.WheelCollider.motorTorque = 0f;
                    wheel.WheelCollider.brakeTorque = 0f;
                    wheel.WheelCollider.steerAngle = 0f;
                }
            }
        }

        // Cancela spin si esta activo
        isSpinning = false;

        Debug.Log("Coche reseteado!");
    }

    public void Jump()
    {
        // Verifica cooldown
        if (Time.time - lastJumpTime < jumpCooldown)
        {
            float remaining = jumpCooldown - (Time.time - lastJumpTime);
            Debug.Log("Salto en cooldown. Espera " + remaining.ToString("F1") + "s");
            return;
        }

        if (rigidBody == null)
        {
            Debug.LogWarning("No se puede saltar sin Rigidbody!");
            return;
        }

        // Aplica fuerza hacia arriba
        rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        lastJumpTime = Time.time;
        Debug.Log("Coche saltando!");
    }

    public void Spin360()
    {
        // Verifica cooldown
        if (Time.time - lastSpinTime < spinCooldown)
        {
            float remaining = spinCooldown - (Time.time - lastSpinTime);
            Debug.Log("Spin en cooldown. Espera " + remaining.ToString("F1") + "s");
            return;
        }

        if (isSpinning)
        {
            return;
        }

        // Captura la direccion actual del movimiento
        if (rigidBody != null)
        {
            spinDirection = rigidBody.linearVelocity.normalized;

            // Si no se esta moviendo, captura el input del joystick/teclado
            if (spinDirection.magnitude < 0.1f)
            {
                float vInput = GetVerticalInput();
                float hInput = GetHorizontalInput();

                Vector3 forward = transform.forward * vInput;
                Vector3 right = transform.right * hInput;
                spinDirection = (forward + right).normalized;
            }
        }

        // Inicia el spin
        isSpinning = true;
        spinStartTime = Time.time;
        spinStartRotation = transform.localEulerAngles.y;

        lastSpinTime = Time.time;

        if (spinDirection.magnitude > 0.1f)
        {
            Debug.Log("Spin 360 grados con movimiento!");
        }
        else
        {
            Debug.Log("Spin 360 grados!");
        }
    }

    // Para debugging
    void OnGUI()
    {
        // Modo edicion con fondo destacado
        if (editMode)
        {
            GUI.backgroundColor = Color.yellow;
            GUI.Box(new Rect(10, 10, 320, 200), "");
            GUI.backgroundColor = Color.white;

            GUI.Label(new Rect(20, 20, 300, 30), "MODO EDICION ACTIVO");
            GUI.Label(new Rect(20, 45, 300, 20), "----------------------------");
            GUI.Label(new Rect(20, 65, 300, 20), "Rueda Raton: Escalar");
            GUI.Label(new Rect(20, 85, 300, 20), "U: Rotar Izquierda");
            GUI.Label(new Rect(20, 105, 300, 20), "I: Rotar Derecha");
            GUI.Label(new Rect(20, 125, 300, 20), "Tab: Salir");
            GUI.Label(new Rect(20, 145, 300, 20), "----------------------------");
            GUI.Label(new Rect(20, 165, 300, 20), "Escala: " + transform.localScale.x.ToString("F2") + "x");
            GUI.Label(new Rect(20, 185, 300, 20), "Rotacion: " + transform.localEulerAngles.y.ToString("F1") + " grados");
        }
        else
        {
            float speed = GetCurrentSpeed();
            GUI.Label(new Rect(10, 10, 300, 20), "Velocidad: " + speed.ToString("F2") + " m/s");
            GUI.Label(new Rect(10, 30, 300, 20), "Velocidad: " + (speed * 3.6f).ToString("F0") + " km/h");

            if (steeringWheel != null)
            {
                GUI.Label(new Rect(10, 50, 300, 20), "Direccion: " + steeringWheel.GetSteeringInput().ToString("F2"));
            }

            GUI.Label(new Rect(10, 70, 300, 20), "Controles PC:");
            GUI.Label(new Rect(10, 90, 300, 20), "W = Acelerar | Q/S = Frenar");
            GUI.Label(new Rect(10, 110, 300, 20), "A = Izquierda | D = Derecha");
            GUI.Label(new Rect(10, 130, 300, 20), "R = Reset | Space = Salto | E = Spin");
            GUI.Label(new Rect(10, 150, 300, 20), "Tab = Modo Edicion");

            // Muestra cooldowns
            float jumpCooldownRemaining = jumpCooldown - (Time.time - lastJumpTime);
            float spinCooldownRemaining = spinCooldown - (Time.time - lastSpinTime);

            if (jumpCooldownRemaining > 0)
            {
                GUI.Label(new Rect(10, 170, 300, 20), "Salto: " + jumpCooldownRemaining.ToString("F1") + "s");
            }

            if (spinCooldownRemaining > 0)
            {
                GUI.Label(new Rect(10, 190, 300, 20), "Spin: " + spinCooldownRemaining.ToString("F1") + "s");
            }

            if (isSpinning)
            {
                GUI.Label(new Rect(10, 210, 300, 20), "SPINNING!");
            }
        }
    }

    void OnDrawGizmos()
    {
        // Dibuja direccion del spin
        if (isSpinning && spinDirection.magnitude > 0.1f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + spinDirection * 3f);
            Gizmos.DrawSphere(transform.position + spinDirection * 3f, 0.2f);
        }

        // Dibuja indicador del modo edicion
        if (editMode)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, Vector3.one * 0.3f);
        }
    }
}