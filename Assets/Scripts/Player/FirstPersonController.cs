using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform seedPrefab;

    [Header("Move parameters")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float airMoveSpeed = 4f;
    [SerializeField] private float runSpeed = 3f;
    [SerializeField] private AnimationCurve runCurve;
    [SerializeField] private AnimationCurve jumpCurve;
    [SerializeField] private float additionalGravity = 10f;
    [SerializeField] private float maxJumpForce = 8f;
    [SerializeField] private float maxJumpTime = 1f;
    [SerializeField] private float jumpDuration = 1.2f;

    [Header("Mouse look parameters")]
    [SerializeField] private float sensitivity = 1.5f; // Sensibilité directe et nette
    [SerializeField] private Transform originTsfm;
    
    [Header("Headbob parameters")]
    [SerializeField] private AnimationCurve headbobCurve;
    [SerializeField] private Transform camHolder;
    [SerializeField] private float headbobAmp = 0.05f;
    [SerializeField] private float headbobTime = 0.5f;

    [Header("UI / Options Integration")]
    [SerializeField] private GameObject optionPanel;

    [Header("Others")]
    [SerializeField] private float maxSeedSize = 10;

    private bool invertYAxis = false;
    
    private Transform mainCam;
    private Rigidbody rb;
    private CapsuleCollider coll;

    private float curSpeed;
    private bool jump;
    private bool jumping;
    private Vector3 moveVector;
    private float curJumpForce;
    private float curPitch;
    private bool grounded;
    private float runTimer;
    private float jumpTimer;
    private float headbobTimer;

    private InputActionMap actionMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction runAction;
    private InputAction jumpAction;

    private bool isPaused = false;

    void Awake()
    {
        if (inputActions != null)
        {
            actionMap = inputActions.FindActionMap("FirstPerson");
            if (actionMap == null && inputActions.actionMaps.Count > 0)
            {
                actionMap = inputActions.actionMaps[0];
            }

            if (actionMap != null)
            {
                moveAction = actionMap.FindAction("Move");
                lookAction = actionMap.FindAction("Look");
                runAction = actionMap.FindAction("Run");
                jumpAction = actionMap.FindAction("Jump");

                actionMap.Enable();

                if (jumpAction != null)
                    jumpAction.performed += OnJump;
            }
        }

        rb = GetComponent<Rigidbody>();
        coll = GetComponent<CapsuleCollider>();
        mainCam = GetComponentInChildren<Camera>().transform;

        if (rb != null)
        {
            rb.freezeRotation = true;
        }

        curSpeed = moveSpeed;
        moveVector = Vector3.zero;
        jump = false;
        jumping = false;
        runTimer = 0;
        jumpTimer = 0;
        curPitch = camHolder ? camHolder.localEulerAngles.x : 0f;
        headbobTimer = 0;
        curJumpForce = 0;
    }

    void OnEnable()
    {
        if (actionMap != null) { try { actionMap.Enable(); } catch {} }
    }

    void OnDisable()
    {
        if (actionMap != null) { try { actionMap.Disable(); } catch {} }
    }

    void Start()
    {
        ResumeGame();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        if (isPaused) return;

        grounded = false;
        if (originTsfm != null && coll != null)
        {
            if (Physics.SphereCast(originTsfm.position, coll.radius, -transform.up, out RaycastHit hit, (coll.height * 0.5f) - coll.radius + 0.2f, ~LayerMask.GetMask("Player")))
            {
                grounded = true;
            }
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Ray ray = new Ray(mainCam.position, mainCam.forward);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, 10f))
            {
                // Interaction
            }
        }

        Vector2 v = (moveAction != null) ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        moveVector = (transform.forward * v.y + transform.right * v.x).normalized;

        if (runAction != null && runAction.IsPressed())
        {
            curSpeed = moveSpeed + runCurve.Evaluate(runTimer / 1.5f) * runSpeed;
            runTimer += Time.deltaTime;
        }
        else
        {
            curSpeed = moveSpeed;
            runTimer = 0;
        }

        // --- CAMÉRA DIRECTE ET SANS INERTIE ---
        Vector2 mouseDelta = (lookAction != null) ? lookAction.ReadValue<Vector2>() : Vector2.zero;

        float mouseX = mouseDelta.x * sensitivity * 0.02f;
        float mouseY = mouseDelta.y * sensitivity * 0.02f;

        // Rotation horizontale instantanée
        transform.Rotate(0f, mouseX, 0f);

        // Rotation verticale instantanée
        if (!invertYAxis)
            mouseY = -mouseY;

        curPitch += mouseY;
        curPitch = Mathf.Clamp(curPitch, -85f, 85f);

        if (camHolder != null)
        {
            camHolder.localRotation = Quaternion.Euler(curPitch, 0, 0);
        }

        if (rb != null && rb.linearVelocity.magnitude > 0.1f && !jumping && grounded && camHolder != null)
        {
            mainCam.localPosition = new Vector3(0, headbobCurve.Evaluate(headbobTimer / headbobTime) * headbobAmp, 0);
            headbobTimer += Time.deltaTime;
            if (headbobTimer > headbobTime)
            {
                headbobTimer = 0;
            }
        }
    }

    private void FixedUpdate()
    {
        if (isPaused)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (jump)
        {
            if (!jumping)
            {
                if (grounded)
                {
                    jumpTimer = 0;
                }
                jumping = true;
                jump = false;
            }
        }
        else if (jumping) 
        {
            float jumpVel = jumpCurve.Evaluate(jumpTimer / jumpDuration) * curJumpForce;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVel, rb.linearVelocity.z);
            
            jumpTimer += Time.fixedDeltaTime;
            if (jumpTimer > jumpDuration)
            {
                jumpTimer = 0;
                jumping = false;
            }
        }
        else if (!jumping)
        {
            if (grounded) 
            {
                // Déplacement direct et net : s'arrête instantanément quand moveVector est nul
                rb.linearVelocity = new Vector3(moveVector.x * curSpeed, rb.linearVelocity.y, moveVector.z * curSpeed);
            }
            else 
            {
                Vector3 airVelocity = new Vector3(moveVector.x * airMoveSpeed, rb.linearVelocity.y, moveVector.z * airMoveSpeed);
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, airVelocity, 10f * Time.fixedDeltaTime);
                rb.AddForce(Vector3.down * additionalGravity, ForceMode.Acceleration);
            }
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isPaused) return;
        jump = true;
        curJumpForce = ((float)context.duration / maxJumpTime) * maxJumpForce;
        if (curJumpForce <= 0.1f) curJumpForce = maxJumpForce * 0.5f;
    }

    private IEnumerator GrowCoroutine(Transform seed)
    {
        for (float s = 0.1f; s < maxSeedSize; s += Time.deltaTime)
        {
            seed.localScale = new Vector3(s, s, s);
            yield return null;
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (optionPanel != null) optionPanel.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (optionPanel != null) optionPanel.SetActive(false);
    }
}