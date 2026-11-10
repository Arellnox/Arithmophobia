using UnityEngine;
using TMPro;

public class RoomTag : MonoBehaviour
{
    [Tooltip("Le numéro de la salle (ex: 8, 666, 13)")]
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
}