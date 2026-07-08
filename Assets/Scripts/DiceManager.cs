using System.Collections;
// using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiceManager : MonoBehaviour
{
    [Header("UI References")]
    public Button rollButton;
    public Text diceText;               // 주사위 결과
    public Text remainingRerollText;    // 남은 리롤 횟수
    public Text remainingMoveText;      // 남은 이동 횟수

    [Header("Movement Settings")]
    public PlayerController player;
    public float moveDuration = 0.3f; // 한 칸 이동하는 시간
    public float moveDistance = 1f;   // 한 칸 이동하는 거리
    public int remainingRerolls = 0;  // 남은 리롤 횟수

    private int remainingMoves = 0;
    private bool isMoving = false;

    private void Start()
    {
        // 버튼 클릭 이벤트 연결
        rollButton.onClick.AddListener(RollDice);
        UpdateUI(0, remainingRerolls);
    }

    public void RollDice()
    {
        // 이동 중이거나 남은 횟수가 있으면 주사위 굴리기 금지
        if (isMoving || remainingRerolls <= 0 || remainingMoves > 0) return;

        int diceValue = Random.Range(1, 7); // 1~6 랜덤
        diceText.text = $"주사위: {diceValue}";

        remainingRerolls--;
        remainingMoves = diceValue;
        UpdateUI(remainingMoves, remainingRerolls);

        rollButton.interactable = false; // 이동 중 버튼 비활성화
        StartCoroutine(MoveRoutine());
    }

    // 플레이어 이동 코루틴
    private IEnumerator MoveRoutine()
    {
        isMoving = true;

        while (remainingMoves > 0)
        {
            yield return new WaitForSeconds(0.2f); // 칸과 칸 사이 약간의 대기 시간

            // 1칸 이동 애니메이션 (Lerp 사용)
            Vector3 startPos = player.transform.position;
            Vector3 targetPos = startPos + Vector3.right * moveDistance; // X축 양의 방향으로 이동
            float elapsedTime = 0f;

            while (elapsedTime < moveDuration)
            {
                player.transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            player.transform.position = targetPos; // 오차 보정

            remainingMoves--;
            player.invincibleMoveCount = Mathf.Max(0, player.invincibleMoveCount - 1); // 이동 횟수 기반 무적 차감
            UpdateUI(remainingMoves, remainingRerolls);

            // 충돌 등으로 인해 이동 횟수가 0 이하로 떨어졌을 경우 강제 종료
            if (remainingMoves <= 0)
            {
                remainingMoves = 0;
                break;
            }
        }

        UpdateUI(remainingMoves, remainingRerolls);
        isMoving = false;
        rollButton.interactable = true; // 턴 종료, 주사위 다시 활성화
    }

    public void AddReroll(int amount)
    {
        remainingRerolls += amount;
        Debug.Log($"리롤 횟수 증가: {amount} -> 남은 리롤: {remainingRerolls}");
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

        UpdateUI(remainingMoves, remainingRerolls);
        Debug.Log($"이동 수 변경: {amount} -> 남은 이동 수: {remainingMoves}");
    }

    private void UpdateUI(int currentMoves, int currentRerolls)
    {
        if (remainingMoveText != null)
        {
            remainingMoveText.text = $"남은 이동: {currentMoves}";
        }
        if (remainingRerollText != null)
        {
            remainingRerollText.text = $"남은 리롤: {currentRerolls}";
        }
    }
    
    private void CheckStopTile()
    {
        // Raycast를 사용하여 정지 발판 구분
        if (Physics.Raycast(player.transform.position, Vector3.down, out RaycastHit hit, 2f))
        {
            if (hit.collider.CompareTag("Tile"))
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                
                if (tile != null && tile.tileType == TileType.Stop)
                {
                    EffectProcessor.ApplyTileEffect(tile, this, player);
                }
            }
        }
    }
}