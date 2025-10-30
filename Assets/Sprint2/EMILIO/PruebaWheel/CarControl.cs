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
    [Tooltip("Si esta activado, el spin recorrera exactamente esta distancia (m) en el tiempo de spin.")]
    public bool useSpinDistance = true;
    [Tooltip("Distancia total que recorre el coche durante el spin (en metros).")]
    public float spinTravelDistance = 1f;

    [Header("Modo Edicion - Siempre Activo en Movil")]
    public float scaleSpeed = 0.01f;
    public float rotationSpeed = 2f;
    public float minScale = 0.3f;
    public float maxScale = 3f;
    [Tooltip("Distancia minima para detectar rotacion con un dedo (en pixeles)")]
    public float rotationDragThreshold = 20f;

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
    private float spinMoveSpeedRuntime;

    // Variables para gestos tactiles
    private float initialPinchDistance = 0f;
    private bool isPinching = false;
    private Vector2 lastSingleTouchPosition;
    private bool isDraggingCar = false;
    private float rotationStartY = 0f;

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
        HandleMobileEditGestures();

        // Controles de teclado (solo para PC)
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

        // Modo edicion con teclado (solo para PC)
        HandleKeyboardEdit();
    }

    void FixedUpdate()
    {
        // Si esta en medio de un spin, no permite control normal
        if (isSpinning)
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

    void HandleMobileEditGestures()
    {
        // Solo procesa gestos tactiles en dispositivos moviles
        if (Input.touchCount == 0)
        {
            isPinching = false;
            isDraggingCar = false;
            return;
        }

        // PINCH ZOOM - Dos dedos para escalar
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            // Cancela drag si habia uno activo
            isDraggingCar = false;

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                // Inicia pinch
                initialPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                isPinching = true;
            }
            else if ((touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved) && isPinching)
            {
                // Calcula nueva distancia
                float currentPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                float pinchDelta = currentPinchDistance - initialPinchDistance;

                // Escala el coche
                ScaleCarMobile(pinchDelta * scaleSpeed);

                // Actualiza distancia inicial
                initialPinchDistance = currentPinchDistance;
            }
        }
        // SINGLE TOUCH - Un dedo para rotar (si toca sobre el coche)
        else if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            // Cancela pinch si habia uno activo
            isPinching = false;

            if (touch.phase == TouchPhase.Began)
            {
                // Verifica si el toque esta sobre el coche
                if (IsTouchOverCar(touch.position))
                {
                    isDraggingCar = true;
                    lastSingleTouchPosition = touch.position;
                    rotationStartY = transform.localEulerAngles.y;
                }
            }
            else if (touch.phase == TouchPhase.Moved && isDraggingCar)
            {
                // Calcula movimiento horizontal
                float deltaX = touch.position.x - lastSingleTouchPosition.x;

                // Solo rota si el movimiento es significativo
                if (Mathf.Abs(deltaX) > rotationDragThreshold * Time.deltaTime)
                {
                    RotateCarMobile(deltaX * rotationSpeed * Time.deltaTime);
                }

                lastSingleTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDraggingCar = false;
            }
        }
    }

    void HandleKeyboardEdit()
    {
        // Escalado con rueda del raton (PC)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            ScaleCar(scroll);
        }

        // Rotacion con teclas U e I (PC)
        if (Input.GetKey(KeyCode.U))
        {
            RotateCar(-1f);
        }
        else if (Input.GetKey(KeyCode.I))
        {
            RotateCar(1f);
        }
    }

    bool IsTouchOverCar(Vector2 touchPosition)
    {
        // Convierte posicion de toque a rayo
        Ray ray = Camera.main.ScreenPointToRay(touchPosition);
        RaycastHit hit;

        // Verifica si el rayo golpea el coche
        if (Physics.Raycast(ray, out hit, 100f))
        {
            // Verifica si el objeto golpeado es parte del coche
            if (hit.transform.IsChildOf(transform) || hit.transform == transform)
            {
                return true;
            }
        }

        return false;
    }

    void ScaleCarMobile(float scaleDelta)
    {
        Vector3 currentScale = transform.localScale;
        float newScaleValue = currentScale.x + scaleDelta;
        newScaleValue = Mathf.Clamp(newScaleValue, minScale, maxScale);
        transform.localScale = new Vector3(newScaleValue, newScaleValue, newScaleValue);
    }

    void RotateCarMobile(float rotationDelta)
    {
        transform.Rotate(0, rotationDelta, 0, Space.World);
    }

    void ScaleCar(float scrollDelta)
    {
        // Version PC con rueda del raton
        Vector3 currentScale = transform.localScale;
        float scaleChange = scrollDelta * 0.5f;
        float newScaleValue = currentScale.x + scaleChange;
        newScaleValue = Mathf.Clamp(newScaleValue, minScale, maxScale);
        transform.localScale = new Vector3(newScaleValue, newScaleValue, newScaleValue);
        Debug.Log("Escala: " + newScaleValue.ToString("F2") + "x");
    }

    void RotateCar(float direction)
    {
        // Version PC con teclas
        float rotationAmount = direction * 50f * Time.deltaTime;
        transform.Rotate(0, rotationAmount, 0, Space.World);
        Debug.Log("Rotacion: " + transform.localEulerAngles.y.ToString("F1") + " grados");
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

            if (spinDirection.magnitude > 0.1f && rigidBody != null)
            {
                rigidBody.linearVelocity = spinDirection * spinMoveSpeedRuntime;
            }
        }
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
            float hInput = GetHorizontalInput();
            Vector3 right = transform.right * hInput;
            spinDirection = right.normalized;
        }

        // Inicia el spin
        isSpinning = true;
        spinStartTime = Time.time;
        spinStartRotation = transform.localEulerAngles.y;
        lastSpinTime = Time.time;

        // Calcula la velocidad para este spin
        if (useSpinDistance)
        {
            spinMoveSpeedRuntime = (spinDuration > 0f) ? (spinTravelDistance / spinDuration) : 0f;
        }
        else
        {
            spinMoveSpeedRuntime = spinMoveSpeed;
        }

        if (spinDirection.magnitude > 0.1f)
        {
            Debug.Log(useSpinDistance
                ? "Spin 360 con desplazamiento de " + spinTravelDistance.ToString("F2") + " m."
                : "Spin 360 grados con movimiento (velocidad).");
        }
        else
        {
            Debug.Log("Spin 360 grados!");
        }
    }
}