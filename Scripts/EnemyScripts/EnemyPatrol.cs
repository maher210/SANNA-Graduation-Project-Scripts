using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyPatrol : MonoBehaviour
{
    private NavMeshAgent agent;
    private EnemyChasing enemyAI;

    private bool canPatrol = true;

    [Header("Patrol Points")]

    [Header("Patrol Settings")]
    public float patrolSpeed = 3.5f;

    public float waitTimeAtPoint = 2f;

    private bool isWaiting = false;
    public Transform[] patrolPoints;

    // أي نقطة حالياً
    private int currentPointIndex = 0;

    void Start()
{
    agent = GetComponent<NavMeshAgent>();

    enemyAI = GetComponent<EnemyChasing>();

    // سرعة الباترول
    agent.speed = patrolSpeed;

    

    GoToNextPoint();
}

    void Update()
{
    agent.speed = patrolSpeed;
   
    // إذا الباترول متوقف
    if (!canPatrol)
        return;

    // إذا يحقق بصوت
    if (enemyAI.currentState != EnemyChasing.EnemyState.Idle)
        return;

    // إذا ينتظر عند النقطة
    if (isWaiting)
        return;

    // إذا وصل للنقطة
    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
    {
        StartCoroutine(WaitAtPoint());
    }
}

    void GoToNextPoint()
    {
        // إذا لا يوجد نقاط
        if (patrolPoints.Length == 0)
            return;

        // الذهاب للنقطة الحالية
        agent.SetDestination(patrolPoints[currentPointIndex].position);

        // الانتقال للنقطة التالية
        currentPointIndex++;

        // إذا وصل للنهاية يرجع للبداية
        if (currentPointIndex >= patrolPoints.Length)
        {
            currentPointIndex = 0;
        }
    }

IEnumerator WaitAtPoint()
{
    isWaiting = true;

    // وقف العدو
    agent.isStopped = true;

    // انتظر
    yield return new WaitForSeconds(waitTimeAtPoint);

    // ارجع الحركة
    agent.isStopped = false;

    GoToNextPoint();

    isWaiting = false;
}
    public void StopPatrol()
        {
            canPatrol = false;

            // وقف الحركة الحالية
            agent.ResetPath();
        }

    public void ResumePatrol()
        {
            canPatrol = true;

            // ارجع مباشرة للباترول
            GoToNextPoint();
        }
}