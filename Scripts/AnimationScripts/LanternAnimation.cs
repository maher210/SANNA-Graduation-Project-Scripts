using UnityEngine;

[RequireComponent(typeof(Animator))]
public class LanternAnimation : MonoBehaviour
{
    private Animator FireBaseAnimator;

    [Header("References")]
    [Tooltip("Drag your main Lantern object here if it's on a different GameObject.")]
    [SerializeField] private LanternSystem lanternController;

    void Awake()
    {
        FireBaseAnimator = GetComponent<Animator>();

        // 🛠️ FIX: If you didn't assign it in the inspector, look for it automatically
        if (lanternController == null)
        {
            // First check this object
            lanternController = GetComponent<LanternSystem>();

            // If it's not here, look upward at the parent objects!
            if (lanternController == null)
            {
                lanternController = GetComponentInParent<LanternSystem>();
            }
        }
    }

    void Update()
    {
        // If we STILL can't find it, safe exit to prevent console errors
        if (lanternController == null || FireBaseAnimator == null) return;

        UpdateLanternAnimations();
    }

    private void UpdateLanternAnimations()
    {
        // Feeds the master system state straight to your animator controller parameters
        FireBaseAnimator.SetBool("IsLit", lanternController.isOn);
    }
}