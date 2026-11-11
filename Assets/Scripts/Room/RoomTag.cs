using UnityEngine;
using TMPro;

public class RoomTag : MonoBehaviour
{
    [Tooltip("Le numéro de la salle")]
    public int tagID;

    [Tooltip("Le prefab de la salle correspondante à associer")]
    public GameObject correspondingRoomPrefab;

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

    // Méthode quand le joueur ramasse l'étiquette (on la cache de la scène)
    public void PickUp()
    {
        gameObject.SetActive(false);
    }

    // Méthode quand on la pose (au sol ou sur la porte)
    public void DropOrPlace(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        gameObject.SetActive(true);
        UpdateTagVisuals();
    }
}
