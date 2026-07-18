using UnityEngine;

public class Interactable : MonoBehaviour
{
    [SerializeField] private Sprite interactionSprite;

    public Sprite GetInteractionSprite()
    {
        return interactionSprite;
    }
}