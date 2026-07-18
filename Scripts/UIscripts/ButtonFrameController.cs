using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonFrameController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Tooltip("Drag the single global Selection Frame UI object here")]
    public RectTransform globalSelectionFrame;


    public void OnPointerEnter(PointerEventData eventData)
    {
        MoveFrameToThisButton();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (EventSystem.current.currentSelectedGameObject != gameObject)
        {
            HideFrame();
        }
    }


    public void OnSelect(BaseEventData eventData)
    {
        MoveFrameToThisButton();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        HideFrame();
    }

    // --- CORE LOGIC ---

    private void MoveFrameToThisButton()
    {
        if (globalSelectionFrame != null)
        {
            globalSelectionFrame.gameObject.SetActive(true);
            
            globalSelectionFrame.position = transform.position;
            
        }
    }

    private void HideFrame()
    {
        if (globalSelectionFrame != null)
        {
            globalSelectionFrame.gameObject.SetActive(false);
        }
    }
}