using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyTest : MonoBehaviour
{
    private NavMeshAgent agent;

    // مكان البداية
    private Vector3 startPosition;

    // هل يحقق الآن؟
    private bool isInvestigating = false;

    // مدة البحث
    public float searchTime = 3f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // نحفظ مكان البداية
        startPosition = transform.position;
    }

    // =========================
    // 👂 سماع الصوت
    // =========================
    public void HearNoise(Vector3 noisePosition)
    {
        // إذا بالفعل يحقق لا تعيد تشغيله
        if (isInvestigating)
            return;

        StartCoroutine(InvestigateRoutine(noisePosition));
    }

    // =========================
    // 🔍 التحقيق
    // =========================
    IEnumerator InvestigateRoutine(Vector3 noisePosition)
    {
        isInvestigating = true;

        Debug.Log("Going To Noise");

        // يذهب لمكان الصوت
        agent.SetDestination(noisePosition);

        // ننتظر حتى يصل
        while (agent.pathPending || agent.remainingDistance > 0.5f)
        {
            yield return null;
        }

        Debug.Log("Searching...");

        // يبحث عدة ثواني
        yield return new WaitForSeconds(searchTime);

        Debug.Log("Returning Home");

        // يرجع لمكانه
        agent.SetDestination(startPosition);

        // ننتظر حتى يرجع
        while (agent.pathPending || agent.remainingDistance > 0.5f)
        {
            yield return null;
        }

        Debug.Log("Back To Position");

        isInvestigating = false;
    }
}