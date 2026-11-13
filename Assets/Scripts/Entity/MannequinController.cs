using UnityEngine;
using UnityEngine.SceneManagement;

public class MannequinController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    
    [Header("Lobby")]
    [SerializeField] private string lobbySceneName = "MainScene";

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

        // Seuil du champ de vision (ajuste si besoin)
        if (dotProduct > 0.3f)
        {
            float distanceToMonster = Vector3.Distance(_playerCamera.position, transform.position);

            RaycastHit hit;
            // On lance un raycast depuis la caméra vers le mannequin
            if (Physics.Raycast(_playerCamera.position, directionToMonster, out hit, distanceToMonster))
            {
                // Si le premier objet touché N'EST PAS le mannequin (ou un de ses enfants), c'est qu'un mur bloque !
                if (hit.transform != transform && !hit.transform.IsChildOf(transform))
                {
                    return false; // Il y a un mur ou un objet entre nous
                }
            }

            // Si rien n'a bloqué le raycast en chemin, la vue est totalement dégagée
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

    // Détection du contact avec le joueur pour retourner au lobby
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Le monstre a attrapé le joueur ! Retour au lobby.");
            SceneManager.LoadScene(lobbySceneName);
        }
    }
}