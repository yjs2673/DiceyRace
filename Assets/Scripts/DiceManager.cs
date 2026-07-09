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

    private bool isKnockedBack = false; // 넉백 상태인지 여부

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

    // PlayerController에서 충돌 시 호출할 넉백 함수
    public void ApplyPenaltyKnockback()
    {
        if (!isMoving || remainingMoves > 0) return;
        isKnockedBack = true;
    }

    // 플레이어 이동 코루틴
    private IEnumerator MoveRoutine()
    {
        isMoving = true;

        while (remainingMoves > 0)
        {
            remainingMoves--;
            isKnockedBack = remainingMoves <= 0; // 마지막 이동일 경우 넉백 신호를 받을 수 있도록 설정
            Vector3 startPos = player.transform.position;
            Vector3 targetPos = startPos + Vector3.right * moveDistance;
            float elapsedTime = 0f;

            // 1칸 이동 (Lerp)
            while (elapsedTime < moveDuration)
            {
                // 이동 도중 넉백 신호를 받으면 즉시 루프 탈출
                // if (isKnockedBack) break;

                player.transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 넉백을 당했을 경우의 처리
            if (isKnockedBack)
            {
                // 위치를 출발했던 1칸 전(startPos)으로 강제 복귀
                player.transform.position = startPos;

                CheckStopTile(); // 정지 발판 체크
                UpdateUI(remainingMoves, remainingRerolls);

                // 잠시 대기 후 다음 루프(또는 턴 종료) 진행
                yield return new WaitForSeconds(0.2f);

                // if (remainingMoves <= 0) break;

                break;
            }

            // 정상적으로 1칸 도착했을 경우
            player.transform.position = targetPos;
            // player.invincibleMoveCount = Mathf.Max(0, player.invincibleMoveCount - 1); // 무적 이동 횟수 감소
            // TODO: 무적 이동 횟수 감소 처리 로직
            // remainingMoves--;
            UpdateUI(remainingMoves, remainingRerolls);

            // 이동이 모두 끝났을 때 정지 발판 체크
            if (remainingMoves <= 0)
            {
                CheckStopTile();
                break;
            }

            yield return new WaitForSeconds(0.2f);
        }

        // 루프 종료 후 남은 이동 수 UI 동기화 방어코드
        if (remainingMoves <= 0)
        {
            remainingMoves = 0;
            UpdateUI(remainingMoves, remainingRerolls);
        }

        isMoving = false;
        rollButton.interactable = true;
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