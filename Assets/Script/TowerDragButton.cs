using UnityEngine;
using UnityEngine.EventSystems;

// UI 버튼을 누르는 순간 드래그를 시작시키는 스크립트
public class TowerDragButton : MonoBehaviour, IPointerDownHandler
{
    public BuildManager buildManager;

    public void OnPointerDown(PointerEventData eventData)
    {
        buildManager.BeginDrag();
    }
}
