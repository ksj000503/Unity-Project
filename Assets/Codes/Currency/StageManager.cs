using UnityEngine;

// 현재 스테이지 수 보관(코인 가치 스케일 등에 사용). 지금은 값 보관만 담당.
// 씬에 없으면 소비 측에서 스테이지 1로 간주(선택적 의존). 이후 웨이브/스테이지 진행 로직이 SetStage 로 갱신.
public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    [SerializeField] private int currentStage = 1;

    public int CurrentStage => Mathf.Max(1, currentStage);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetStage(int stage)
    {
        currentStage = Mathf.Max(1, stage);
    }
}
