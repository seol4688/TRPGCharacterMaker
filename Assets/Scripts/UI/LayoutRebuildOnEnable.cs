using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오브젝트가 활성화(SetActive true)될 때 하위 레이아웃을 강제 재계산합니다.
///
/// 사용처: MainPanelManager 등에서 SetActive로 관리되는 탭 패널에 부착.
/// ContentSizeFitter가 비활성 상태에서 계산되지 않는 문제를 해결합니다.
/// </summary>
public class LayoutRebuildOnEnable : MonoBehaviour
{
    [Tooltip("true: 다음 프레임에 재계산 (Instantiate 직후 크기 미확정 시)\nfalse: 즉시 재계산")]
    [SerializeField] private bool rebuildNextFrame = true;

    private void OnEnable()
    {
        if (rebuildNextFrame)
            StartCoroutine(RebuildCoroutine());
        else
            Rebuild();
    }

    private IEnumerator RebuildCoroutine()
    {
        yield return null;
        Rebuild();
    }

    private void Rebuild()
    {
        // 하위 RectTransform을 자식에서 부모 방향으로 재계산 (leaf → root 순서)
        RectTransform[] rects = GetComponentsInChildren<RectTransform>(false);
        for (int i = rects.Length - 1; i >= 0; i--)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rects[i]);
    }
}
