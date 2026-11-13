using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

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
    [SerializeField] private float interactRange = 3f;

    private bool isReading = false; // true pendant la lecture d'une fiche

    [Header("Interaction prompts")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string readPrompt = "Press E to read";
    [SerializeField] private string takePrompt = "Press E to take";
    [SerializeField] private string putDownPrompt = "Press E to put down";
    [SerializeField] private string placeTagPrompt = "Press E to place the tag";
    [SerializeField] private string swapTagPrompt = "Press E to swap the tag";
    [SerializeField] private string openDoorPrompt = "Press F to open";
    [SerializeField] private string closeDoorPrompt = "Press F to close";

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
        if (actionMap != null) { try { actionMap.Enable(); } catch { } }
    }

    void OnDisable()
    {
        if (actionMap != null) { try { actionMap.Disable(); } catch { } }
    }

    void Start()
    {
        ResumeGame();
    }

    void Update()
    {
        // Lecture d'une fiche : seuls E et Échap sont actifs, ils referment la fiche
        if (isReading)
        {
            SetPrompt(null);
            if (Keyboard.current != null &&
                (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                CloseNote();
            }
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        if (isPaused)
        {
            SetPrompt(null);
            return;
        }

        UpdatePrompt();

        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame) InteractWithTags();   // E : étiquettes (ramasser / poser / échanger / reposer)
            if (Keyboard.current.fKey.wasPressedThisFrame) InteractWithDoor();   // F : ouvrir / fermer la porte
        }

        grounded = false;
        if (originTsfm != null && coll != null)
        {
            if (Physics.SphereCast(originTsfm.position, coll.radius * 0.9f, -transform.up, out RaycastHit hit, (coll.height * 0.5f) - coll.radius + 0.15f, ~LayerMask.GetMask("Player")))
            {
                grounded = true;
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

    // Raycast depuis le centre de la caméra
    private bool TryRaycast(out RaycastHit hit)
    {
        Ray ray = new Ray(mainCam.position, mainCam.forward);
        return Physics.Raycast(ray, out hit, interactRange, ~0, QueryTriggerInteraction.Ignore);
    }

    // F : ouvre / ferme la porte (même si on vise l'étiquette qui est dessus)
    private void InteractWithDoor()
    {
        if (!TryRaycast(out RaycastHit hit)) return;

        DoorScript.Door door = hit.collider.GetComponentInParent<DoorScript.Door>();
        if (door != null) door.OpenDoor();
    }

    // E : ramasser, poser, échanger ou reposer une étiquette
    private void InteractWithTags()
    {
        if (!TryRaycast(out RaycastHit hit)) return;

        // Fiche de note : on la lit
        NoteSheet note = hit.collider.GetComponentInParent<NoteSheet>();
        if (note != null)
        {
            OpenNote(note);
            return;
        }

        DoorScript.Door door = hit.collider.GetComponentInParent<DoorScript.Door>();
        RoomTag roomTag = hit.collider.GetComponent<RoomTag>();

        if (door != null)
        {
            // Étiquette ou panneau de la porte : poser, échanger ou reprendre l'étiquette
            if (roomTag != null || heldTag != null)
                heldTag = door.SwapTag(heldTag);
        }
        else if (roomTag != null && heldTag == null)
        {
            // Étiquette au sol : ramassage
            heldTag = roomTag;
            roomTag.PickUp();
        }
        else if (heldTag != null)
        {
            // On ne repose l'étiquette que sur une surface horizontale tournée vers le haut (sol, table...)
            if (Vector3.Dot(hit.normal, Vector3.up) < 0.7f) return;

            // Face avec le chiffre vers le haut, texte lisible depuis la position du joueur
            Quaternion rot = Quaternion.LookRotation(-hit.normal, transform.forward);
            heldTag.DropOrPlace(hit.point + hit.normal * 0.02f, rot);
            heldTag = null;
        }
    }

    // Affiche l'indication d'interaction selon ce que le joueur regarde
    private void UpdatePrompt()
    {
        if (promptText == null) return;

        string prompt = null;

        if (TryRaycast(out RaycastHit hit))
        {
            DoorScript.Door door = hit.collider.GetComponentInParent<DoorScript.Door>();
            RoomTag roomTag = hit.collider.GetComponent<RoomTag>();

            if (hit.collider.GetComponentInParent<NoteSheet>() != null)
            {
                prompt = readPrompt;
            }
            else if (door != null)
            {
                // Ligne "étiquette" (E) : seulement si la porte est fermée, sinon l'échange est refusé
                string tagLine = null;
                if (door.IsClosed)
                {
                    if (heldTag != null) tagLine = (roomTag != null) ? swapTagPrompt : placeTagPrompt;
                    else if (roomTag != null) tagLine = takePrompt;
                }

                // Ligne "porte" (F)
                string doorLine = door.open ? closeDoorPrompt : openDoorPrompt;
                prompt = (tagLine != null) ? tagLine + "\n" + doorLine : doorLine;
            }
            else if (roomTag != null && heldTag == null)
            {
                prompt = takePrompt;
            }
            else if (heldTag != null && Vector3.Dot(hit.normal, Vector3.up) >= 0.7f)
            {
                prompt = putDownPrompt;
            }
        }

        SetPrompt(prompt);
    }

    private void SetPrompt(string text)
    {
        if (promptText == null) return;

        bool show = !string.IsNullOrEmpty(text);
        if (show) promptText.text = text;
        if (promptText.gameObject.activeSelf != show) promptText.gameObject.SetActive(show);
    }

    private void OpenNote(NoteSheet note)
    {
        if (NarrationManager.Instance == null) return;

        NarrationManager.Instance.ShowNote(note);
        isReading = true;
    }

    private void CloseNote()
    {
        if (NarrationManager.Instance != null) NarrationManager.Instance.HideNote();
        isReading = false;
    }

    private void FixedUpdate()
    {
        if (isReading)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

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