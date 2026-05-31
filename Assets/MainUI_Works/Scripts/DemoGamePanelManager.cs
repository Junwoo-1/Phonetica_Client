using UnityEngine;
using UnityEngine.UI;

public class DemoGamePanelManager : MonoBehaviour
{
    [Header("--- Target UI Element ---")]
    [Tooltip("게임 패널 중앙에 위치한 이미지를 연결하세요.")]
    public Image centerUnitImage;

    [Header("--- Unit Sprites Array ---")]
    [Tooltip("8개의 유닛 스프라이트를 여기에 전부 넣어두어야 합니다.")]
    public Sprite[] allUnitSprites;

    // 유니티에서 패널이 활성화(SetActive(true))될 때마다 호출됩니다.
    // 데모 패널로 넘어가는 순간 실행됩니다.
    void OnEnable()
    {
        SetCurrentUnitImage();
    }

    void SetCurrentUnitImage()
    {
        // 1. PlayerPrefs에서 저장된 유닛 이름을 가져옵니다. (없으면 기본값 "Base")
        string savedUnitName = PlayerPrefs.GetString("SelectedVoxType", "Base");

        // 에셋 이름(예: GearUnit)과 맞추기 위해 "Unit"을 붙여줍니다.
        string targetAssetName = savedUnitName + "Unit";

        // 2. 저장된 이름과 일치하는 스프라이트를 스프라이트 뭉치(배열)에서 찾습니다.
        Sprite matchingSprite = null;

        if (allUnitSprites != null)
        {
            foreach (Sprite sprite in allUnitSprites)
            {
                // [수정됨] 스프라이트 에셋의 이름이 targetAssetName("GearUnit")과 정확히 같은지 확인합니다!
                if (sprite != null && sprite.name == targetAssetName)
                {
                    matchingSprite = sprite;
                    break; // 찾았으면 반복문을 멈춥니다.
                }
            }
        }

        // 3. 찾은 스프라이트를 이미지에 적용합니다.
        if (centerUnitImage != null && matchingSprite != null)
        {
            centerUnitImage.sprite = matchingSprite;
        }
    }
}