using System.Collections;
// using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiceManager : MonoBehaviour
{
    [Header("UI References")]
    public Button rollButton;
    public Text diceText;          // 주사위 결과
    public Text remainingMoveText; // 남은 이동 횟수

    [Header("Movement Settings")]
    public Transform playerTransform;
    public float moveDuration = 0.3f; // 한 칸 이동하는 시간
    public float moveDistance = 1f;   // 한 칸 이동하는 거리

    private int remainingMoves = 0;
    private bool isMoving = false;

    private void Start()
    {
        // 버튼 클릭 이벤트 연결
        rollButton.onClick.AddListener(RollDice);
        UpdateUI(0);
    }

    public void RollDice()
    {
        // 이동 중이거나 남은 횟수가 있으면 주사위 굴리기 금지
        if (isMoving || remainingMoves > 0) return;

        int diceValue = Random.Range(1, 7); // 1~6 랜덤
        diceText.text = $"주사위: {diceValue}";
        
        remainingMoves = diceValue;
        UpdateUI(remainingMoves);
        
        rollButton.interactable = false; // 이동 중 버튼 비활성화
        StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {
        isMoving = true;

        while (remainingMoves > 0)
        {
            yield return new WaitForSeconds(0.2f); // 칸과 칸 사이 약간의 대기 시간

            // 1칸 이동 애니메이션 (Lerp 사용)
            Vector3 startPos = playerTransform.position;
            Vector3 targetPos = startPos + Vector3.right * moveDistance; // X축 양의 방향으로 이동
            float elapsedTime = 0f;

            while (elapsedTime < moveDuration)
            {
                playerTransform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            playerTransform.position = targetPos; // 오차 보정

            remainingMoves--;
            UpdateUI(remainingMoves);

            // 충돌 등으로 인해 이동 횟수가 0 이하로 떨어졌을 경우 강제 종료
            if (remainingMoves <= 0)
            {
                remainingMoves = 0;
                break;
            }
        }

        UpdateUI(remainingMoves);
        isMoving = false;
        rollButton.interactable = true; // 턴 종료, 주사위 다시 활성화
    }

    // PlayerController에서 충돌 시 호출할 메서드
    public void ModifyMoves(int amount)
    {
        if (!isMoving) return;
        
        remainingMoves += amount;
        if (remainingMoves < 0)
        {
            remainingMoves = 0; // 음수 방지
        }
        
        UpdateUI(remainingMoves);
        Debug.Log($"이동 수 변경: {amount} -> 남은 이동 수: {remainingMoves}");
    }

    private void UpdateUI(int currentMoves)
    {
        if (remainingMoveText != null)
        {
            remainingMoveText.text = $"남은 이동: {currentMoves}";
        }
    }
}