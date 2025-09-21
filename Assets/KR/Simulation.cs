namespace KR
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using System.Collections; // I usually use UniTask but for simplicity, I'll use IEnumerator here

    public class Simulation : MonoBehaviour
    {
        #region Input Setup (New Input System)
        [SerializeField] private InputAction inputFire; // Input action for firing (don't forget to assign it in the inspector if missing)

        private void Awake()
        {
            inputFire = new InputAction("Fire", binding: "<Mouse>/leftButton");
        }

        private void OnEnable()
        {
            inputFire.Enable();
            inputFire.performed += FiringPerformed;
        }

        private void OnDisable()
        {
            inputFire.Disable();
            inputFire.performed -= FiringPerformed;
        }

        private void OnDestroy()
        {
            inputFire.Dispose();
        }

        private void FiringPerformed(InputAction.CallbackContext context)
        {
            if (context.performed && allowFiring)
            {
                StopCoroutine(pauseCoroutine);
                CallInterception();
                StartCoroutine(PauseSimulationCoroutine());
                allowFiring = false;
            }
        }

        #endregion

        #region Interception Point Example
        private readonly InterceptionPoint interceptionPoint = new();
        [Header("Self (Your Tank) Settings")]
        [SerializeField] private Rigidbody self; // Assign the tank's rigidbody in the inspector
        [SerializeField] private Transform selfTurret; // Assign the tank's turret transform in the inspector
        [SerializeField] private Rigidbody selfBullet; // Assign the bullet prefab in the inspector
        [SerializeField] private float selfMoveSpeed = 5f; // Speed at which the tank moves
        [SerializeField] private float selfBulletSpeed = 20f; // Speed of the bullet
        [SerializeField] private float selfTurretRotSpeed = 90f; // Speed of turret rotation in degrees per second

        [Header("Target Settings")]
        [SerializeField] private Rigidbody target; // Assign the target rigidbody in the inspector
        [SerializeField] private float targetMoveSpeed = 5f; // Speed at which the target moves
        [SerializeField] private float resetMovementTime = 5f; // Time to reset the target's movement
        private WaitForSeconds waitResetMovementTime;

        private bool allowFiring = true;

        private void CallInterception()
        {
            // I'll provide an example of how to use the InterceptionPoint class here.
            // Example usage of the InterceptionPoint class
            Vector3 selfPosition = self.position; // Get the tank's current world position
            Vector3 selfVelocity = self.linearVelocity; // Get the tank's current velocity
            Vector3 targetPosition = target.position; // Get the target's current world position
            Vector3 targetVelocity = target.linearVelocity; // Get the target's current velocity
            float bulletSpeed = this.selfBulletSpeed; // Use the serialized bullet speed

            if (interceptionPoint.CalculateInterceptPosition(selfPosition, selfVelocity, targetPosition, targetVelocity, bulletSpeed, out Vector3 interceptPosition))
            {
                // Now you have the interception or predicted point!
                // 1. Calculate the required direction: 
                Vector3 directionToFire = (interceptPosition - selfPosition).normalized;

                // Debug Draw raycast for visualization to hit the target
                Debug.DrawRay(selfTurret.position, directionToFire * 100f, Color.red, 2f);

                // 2. Rotate the turret towards directionToFire.
                SelfTurretRotation(selfTurret, directionToFire, selfTurretRotSpeed);

                // 3. Fire the projectile in that direction.
                // FireBullet(selfBullet, selfTurret, selfBulletSpeed);
                Debug.Log($"Interception possible at position: {interceptPosition}");
            }
            else
            {
                Debug.Log("Interception is not possible with current parameters.");
            }

            // Note: This is a basic example. In a real scenario, you would call this method when you want to fire, e.g., in response to player input.

            // Well you can put the parameter directly in the method call if you want
            // if (interceptionPoint.CalculateInterceptPosition(self.position, self.linearVelocity, target.position, target.linearVelocity, bulletSpeed, out Vector3 interceptPosition))
            // {
            //     Debug.Log($"Interception possible at position: {interceptPosition}");
            //     // Here you can rotate your turret towards interceptPosition and fire
            // }
        }


        private Vector3 defaultTargetPosition;
        private Vector3 defaultSelfPosition;
        private readonly WaitForSeconds waitAfterReset = new(0.01f);
        private readonly WaitForSecondsRealtime waitPauseSimulationTime = new(1f);
        private IEnumerator selfTurretRotCoroutine;
        private IEnumerator pauseCoroutine;
        private IEnumerator fireCoroutine;

        private void Start()
        {
            defaultTargetPosition = target.transform.position;
            defaultSelfPosition = self.transform.position;
            waitResetMovementTime = new(resetMovementTime);
            StartCoroutine(TargetAutoMovementCoroutine());
            pauseCoroutine = PauseSimulationCoroutine();
            fireCoroutine = FireBullet(selfBullet, selfTurret, selfBulletSpeed);
        }

        private IEnumerator PauseSimulationCoroutine()
        {
            Time.timeScale = 0f; // Pause the simulation
            yield return waitPauseSimulationTime; // Wait for 2 real-time seconds
            Time.timeScale = 1f; // Resume the simulation
        }

        private IEnumerator TargetAutoMovementCoroutine()
        {
            while (true)
            {
                // Reset Target Position
                self.transform.position = defaultSelfPosition;
                target.transform.position = defaultTargetPosition;

                // Reset Target Velocity
                self.linearVelocity = Vector3.zero;
                target.linearVelocity = Vector3.zero;

                yield return waitAfterReset; // Small delay to ensure position

                // Movement using Rigidbody velocity
                self.linearVelocity = selfMoveSpeed * -self.transform.right;
                target.linearVelocity = targetMoveSpeed * -target.transform.right;
                yield return waitResetMovementTime;
                allowFiring = true;
            }
        }

        private void SelfTurretRotation(Transform turret, Vector3 directionToTarget, float rotationSpeed)
        {
            if (selfTurretRotCoroutine != null)
            {
                StopCoroutine(selfTurretRotCoroutine);
            }
            selfTurretRotCoroutine = SelfTurretRotationCoroutine(turret, directionToTarget, rotationSpeed);
            StartCoroutine(selfTurretRotCoroutine);
        }

        private IEnumerator SelfTurretRotationCoroutine(Transform turret, Vector3 directionToTarget, float rotationSpeed)
        {
            // Example coroutine to rotate the turret towards the target position smoothly
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            while (Quaternion.Angle(turret.rotation, targetRotation) > 0.1f)
            {
                turret.rotation = Quaternion.RotateTowards(turret.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                yield return null;
            }

            StopCoroutine(selfTurretRotCoroutine);
            fireCoroutine = FireBullet(selfBullet, selfTurret, selfBulletSpeed);
            StartCoroutine(fireCoroutine);
        }

        private IEnumerator FireBullet(Rigidbody selfBullet, Transform selfTurret, float selfBulletSpeed)
        {
            selfBullet.transform.SetPositionAndRotation(self.transform.position, selfTurret.rotation); // Set the bullet's position and rotation to the turret's
            selfBullet.linearVelocity = Vector3.zero; // Reset velocity before firing
            yield return waitAfterReset; // Small delay to ensure velocity reset

            selfBullet.linearVelocity = selfTurret.forward * selfBulletSpeed; // Set the bullet's velocity
        }

        #endregion
    }
}