using UnityEngine;

public interface IDamageable
{
    struct DamageInfo
    {
        public int damage;
        public bool canParry;
        public bool ignoreInvincible;
        public Vector2 knockbackDir; 
    }

    DamageInfo SetDamage();

    void GetDamage(DamageInfo damageInfo);
}
