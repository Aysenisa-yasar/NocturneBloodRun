using System;
using UnityEngine;

namespace Nocturne
{
    public class CameraRigFollow : MonoBehaviour
    {
        [SerializeField] private Vector3 baseOffset = new Vector3(0f, 13f, -16f);
        [SerializeField] private float followSharpness = 4f;
        [SerializeField] private float rotationSharpness = 4f;
        [SerializeField] private float distanceScale = 0.35f;
        [SerializeField] private Transform[] targets = Array.Empty<Transform>();

        public void SetTargets(Transform[] followTargets)
        {
            targets = followTargets ?? Array.Empty<Transform>();
        }

        private void LateUpdate()
        {
            if (targets == null || targets.Length == 0)
            {
                return;
            }

            Vector3 center = Vector3.zero;
            int count = 0;

            foreach (Transform target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                center += target.position;
                count++;
            }

            if (count == 0)
            {
                return;
            }

            center /= count;

            float maxSpread = 0f;
            foreach (Transform target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                maxSpread = Mathf.Max(maxSpread, Vector3.Distance(center, target.position));
            }

            Vector3 desiredPosition = center + baseOffset + Vector3.back * (maxSpread * distanceScale) + Vector3.up * (maxSpread * 0.18f);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSharpness * Time.deltaTime);

            Vector3 lookPoint = center + Vector3.up * 1.4f;
            Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSharpness * Time.deltaTime);
        }
    }
}
