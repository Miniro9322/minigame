using System.Collections.Generic;
using UnityEngine;

public class AttackZone : MonoBehaviour
{
    [SerializeField] private GameObject parent;

    private readonly HashSet<GameObject> hitTargets = new();
    private readonly List<Collider2D> overlapResults = new();
    private ContactFilter2D overlapFilter = new() { useTriggers = true };
    private Collider2D ownCollider;
    private IDamageable parentDamageable;

    private void EnsureRefs()
    {
        if (ownCollider == null) ownCollider = GetComponent<Collider2D>();
        if (parentDamageable == null && parent != null) parentDamageable = parent.GetComponent<IDamageable>();
    }

    private void OnEnable()
    {
        hitTargets.Clear();
    }

    private void OnTriggerStay2D(Collider2D collision) => TryDealDamage(collision);

    private void OnTriggerEnter2D(Collider2D collision) => TryDealDamage(collision);

    private void TryDealDamage(Collider2D collision)
    {
        if (hitTargets.Contains(collision.gameObject)) return;

        bool valid = (collision.gameObject.CompareTag("Player") && parent.CompareTag("Boss"))
                  || (collision.gameObject.CompareTag("Boss")   && parent.CompareTag("Player"));

        if (!valid) return;

        EnsureRefs();
        if (parentDamageable == null) return;
        if (!collision.gameObject.TryGetComponent<IDamageable>(out var target)) return;

        target.GetDamage(parentDamageable.SetDamage());
        hitTargets.Add(collision.gameObject);
    }

    public void Deactivate() => gameObject.SetActive(false);

    public void Activate()
    {
        hitTargets.Clear();
        gameObject.SetActive(true);

        EnsureRefs();
        if (ownCollider == null) return;

        overlapResults.Clear();
        Physics2D.OverlapCollider(ownCollider, overlapFilter, overlapResults);
        foreach (var hit in overlapResults)
            TryDealDamage(hit);
    }
}
