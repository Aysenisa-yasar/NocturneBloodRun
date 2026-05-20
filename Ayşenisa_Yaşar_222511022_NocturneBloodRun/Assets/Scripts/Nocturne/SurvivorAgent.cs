using System;
using UnityEngine;
using UnityEngine.AI;

namespace Nocturne
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class SurvivorAgent : MonoBehaviour
    {
        public enum SurvivorState
        {
            Active,
            Safe,
            Dead
        }

        public enum ControlScheme
        {
            Wasd,
            ArrowKeys
        }

        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [SerializeField] private string survivorName = "Survivor";
        [SerializeField] private ControlScheme controlScheme = ControlScheme.Wasd;
        [SerializeField] private float moveSpeed = 5.1f;
        [SerializeField] private float panicBoostMultiplier = 1.25f;
        [SerializeField] private float panicDistance = 9f;
        [SerializeField] private float rotationSharpness = 10f;
        [SerializeField] private float fireCooldown = 0.38f;
        [SerializeField] private float fireRange = 28f;
        [SerializeField] private float collapsePitch = 78f;
        [SerializeField] private Vector3 fireOffset = new Vector3(0f, 1.2f, 0.58f);

        private NavMeshAgent agent;
        private Animator animator;
        private MonsterAgent monster;
        private NocturneScenarioDirector director;
        private ParticleSystem deathEffectPrefab;
        private ParticleSystem muzzleEffectPrefab;
        private ParticleSystem hitEffectPrefab;
        private GameObject shotTracerPrefab;
        private Vector2 cachedMoveInput;
        private Vector3 lastLookDirection = Vector3.forward;
        private float nextFireTime;
        private float animationBlend;
        private int collectedGold;
        private bool weaponsUnlocked;
        private bool controlsEnabled;

        public SurvivorState State { get; private set; } = SurvivorState.Active;
        public bool IsAvailableTarget => State == SurvivorState.Active;
        public string SurvivorName => survivorName;
        public int CollectedGold => collectedGold;
        public ControlScheme Scheme => controlScheme;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();

            Rigidbody body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            agent.updateRotation = false;
            agent.autoBraking = false;
            agent.isStopped = false;
        }

        private void Update()
        {
            if (!controlsEnabled || State != SurvivorState.Active)
            {
                cachedMoveInput = Vector2.zero;
                UpdateAnimator(0f);
                return;
            }

            cachedMoveInput = ReadMoveInput();

            if (Input.GetKeyDown(GetFireKey()))
            {
                TryFire();
            }

            UpdateAnimator(cachedMoveInput.magnitude);
        }

        private void FixedUpdate()
        {
            if (!controlsEnabled || State != SurvivorState.Active || !agent.isOnNavMesh)
            {
                return;
            }

            Vector3 moveDirection = new Vector3(cachedMoveInput.x, 0f, cachedMoveInput.y);
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            bool isMoving = moveDirection.sqrMagnitude > 0.001f;
            if (!isMoving)
            {
                return;
            }

            float speed = moveSpeed;
            if (monster != null && monster.IsAlive)
            {
                float monsterDistance = Vector3.Distance(transform.position, monster.transform.position);
                if (monsterDistance < panicDistance)
                {
                    speed *= panicBoostMultiplier;
                }
            }

            agent.Move(moveDirection * speed * Time.fixedDeltaTime);
            lastLookDirection = moveDirection.normalized;

            Quaternion desiredRotation = Quaternion.LookRotation(lastLookDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSharpness * Time.fixedDeltaTime);
        }

        public void Configure(
            ControlScheme scheme,
            NocturneScenarioDirector owner,
            MonsterAgent monsterAgent,
            ParticleSystem deathEffect,
            ParticleSystem muzzleEffect,
            ParticleSystem impactEffect,
            GameObject tracerPrefab)
        {
            controlScheme = scheme;
            director = owner;
            monster = monsterAgent;
            deathEffectPrefab = deathEffect;
            muzzleEffectPrefab = muzzleEffect;
            hitEffectPrefab = impactEffect;
            shotTracerPrefab = tracerPrefab;
            controlsEnabled = true;
            weaponsUnlocked = false;
            collectedGold = 0;
            cachedMoveInput = Vector2.zero;
            nextFireTime = 0f;
            State = SurvivorState.Active;

            if (transform.forward.sqrMagnitude > 0.001f)
            {
                lastLookDirection = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            }
        }

        public void SetWeaponsUnlocked(bool unlocked)
        {
            weaponsUnlocked = unlocked;
        }

        public void SetControllable(bool enabled)
        {
            controlsEnabled = enabled;
            agent.isStopped = !enabled;

            if (!enabled)
            {
                cachedMoveInput = Vector2.zero;
            }
        }

        public void CollectGold(int scoreAmount)
        {
            if (State != SurvivorState.Active || director == null)
            {
                return;
            }

            collectedGold++;
            director.RegisterGoldPickup(this, scoreAmount);
        }

        public void ReachSafety(Vector3 lookPoint)
        {
            if (State != SurvivorState.Active)
            {
                return;
            }

            State = SurvivorState.Safe;
            SetControllable(false);

            Vector3 lookDirection = lookPoint - transform.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.05f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            }

            director?.NotifySurvivorEscaped(this);
        }

        public void Die()
        {
            if (State != SurvivorState.Active)
            {
                return;
            }

            State = SurvivorState.Dead;
            SetControllable(false);

            if (animator != null)
            {
                animator.enabled = false;
            }

            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position + Vector3.up * 1.1f, Quaternion.identity);
            }

            transform.rotation = Quaternion.Euler(collapsePitch, transform.eulerAngles.y, 0f);
            director?.NotifySurvivorEaten(this);
        }

        public string GetControlSummary()
        {
            return controlScheme == ControlScheme.Wasd
                ? "Remy: WASD + Left Shift"
                : "Peasant Girl: Arrow Keys + Right Ctrl";
        }

        private Vector2 ReadMoveInput()
        {
            int horizontal = 0;
            int vertical = 0;

            if (controlScheme == ControlScheme.Wasd)
            {
                if (Input.GetKey(KeyCode.A))
                {
                    horizontal--;
                }

                if (Input.GetKey(KeyCode.D))
                {
                    horizontal++;
                }

                if (Input.GetKey(KeyCode.S))
                {
                    vertical--;
                }

                if (Input.GetKey(KeyCode.W))
                {
                    vertical++;
                }
            }
            else
            {
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    horizontal--;
                }

                if (Input.GetKey(KeyCode.RightArrow))
                {
                    horizontal++;
                }

                if (Input.GetKey(KeyCode.DownArrow))
                {
                    vertical--;
                }

                if (Input.GetKey(KeyCode.UpArrow))
                {
                    vertical++;
                }
            }

            return new Vector2(horizontal, vertical);
        }

        private KeyCode GetFireKey()
        {
            return controlScheme == ControlScheme.Wasd ? KeyCode.LeftShift : KeyCode.RightControl;
        }

        private void TryFire()
        {
            if (!weaponsUnlocked || Time.time < nextFireTime || monster == null || !monster.IsAlive)
            {
                return;
            }

            nextFireTime = Time.time + fireCooldown;

            Vector3 direction = lastLookDirection.sqrMagnitude > 0.001f ? lastLookDirection : transform.forward;
            Vector3 origin = transform.position + Vector3.up * fireOffset.y + direction.normalized * fireOffset.z;
            Vector3 hitPoint = origin + direction.normalized * fireRange;

            if (muzzleEffectPrefab != null)
            {
                Instantiate(muzzleEffectPrefab, origin, Quaternion.LookRotation(direction.normalized, Vector3.up));
            }

            RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, fireRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                hitPoint = hit.point;
                MonsterAgent hitMonster = hit.collider.GetComponentInParent<MonsterAgent>();
                if (hitMonster != null && hitMonster.IsAlive)
                {
                    hitMonster.TakeDamage(1, hit.point);
                }

                if (hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, hit.point, Quaternion.identity);
                }

                break;
            }

            if (shotTracerPrefab != null)
            {
                GameObject tracerObject = Instantiate(shotTracerPrefab);
                ShotTracer tracer = tracerObject.GetComponent<ShotTracer>();
                if (tracer != null)
                {
                    tracer.Configure(
                        origin,
                        hitPoint,
                        controlScheme == ControlScheme.Wasd
                            ? new Color(0.46f, 0.91f, 1f, 1f)
                            : new Color(1f, 0.83f, 0.3f, 1f));
                }
            }
        }

        private void UpdateAnimator(float moveBlend)
        {
            if (animator == null || !animator.enabled)
            {
                return;
            }

            animationBlend = Mathf.Lerp(animationBlend, moveBlend * moveSpeed, 8f * Time.deltaTime);
            animator.SetFloat(SpeedHash, animationBlend);
        }
    }
}
