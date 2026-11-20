using System.Collections;
using UnityEngine;
using TMPro;

// 웨이브 시작 때 화면 가운데에 잠깐 떴다가 사라지는 알림
[RequireComponent(typeof(CanvasGroup))]
public class WaveBanner : MonoBehaviour
{
    public TMP_Text titleText;    // "웨이브 4 / 10"
    public TMP_Text storyText;    // "적들이 갑옷을 보강했다"
    public TMP_Text scaleText;    // "적 체력 ×2.4  (▲0.3)"

    public float fadeIn = 0.3f;
    public float hold = 1.8f;
    public float fadeOut = 0.5f;

    CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;   // 알림이 떠 있어도 맵 클릭과 드래그를 막지 않음
        group.interactable = false;
    }

    // prevScale: 직전 웨이브 배율 (1웨이브면 0을 넘겨서 증가량을 표시하지 않음)
    public void Show(int wave, int total, string message, float hpScale, float prevScale = 0f)
    {
        titleText.text = $"웨이브 {wave} / {total}";

        // 스토리 줄: 문구가 없으면 숨김
        bool hasStory = !string.IsNullOrEmpty(message);
        storyText.gameObject.SetActive(hasStory);
        if (hasStory) storyText.text = message;

        // 정보 줄: 배율 + 직전 웨이브 대비 증가량
        string delta = "";
        if (prevScale > 0f && hpScale > prevScale)
            delta = $"  <size=80%><color=#FF8A80>(▲{hpScale - prevScale:0.0})</color></size>";
        scaleText.text = $"적 체력 ×{hpScale:0.0}{delta}";

        StopAllCoroutines();
        StartCoroutine(Play());
    }

    IEnumerator Play()
    {
        yield return Fade(0f, 1f, fadeIn);
        yield return new WaitForSeconds(hold);
        yield return Fade(1f, 0f, fadeOut);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }
}