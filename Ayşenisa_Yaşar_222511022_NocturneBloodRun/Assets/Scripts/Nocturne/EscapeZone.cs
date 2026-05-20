using UnityEngine;

namespace Nocturne
{
    [RequireComponent(typeof(Collider))]
    public class EscapeZone : MonoBehaviour
    {
        [SerializeField] private ParticleSystem escapeBurstPrefab;
        [SerializeField] private Transform lookPoint;

        public void Configure(Transform safeLookPoint, ParticleSystem burstPrefab)
        {
            lookPoint = safeLookPoint;
            escapeBurstPrefab = burstPrefab;
        }

        private void Reset()
        {
            Collider zoneCollider = GetComponent<Collider>();
            zoneCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            SurvivorAgent survivor = other.GetComponentInParent<SurvivorAgent>();
            if (survivor == null || !survivor.IsAvailableTarget)
            {
                return;
            }

            survivor.ReachSafety(lookPoint != null ? lookPoint.position : transform.position + transform.forward * 4f);

            if (escapeBurstPrefab != null)
            {
                Instantiate(escapeBurstPrefab, survivor.transform.position + Vector3.up, Quaternion.identity);
            }
        }
    }
}
