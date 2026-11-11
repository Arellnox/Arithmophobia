using UnityEngine;

namespace DoorScript
{
    [RequireComponent(typeof(AudioSource))]
    public class Door : MonoBehaviour
    {
        public bool open;
        public float smooth = 1.0f;
        float DoorOpenAngle = -90.0f;
        float DoorCloseAngle = 0.0f;
        public AudioSource asource;
        public AudioClip openDoor, closeDoor;

        [Header("Room & Tag System")]
        [Tooltip("L'étiquette actuellement placée sur cette porte")]
        public RoomTag currentTag;
        [Tooltip("Point exact où la salle doit s'instancier derrière la porte")]
        public Transform roomSpawnPoint;
        [Tooltip("Point où l'étiquette s'affiche visuellement sur la porte")]
        [SerializeField] private Transform tagSlotPoint;

        private GameObject activeRoomInstance;
        private bool hasTriggeredTransition = false;

        void Start()
        {
            asource = GetComponent<AudioSource>();

            // Si une étiquette est déjà assignée au démarrage, on la positionne sur le slot
            if (currentTag != null && tagSlotPoint != null)
            {
                currentTag.DropOrPlace(tagSlotPoint.position, tagSlotPoint.rotation);
                currentTag.transform.SetParent(tagSlotPoint);
            }
        }

        void Update()
        {
            if (open)
            {
                var target = Quaternion.Euler(0, DoorOpenAngle, 0);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * 5 * smooth);
            }
            else
            {
                var target1 = Quaternion.Euler(0, DoorCloseAngle, 0);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, target1, Time.deltaTime * 5 * smooth);
            }
        }

        public void OpenDoor()
        {
            // Vérification : La porte ne s'ouvre que s'il y a une étiquette ET un prefab de salle assigné
            if (currentTag == null || currentTag.correspondingRoomPrefab == null)
            {
                Debug.Log("Impossible d'ouvrir : aucune étiquette valide n'est fixée sur la porte.");
                //asource.PlayOneShot(closeDoor);
                return;
            }

            open = !open;
            asource.clip = open ? openDoor : closeDoor;
            asource.Play();

            if (open && !hasTriggeredTransition)
            {
                TriggerRoomTransition();
            }
        }

        void TriggerRoomTransition()
        {
            hasTriggeredTransition = true;

            if (activeRoomInstance != null)
            {
                Destroy(activeRoomInstance);
            }

            if (roomSpawnPoint != null && currentTag.correspondingRoomPrefab != null)
            {
                activeRoomInstance = Instantiate(currentTag.correspondingRoomPrefab, roomSpawnPoint.position, roomSpawnPoint.rotation);
            }
        }

        // Gère la pose ou l'échange d'étiquette sur la porte
        public RoomTag SwapTag(RoomTag incomingTag)
        {
            RoomTag oldTag = currentTag;

            // Installe la nouvelle étiquette sur la porte
            currentTag = incomingTag;
            if (currentTag != null && tagSlotPoint != null)
            {
                currentTag.DropOrPlace(tagSlotPoint.position, tagSlotPoint.rotation);
                currentTag.transform.SetParent(tagSlotPoint);
            }

            // Nettoie l'ancienne salle puisque l'étiquette change
            hasTriggeredTransition = false;
            if (activeRoomInstance != null)
            {
                Destroy(activeRoomInstance);
            }

            // Si la porte était ouverte, on la referme par sécurité lors du changement d'étiquette
            if (open)
            {
                open = false;
                asource.PlayOneShot(closeDoor);
            }

            return oldTag; // Renvoie l'ancienne étiquette (pour que le joueur la récupère en main)
        }
    }
}