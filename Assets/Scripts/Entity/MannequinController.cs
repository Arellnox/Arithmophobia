using System.Collections;
using UnityEngine;

public class MannequinController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    
    [Header("Respawn Settings")]
    [Tooltip("Tag de l'objet vide placé au point de respawn dans le lobby")]
    [SerializeField] private string respawnTag = "Respawn";
    [Tooltip("CanvasGroup de l'image noire plein écran pour l'effet de mort")]
    [SerializeField] private CanvasGroup blackScreenCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f; // Vitesse du fondu

    private Rigidbody _rb;
    private Transform _playerCamera;
    private Transform _respawnPoint;
    private bool _isCaught = false;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (Camera.main != null)
        {
            _playerCamera = Camera.main.transform;
        }

        // Trouve automatiquement le point de respawn dans la scène grâce à son Tag
        GameObject respawnObj = GameObject.FindWithTag(respawnTag);
        if (respawnObj != null)
        {
            _respawnPoint = respawnObj.transform;
        }
        else
        {
            Debug.LogWarning("Aucun objet avec le tag 'Respawn' n'a été trouvé dans la scène !");
        }

        // S'assure que l'écran noir est bien invisible au démarrage
        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = 0f;
            blackScreenCanvasGroup.blocksRaycasts = false;
        }
    }

    void FixedUpdate()
    {
        if (_playerCamera == null || _isCaught) return;

        if (CheckIfPlayerIsLookingAtMe())
        {
            MoveTowardsPlayer();
        }
        else
        {
            // Il s'arrête net
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
        }
    }

    bool CheckIfPlayerIsLookingAtMe()
    {
        Vector3 directionToMonster = (transform.position - _playerCamera.position).normalized;
        float dotProduct = Vector3.Dot(_playerCamera.forward, directionToMonster);

        if (dotProduct > 0.3f)
        {
            float distanceToMonster = Vector3.Distance(_playerCamera.position, transform.position);

            RaycastHit hit;
            if (Physics.Raycast(_playerCamera.position, directionToMonster, out hit, distanceToMonster))
            {
                if (hit.transform != transform && !hit.transform.IsChildOf(transform))
                {
                    return false;
                }
            }
            return true;
        }

        return false;
    }

    void MoveTowardsPlayer()
    {
        Vector3 targetDirection = (_playerCamera.position - transform.position).normalized;
        targetDirection.y = 0f;

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 15f);
        }

        Vector3 moveVelocity = targetDirection * moveSpeed;
        _rb.linearVelocity = new Vector3(moveVelocity.x, _rb.linearVelocity.y, moveVelocity.z);
    }

    // Détection du contact avec le joueur
    private void OnCollisionEnter(Collision collision)
    {
        if (_isCaught) // Empêche de déclencher plusieurs fois à la suite
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            _isCaught = true;
            StartCoroutine(CaughtSequence(collision.gameObject));
        }
    }

    // Séquence : Écran noir -> Attente de 2s -> Respawn -> Révélation
    IEnumerator CaughtSequence(GameObject player)
    {
        // 1. Stoppe le mouvement du mannequin
        _rb.linearVelocity = Vector3.zero;

        // 2. Fondu vers le noir
        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.blocksRaycasts = true;
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                blackScreenCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
                yield return null;
            }
            blackScreenCanvasGroup.alpha = 1f;
        }

        // 3. Attente de 2 secondes d'écran noir complet
        yield return new WaitForSeconds(2f);

        // 4. Téléportation du joueur au point de respawn
        if (_respawnPoint != null)
        {
            // Si le joueur utilise un CharacterController, il faut le désactiver temporairement pour téléporter
            FirstPersonController fpc = player.GetComponent<FirstPersonController>();
            if (fpc != null) fpc.enabled = false;

            player.transform.position = _respawnPoint.position;
            player.transform.rotation = _respawnPoint.rotation;

            if (fpc != null) fpc.enabled = true;
        }
        else
        {
            Debug.LogError("Impossible de respawn : aucun _respawnPoint assigné !");
        }

        // Optionnel : Remettre le mannequin à sa position de départ si tu le souhaites (ou l'y laisser)
        // transform.position = _startPosition;

        // 5. Fondu inverse (l'écran s'éclaire)
        if (blackScreenCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                blackScreenCanvasGroup.alpha = Mathf.Clamp01(1f - (timer / fadeDuration));
                yield return null;
            }
            blackScreenCanvasGroup.alpha = 0f;
            blackScreenCanvasGroup.blocksRaycasts = false;
        }

        _isCaught = false;
    }
}