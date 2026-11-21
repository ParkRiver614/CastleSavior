using UnityEngine;

// 시작 화면 장식용: 웨이포인트를 따라 걷다가 끝에 닿으면 처음부터 다시 걸음
public class TitleWalker : MonoBehaviour
{
    public Transform[] waypoints;     // StartScene에 복사한 WP들
    public float speed = 1.5f;
    public float startDelay = 0f;     // 여러 마리를 시차를 두고 출발시키기

    int index;
    float wait;

    void Start()
    {
        wait = startDelay;
        transform.position = waypoints[0].position;
    }

    void Update()
    {
        if (wait > 0f)
        {
            wait -= Time.deltaTime;
            return;
        }

        Vector3 target = waypoints[index].position;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            index++;
            if (index >= waypoints.Length)   // 성에 도착하면 처음으로
            {
                index = 0;
                transform.position = waypoints[0].position;
            }
        }
    }
}