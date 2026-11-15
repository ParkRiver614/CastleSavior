using UnityEngine;
using UnityEngine.EventSystems;

// UI 버튼을 누르는 순간 드래그 설치를 시작시키는 스크립트
public class TowerDragButton : MonoBehaviour, IPointerDownHandler
{
    public BuildManager buildManager;
    public bool isFoundation;   // 체크하면 '토대' 버튼, 해제하면 '포탑' 버튼

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isFoundation)
            buildManager.BeginDragFoundation();
        else
            buildManager.BeginDragTurret();
    }
}
