using UnityEngine;

/// <summary>플레이어의 체력, 무적/패링 상태, 피격 처리를 전담한다.</summary>
public class PlayerCombat
{
    private readonly Player player;
    private readonly ParticleSystem parryEffect;

    private int  currHp;
    private bool invincible = false;
    private bool parrying   = false;

    public Vector2 KnockbackDir { get; private set; }

    public PlayerCombat(Player player, ParticleSystem parryEffect)
    {
        this.player      = player;
        this.parryEffect = parryEffect;
    }

    public void Initialize()
    {
        currHp = player.Data.MaxHp;
        player.OnHpChange?.Invoke(currHp, player.Data.MaxHp);
    }

    public void ToggleInvincible() => invincible = !invincible;
    public void ToggleParry()      => parrying   = !parrying;

    public IDamageable.DamageInfo SetDamage() => new() { canParry = false, damage = player.Data.Atk };

    public void GetDamage(IDamageable.DamageInfo damageInfo)
    {
        if (invincible && !damageInfo.ignoreInvincible)
        {
            return;
        }

        if (parrying && damageInfo.canParry)
        {
            player.SuccessParry?.Invoke();
            if (parryEffect)
            {
                parryEffect.transform.position = player.transform.position;
                parryEffect.Play();
            }
            return;
        }

        KnockbackDir = damageInfo.knockbackDir;
        currHp -= damageInfo.damage;

        player.OnHpChange?.Invoke(currHp, player.Data.MaxHp);

        if (currHp <= 0)
        {
            currHp = 0;
            player.Fsm.ChangeState(player.DeathState);
            return;
        }

        player.Fsm.ChangeState(player.HitState);

        player.OnHit?.Invoke();
    }
}
