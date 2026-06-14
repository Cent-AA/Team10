using UnityEngine;

public interface Controllable
{
    float CurrentHealth { get; }
    Transform Transform { get; }
    bool IsDead { get; }
    void TakeDamage(float damage, Vector2 knockback);

}
