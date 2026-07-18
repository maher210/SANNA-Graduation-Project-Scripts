using UnityEngine;
[RequireComponent(typeof(Animator))]
public class TrapAnimation : MonoBehaviour
{
    private Animator animator;
    private Trap trap;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        animator = GetComponent<Animator>();
        trap = GetComponent<Trap>();
    }

    // Update is called once per frame
    void Update()
    {
        if (trap == null) return;

        TrapTrigger();

    }
    public void TrapTrigger()
    {
        animator.SetBool("IsTriggered", trap.isTriggered);
    }
}
