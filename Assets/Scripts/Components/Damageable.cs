using DefaultNamespace.Interface;
using UnityEngine;
using UnityEngine.Events;

namespace DefaultNamespace.Components
{
    public class Damageable : MonoBehaviour, IDamageable
    {
        public UnityEvent OnDamageTaken;
        public void ReceiveDamage(int damage)
        {
            GameManager.Instance.ChangeFood(-damage);
            OnDamageTaken?.Invoke();
        }
    }
}