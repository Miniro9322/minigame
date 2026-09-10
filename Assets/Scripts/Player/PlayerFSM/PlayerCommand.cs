/// <summary>플레이어 입력 버퍼(콤보 큐)에 쌓이는 명령 종류</summary>
public enum PlayerCommand
{
    Attack, // 지상/공중 일반 공격 콤보
    Down,   // 아래 입력 (공중 낙하 공격 예약)
}
