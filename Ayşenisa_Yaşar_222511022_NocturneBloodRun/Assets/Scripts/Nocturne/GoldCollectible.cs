using UnityEngine;

namespace Nocturne
{
    [RequireComponent(typeof(SphereCollider))]
    public class GoldCollectible : MonoBehaviour
    {
        [SerializeField] private int scoreValue = 10;
        [SerializeField] private float rotateSpeed = 110f;
        [SerializeField] private float bobAmplitude = 0.2f;
        [SerializeField] private float bobFrequency = 1.6f;
        [SerializeField] private ParticleSystem pickupEffectPrefab;

        private Vector3 basePosition;
        private bool collected;

        private void Awake()
        {
            SphereCollider trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            basePosition = transform.position;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
            transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * bobFrequency) * bobAmplitude);
        }

        public void Configure(int points, ParticleSystem pickupEffect)
        {
            scoreValue = points;
            pickupEffectPrefab = pickupEffect;
            basePosition = transform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected)
            {
                return;
            }

            SurvivorAgent survivor = other.GetComponentInParent<SurvivorAgent>();
            if (survivor == null || !survivor.IsAvailableTarget)
            {
                return;
            }

            collected = true;

            if (pickupEffectPrefab != null)
            {
                Instantiate(pickupEffectPrefab, transform.position + Vector3.up * 0.4f, Quaternion.identity);
            }

            survivor.CollectGold(scoreValue);
            Destroy(gameObject);
        }
    }
}
