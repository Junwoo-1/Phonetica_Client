using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitGridManager : MonoBehaviour
{
    [Header("--- Target UI Elements ---")]
    [Tooltip("바뀌어야 할 메인 화면의 유닛 이미지")]
    public Image targetUnitImage;

    [Tooltip("바뀌어야 할 메인 화면의 유닛 이름 텍스트")]
    public TMP_Text targetUnitName;

    [Header("--- Grid Window ---")]
    [Tooltip("껐다 켤 3x3 유닛 그리드 창")]
    public GameObject unitGridWindow;

    void Start()
    {
        if (unitGridWindow != null)
        {
            unitGridWindow.SetActive(false);
        }
    }

    public void OpenGridWindow()
    {
        if (unitGridWindow != null) unitGridWindow.SetActive(true);
    }

    public void CloseGridWindow()
    {
        if (unitGridWindow != null) unitGridWindow.SetActive(false);
    }

    // 실제 변경을 처리하는 내부 로직
    private void UpdateUnitInfo(Sprite newSprite, string newName)
    {
        if (targetUnitImage != null) targetUnitImage.sprite = newSprite;
        if (targetUnitName != null) targetUnitName.text = newName;

        // [중요] 유저가 선택한 유닛의 이름을 유니티 시스템에 저장합니다!
        PlayerPrefs.SetString("SelectedVoxType", newName);
    }

    // ==========================================
    // 유니티 버튼 인스펙터 연결용 함수들 (파라미터 1개)
    // ==========================================
    public void SelectUnit_Base(Sprite sprite) { UpdateUnitInfo(sprite, "Base"); }
    public void SelectUnit_Gear(Sprite sprite) { UpdateUnitInfo(sprite, "Gear"); }
    public void SelectUnit_Grid(Sprite sprite) { UpdateUnitInfo(sprite, "Grid"); }
    public void SelectUnit_Node(Sprite sprite) { UpdateUnitInfo(sprite, "Node"); }
    public void SelectUnit_Spin(Sprite sprite) { UpdateUnitInfo(sprite, "Spin"); }
    public void SelectUnit_Star(Sprite sprite) { UpdateUnitInfo(sprite, "Star"); }
    public void SelectUnit_Sun(Sprite sprite) { UpdateUnitInfo(sprite, "Sun"); }
    public void SelectUnit_Ward(Sprite sprite) { UpdateUnitInfo(sprite, "Ward"); }
}