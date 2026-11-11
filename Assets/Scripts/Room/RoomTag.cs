using UnityEngine;
using TMPro;

public class RoomTag : MonoBehaviour
{
    [Tooltip("Le numéro de la salle")]
    public int tagID;

    [Tooltip("Le prefab de la salle correspondante à associer")]
    public GameObject correspondingRoomPrefab;

    [Tooltip("Cocher pour l'étiquette qui mène à la vraie sortie (le lucky number)")]
    public bool isExit;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI textMeshPro;

    private void OnValidate()
    {
        UpdateTagVisuals();
    }

    private void Start()
    {
        UpdateTagVisuals();
    }

    public void UpdateTagVisuals()
    {
        if (textMeshPro != null)
        {
            textMeshPro.text = tagID.ToString();
        }
    }

    // Ramassage : on détache l'étiquette (de la porte ou de la salle) puis on la cache.
    // Détacher évite qu'elle soit détruite avec la salle dont elle faisait partie.
    public void PickUp()
    {
        transform.SetParent(null);
        gameObject.SetActive(false);
    }

    // Pose libre (au sol, sur une table...)
    public void DropOrPlace(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        gameObject.SetActive(true);
        UpdateTagVisuals();
    }

    // Pose sur un emplacement précis (le slot de la porte)
    public void PlaceOn(Transform slot)
    {
        DropOrPlace(slot.position, slot.rotation);
        transform.SetParent(slot, true);
    }
}