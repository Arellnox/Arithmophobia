using UnityEngine;

public class MannequinController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    
    private Rigidbody _rb;
    private Transform _playerCamera;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (Camera.main != null)
        {
            _playerCamera = Camera.main.transform;
        }
    }

    void FixedUpdate()
    {
        if (_playerCamera == null) return;

        // Vérifie si le joueur regarde dans la direction du mannequin
        if (CheckIfPlayerIsLookingAtMe())
        {
            // LE MANNEQUIN BOUGE (puisqu'on le regarde - ange pleureur inversé)
            MoveTowardsPlayer();
        }
        else
        {
            // IL S'ARRÊTE NET (dès qu'on ne le regarde plus)
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
        }
    }

    bool CheckIfPlayerIsLookingAtMe()
    {
        Vector3 directionToMonster = (transform.position - _playerCamera.position).normalized;

        // Le Dot Product vérifie si le mannequin est dans l'écran du joueur (champ de vision)
        // 0.3 ou 0.4 permet une tolérance plus large pour pas que ça bloque s'il est légèrement sur le côté de l'écran
        float dotThreshold = 0.3f; 
        float dotProduct = Vector3.Dot(_playerCamera.forward, directionToMonster);

        if (dotProduct > dotThreshold)
        {
            // Vérification des murs (Raycast)
            RaycastHit hit;
            if (Physics.Raycast(_playerCamera.position, directionToMonster, out hit, Mathf.Infinity))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    return true;
                }
            }
        }

        return false;
    }

    void MoveTowardsPlayer()
    {
        // Direction vers le joueur sur le plan horizontal (XZ)
        Vector3 targetDirection = (_playerCamera.position - transform.position).normalized;
        targetDirection.y = 0f;

        // Rotation optionnelle pour qu'il regarde le joueur en avançant
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 15f);
        }

        // APPLICATION DE LA VITESSE LINEAIRE 
        Vector3 moveVelocity = targetDirection * moveSpeed;
        _rb.linearVelocity = new Vector3(moveVelocity.x, _rb.linearVelocity.y, moveVelocity.z);
    }
}