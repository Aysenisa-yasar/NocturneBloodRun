using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Nocturne
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class MonsterAgent : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        [SerializeField] private int maxHealth = 6;
        [SerializeField] private float roamSpeed = 3.4f;
        [SerializeField] private float chaseSpeed = 4.8f;
        [SerializeField] private float detectionRange = 45f;
        [SerializeField] private float attackRange = 2.1f;
        [SerializeField] private float attackCooldown = 2.3f;
        [SerializeField] private float eyeHeight = 1.55f;
        [SerializeField] private float attackWindup = 0.4f;
        [SerializeField] private float attackRecovery = 0.85f;
        [SerializeField] private float collapsePitch = 84f;
        [SerializeField] private LayerMask sightMask = ~0;

        private NavMeshAgent agent;
        private Animator animator;
        private SurvivorAgent[] survivors = Array.Empty<SurvivorAgent>();
        private NocturneScenarioDirector director;
        private ParticleSystem attackEffectPrefab;
        private ParticleSystem deathEffectPrefab;
        private SurvivorAgent currentTarget;
        private Vector3 lastKnownPosition;
        private float nextAttackTime;
        private int currentHealth;
        private bool isAttacking;
        private bool isDead;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsAlive => !isDead;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();

            Rigidbody body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            currentHealth = maxHealth;
        }

        public void Initialize(
            SurvivorAgent[] activeSurvivors,
            NocturneScenarioDirector owner,
            ParticleSystem attackEffect,
            ParticleSystem deathEffect)
        {
            survivors = activeSurvivors ?? Array.Empty<SurvivorAgent>();
            director = owner;
            attackEffectPrefab = attackEffect;
            deathEffectPrefab = deathEffect;
            currentHealth = maxHealth;
            isDead = false;
            isAttacking = false;
            lastKnownPosition = transform.position;
            nextAttackTime = 0f;

            if (agent != null)
            {
                agent.speed = roamSpeed;
                agent.isStopped = false;
            }
        }

        private void Update()
        {
            if (isDead)
            {
                UpdateAnimator(0f);
                return;
            }

            currentTarget = FindClosestTarget();
            if (currentTarget == null)
            {
                agent.isStopped = true;
                UpdateAnimator(0f);
                return;
            }

            agent.isStopped = false;
            bool hasSight = TryRefreshLastKnownPosition(currentTarget);
            agent.speed = hasSight ? chaseSpeed : roamSpeed;

            if (agent.isOnNavMesh)
            {
                agent.SetDestination(lastKnownPosition);
            }

            if (!isAttacking && Time.time >= nextAttackTime)
            {
                float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
                if (distance <= attackRange)
                {
                    StartCoroutine(AttackTarget(currentTarget));
                }
            }

            UpdateAnimator(agent.velocity.magnitude);
        }

        public void TakeDamage(int damageAmount, Vector3 hitPoint)
        {
            if (isDead)
            {
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - damageAmount);
            director?.RefreshHud();

            if (currentHealth <= 0)
            {
                Die(hitPoint);
            }
        }

        public void SetBehaviourEnabled(bool enabled)
        {
            enabled = enabled && !isDead;
            this.enabled = enabled;

            if (agent != null)
            {
                agent.isStopped = !enabled;
            }
        }

        private SurvivorAgent FindClosestTarget()
        {
            SurvivorAgent bestTarget = null;
            float bestDistance = float.MaxValue;

            foreach (SurvivorAgent survivor in survivors)
            {
                if (survivor == null || !survivor.IsAvailableTarget)
                {
                    continue;
                }

                float distance = (survivor.transform.position - transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = survivor;
                }
            }

            return bestTarget;
        }

        private bool TryRefreshLastKnownPosition(SurvivorAgent target)
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 targetPoint = target.transform.position + Vector3.up * 1.1f;
            Vector3 direction = targetPoint - origin;

            if (direction.sqrMagnitude > detectionRange * detectionRange)
            {
                return false;
            }

            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, detectionRange, sightMask, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<SurvivorAgent>() == target)
            {
                lastKnownPosition = target.transform.position;
                return true;
            }

            return false;
        }

        private IEnumerator AttackTarget(SurvivorAgent target)
        {
            if (target == null || isDead)
            {
                yield break;
            }

            isAttacking = true;
            nextAttackTime = Time.time + attackCooldown;
            agent.isStopped = true;

            Vector3 faceDirection = target.transform.position - transform.position;
            faceDirection.y = 0f;
            if (faceDirection.sqrMagnitude > 0.05f)
            {
                transform.rotation = Quaternion.LookRotation(faceDirection.normalized, Vector3.up);
            }

            if (animator != null)
            {
                animator.SetTrigger(AttackHash);
            }

            yield return new WaitForSeconds(attackWindup);

            if (!isDead
                && target != null
                && target.IsAvailableTarget
                && Vector3.Distance(transform.position, target.transform.position) <= attackRange + 0.8f)
            {
                if (attackEffectPrefab != null)
                {
                    Instantiate(attackEffectPrefab, target.transform.position + Vector3.up, Quaternion.identity);
                }

                target.Die();
            }

            yield return new WaitForSeconds(attackRecovery);

            if (!isDead)
            {
                agent.isStopped = false;
            }

            isAttacking = false;
        }

        private void Die(Vector3 hitPoint)
        {
            isDead = true;
            isAttacking = false;
            agent.isStopped = true;

            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            if (animator != null)
            {
                animator.enabled = false;
            }

            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, hitPoint + Vector3.up * 0.5f, Quaternion.identity);
            }

            transform.rotation = Quaternion.Euler(collapsePitch, transform.eulerAngles.y, 0f);
            director?.NotifyMonsterDefeated();
        }

        private void UpdateAnimator(float speed)
        {
            if (animator == null || !animator.enabled)
            {
                return;
            }

            animator.SetFloat(SpeedHash, speed, 0.15f, Time.deltaTime);
        }
    }
}
