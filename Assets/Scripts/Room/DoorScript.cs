using UnityEngine;

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

        private GameObject activeRoomInstance;
        private bool hasTriggeredTransition = false;

        void Start()
        {
            asource = GetComponent<AudioSource>();
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
                asource.PlayOneShot(closeDoor); // Petit son de verrouillage/refus si besoin
                return;
            }

            // Bascule l'état d'ouverture de la porte
            open = !open;
            asource.clip = open ? openDoor : closeDoor;
            asource.Play();

            // Si on ouvre la porte et que la salle n'a pas encore été instanciée
            if (open && !hasTriggeredTransition)
            {
                TriggerRoomTransition();
            }
        }

        void TriggerRoomTransition()
        {
            hasTriggeredTransition = true;

            // Nettoie l'ancienne salle si nécessaire
            if (activeRoomInstance != null)
            {
                Destroy(activeRoomInstance);
            }

            // Instancie la salle correspondant à l'étiquette
            if (roomSpawnPoint != null)
            {
                activeRoomInstance = Instantiate(currentTag.correspondingRoomPrefab, roomSpawnPoint.position, roomSpawnPoint.rotation);
            }
        }

        // Méthode pour poser une étiquette par script (via l'inventaire ou un raycast)
        public void AttachTag(RoomTag newTag)
        {
            currentTag = newTag;
            hasTriggeredTransition = false; // Réinitialise si on change d'étiquette
        }
    }
