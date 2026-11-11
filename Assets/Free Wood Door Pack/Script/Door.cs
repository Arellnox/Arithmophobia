using UnityEngine;
using UnityEngine.Events;

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

        [Header("Events")]
        [Tooltip("Appelé quand la porte s'ouvre sur l'étiquette de sortie (fin du jeu)")]
        public UnityEvent onExitOpened;

        private GameObject activeRoomInstance;
        private bool hasTriggeredTransition = false;

        // Vrai quand la porte est fermée ET a fini de bouger
        public bool IsClosed =>
            !open && Quaternion.Angle(transform.localRotation, Quaternion.Euler(0, DoorCloseAngle, 0)) < 2f;

        void Start()
        {
            asource = GetComponent<AudioSource>();

            // Si une étiquette est déjà assignée au démarrage, on la positionne sur le slot
            if (currentTag != null && tagSlotPoint != null)
            {
                currentTag.PlaceOn(tagSlotPoint);
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

        // Ouvre ou ferme la porte
        public void OpenDoor()
        {
            // On peut toujours fermer. Pour ouvrir, il faut une étiquette valide sur la porte.
            if (!open && (currentTag == null || currentTag.correspondingRoomPrefab == null))
            {
                Debug.Log("Impossible d'ouvrir : aucune étiquette valide n'est fixée sur la porte.");
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

            if (currentTag.isExit)
            {
                onExitOpened?.Invoke();
            }
        }

        // Pose ou échange l'étiquette de la porte.
        // Renvoie ce que le joueur doit avoir en main après l'opération.
        public RoomTag SwapTag(RoomTag incomingTag)
        {
            // On ne change l'étiquette que porte fermée : sinon la salle disparaîtrait sous les yeux du joueur
            if (!IsClosed)
            {
                Debug.Log("Ferme la porte avant de changer l'étiquette.");
                return incomingTag; // le joueur garde son étiquette en main
            }

            RoomTag oldTag = currentTag;

            // 1. L'ancienne étiquette quitte la porte (détachée + cachée, prête à être tenue en main)
            if (oldTag != null)
            {
                oldTag.PickUp();
            }

            // 2. La nouvelle étiquette se fixe sur la porte
            currentTag = incomingTag;
            if (currentTag != null && tagSlotPoint != null)
            {
                currentTag.PlaceOn(tagSlotPoint);
            }

            // 3. L'ancienne salle est détruite (même si on retire l'étiquette sans en remettre)
            if (activeRoomInstance != null)
            {
                Destroy(activeRoomInstance);
                activeRoomInstance = null;
            }
            hasTriggeredTransition = false;

            return oldTag;
        }
    }
}