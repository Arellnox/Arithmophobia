using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform seedPrefab;

    [Header("Move parameters")]
    [SerializeField] private float moveSpeed = 5f;     // Vitesse extrêmement basse pour contrer l'échelle de la map
    [SerializeField] private float runSpeedMultiplier = 1.2f;
    [SerializeField] private float jumpForce = 3.5f;
    [SerializeField] private float gravityMultiplier = 3f;

    [Header("Mouse look parameters")]
    [SerializeField] private float sensitivity = 1.2f;
    [SerializeField] private Transform originTsfm;
    
    [Header("Headbob parameters")]
    [SerializeField] private AnimationCurve headbobCurve;
    [SerializeField] private Transform camHolder;
    [SerializeField] private float headbobAmp = 0.02f;
    [SerializeField] private float headbobTime = 0.6f;

    [Header("UI / Options Integration")]
    [SerializeField] private GameObject optionPanel;

    [Header("Others")]
    [SerializeField] private float maxSeedSize = 10;

    private bool invertYAxis = false;
    
    private Transform mainCam;
    private Rigidbody rb;
    private CapsuleCollider coll;

    private float curSpeed;
    private bool jumpRequested;
    private Vector3 moveDirection;
    private float curPitch;
    private bool grounded;
    private float headbobTimer;

    private InputActionMap actionMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction runAction;
    private InputAction jumpAction;

    private bool isPaused = false;

    private RoomTag heldTag = null; // Étiquette tenue en main par le joueur

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
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        curSpeed = moveSpeed;
        moveDirection = Vector3.zero;
        jumpRequested = false;
        curPitch = camHolder ? camHolder.localEulerAngles.x : 0f;
        headbobTimer = 0;
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
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Ray ray = new Ray(mainCam.position, mainCam.forward);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
            {
                // 1. Interaction avec une étiquette au sol
                RoomTag floorTag = hitInfo.collider.GetComponent<RoomTag>();
                if (floorTag != null && heldTag == null)
                {
                    heldTag = floorTag;
                    heldTag.PickUp();
                    Debug.Log("Étiquette ramassée en main : " + heldTag.tagID);
                    return;
                }

                // 2. Interaction avec la porte
                DoorScript.Door door = hitInfo.collider.GetComponent<DoorScript.Door>();
                if (door != null)
                {
                    if (heldTag != null)
                    {
                        // Le joueur pose/échange son étiquette sur la porte
                        RoomTag returnedTag = door.SwapTag(heldTag);
                        heldTag = returnedTag; // Si la porte avait déjà une étiquette, le joueur récupère l'ancienne en main
                        Debug.Log("Étiquette placée sur la porte.");
                    }
                    else
                    {
                        // Pas d'étiquette en main, on essaie d'ouvrir la porte
                        door.OpenDoor();
                    }
                }
            }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (isPaused) ResumeGame();
                else PauseGame();
            }
        }

        if (isPaused) return;

        grounded = false;
        if (originTsfm != null && coll != null)
        {
            if (Physics.SphereCast(originTsfm.position, coll.radius * 0.9f, -transform.up, out RaycastHit hit, (coll.height * 0.5f) - coll.radius + 0.15f, ~LayerMask.GetMask("Player")))
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
        moveDirection = (transform.forward * v.y + transform.right * v.x).normalized;

        if (runAction != null && runAction.IsPressed())
        {
            curSpeed = moveSpeed * runSpeedMultiplier;
        }
        else
        {
            curSpeed = moveSpeed;
        }

        // --- CAMÉRA ---
        Vector2 mouseDelta = (lookAction != null) ? lookAction.ReadValue<Vector2>() : Vector2.zero;

        float mouseX = mouseDelta.x * sensitivity * 0.02f;
        float mouseY = mouseDelta.y * sensitivity * 0.02f;

        transform.Rotate(0f, mouseX, 0f);

        if (!invertYAxis)
            mouseY = -mouseY;

        curPitch += mouseY;
        curPitch = Mathf.Clamp(curPitch, -85f, 85f);

        if (camHolder != null)
        {
            camHolder.localRotation = Quaternion.Euler(curPitch, 0, 0);
        }

        if (rb != null && moveDirection.magnitude > 0.1f && grounded && camHolder != null)
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

        Vector3 targetVelocity = new Vector3(moveDirection.x * curSpeed, rb.linearVelocity.y, moveDirection.z * curSpeed);
        
        if (grounded)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, 20f * Time.fixedDeltaTime);

            if (jumpRequested)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
                jumpRequested = false;
            }
        }
        else
        {
            Vector3 airVelocity = new Vector3(moveDirection.x * (curSpeed * 0.8f), rb.linearVelocity.y, moveDirection.z * (curSpeed * 0.8f));
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, airVelocity, 5f * Time.fixedDeltaTime);
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        }

        jumpRequested = false;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isPaused) return;
        if (grounded)
        {
            jumpRequested = true;
        }
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
    
    public void QuitGame()
    {
        Debug.Log("Quit Game requested...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}