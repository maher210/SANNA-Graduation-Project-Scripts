using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSound : MonoBehaviour, IPointerEnterHandler
{
    public UIAudioManager audioManager;

    public void OnPointerEnter(PointerEventData eventData)
    {
        audioManager.PlayHover();
    }
}