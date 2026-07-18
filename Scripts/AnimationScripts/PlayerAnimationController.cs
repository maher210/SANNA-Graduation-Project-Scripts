using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    public Animator animator;
    private PlayerMovement movement;
    private NoiseEmitter noiseEmitter;

    void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        noiseEmitter = GetComponent<NoiseEmitter>();
    }

    void Update()
    {
        if (movement == null) return;

        UpdateMovementAnimations();
        PlayNoise();
    }

    private void UpdateMovementAnimations()
    {

        animator.SetFloat("Speed", movement.CurrentSpeedPercent);


        animator.SetBool("IsCrouching", movement.IsCrouching);
    }

    public void PlayInteract()
    {
        animator.SetTrigger("Interact");
    }

    public void PlayBreak()
    {
        animator.SetTrigger("Break");
    }
    public void PlayNoise()
    {
        animator.SetBool("IsShaking", noiseEmitter.isShaking);
        
    }
}