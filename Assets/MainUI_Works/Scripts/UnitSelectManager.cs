using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitSelectManager : MonoBehaviour
{
    [Header("--- Target UI Elements ---")]
    [Tooltip("바뀌어야 할 메인 화면의 유닛 이미지 (SelectedUnitImg)")]
    public Image targetUnitImage;

    [Tooltip("바뀌어야 할 메인 화면의 유닛 이름 텍스트 (UnitNameText)")]
    public TMP_Text targetUnitName;

    [Header("--- Panels to Toggle ---")]
    public GameObject playWindow;       // 기존 메인 모드/튜토리얼이 있는 창
    public GameObject unitSelectPanel;  // 새로 띄울 유닛 선택 창

    // ==========================================
    // 아래 함수들을 유닛 선택 창의 각 버튼 OnClick()에 연결합니다.
    // ==========================================

    public void SelectUnit_Alpha(Sprite unitSprite)
    {
        UpdateUnitInfo(unitSprite, "Alpha");
    }

    public void SelectUnit_Delta(Sprite unitSprite)
    {
        UpdateUnitInfo(unitSprite, "Delta");
    }

    public void SelectUnit_Omega(Sprite unitSprite)
    {
        UpdateUnitInfo(unitSprite, "Omega");
    }

    // ==========================================
    // 실제 이미지 교체 및 창 전환 로직
    // ==========================================
    private void UpdateUnitInfo(Sprite newSprite, string newName)
    {
        // 1. 이미지와 텍스트 교체
        if (targetUnitImage != null) targetUnitImage.sprite = newSprite;
        if (targetUnitName != null) targetUnitName.text = newName;

        // 2. 창 스위칭 (유닛 창 끄고 -> 메인 창 켜기)
        if (unitSelectPanel != null) unitSelectPanel.SetActive(false);
        if (playWindow != null) playWindow.SetActive(true);
    }
}