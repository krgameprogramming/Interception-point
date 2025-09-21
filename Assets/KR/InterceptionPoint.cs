namespace KR
{
    using UnityEngine;

    public class InterceptionPoint
    {
        /// <summary>
        /// Called each time your tank wants to fire. Calculate and return interceptPosition so that it is able to rotate the turret
        /// and fire at a moving target.
        /// </summary>
        /// <param name="selfPosition">Current position of your tank</param>
        /// <param name="selfVelocity">Current velocity of your tank</param>
        /// <param name="targetPosition">Position of the target</param>
        /// <param name="targetVelocity">Velocity of the target</param>
        /// <param name="bulletSpeed">When fired, this is the speed at which the bullet moves.</param>
        /// <param name="interceptPosition">The expected position at which the bullet will impact the moving target</param>
        /// <returns>Return True if it is possible to intercept. False otherwise (if the target is moving too fast).</returns>
        public bool CalculateInterceptPosition(Vector3 selfPosition, Vector3 selfVelocity, Vector3 targetPosition, Vector3 targetVelocity,
                                               float bulletSpeed, out Vector3 interceptPosition)
        {
            // Set a default value for interceptPosition in case we fail to find an interception.
            interceptPosition = default;

            // Calculate the relative position and velocity of the target with respect to the self.
            Vector3 relativePosition = targetPosition - selfPosition;
            Vector3 relativeVelocity = targetVelocity - selfVelocity;

            // The interception problem can be modeled as solving a quadratic equation of the form:
            // a*t^2 + b*t + c = 0
            // where t is the time to intercept.

            float a = Vector3.Dot(relativeVelocity, relativeVelocity) - (bulletSpeed * bulletSpeed);
            float b = 2f * Vector3.Dot(relativeVelocity, relativePosition);
            float c = Vector3.Dot(relativePosition, relativePosition);

            // Calculate the discriminant (delta) to check for real solutions.
            float discriminant = (b * b) - (4f * a * c);

            // If the discriminant is negative, there are no real solutions, meaning an interception is impossible.
            if (discriminant < 0)
            {
                return false;
            }

            // Calculate the time to intercept using the quadratic formula.
            // We take the smaller of the two positive roots, as this represents the first possible interception time.
            float t1 = (-b + Mathf.Sqrt(discriminant)) / (2f * a);
            float t2 = (-b - Mathf.Sqrt(discriminant)) / (2f * a);

            float timeToIntercept;

            if (t1 > 0 && t2 > 0)
            {
                timeToIntercept = Mathf.Min(t1, t2);
            }
            else if (t1 > 0)
            {
                timeToIntercept = t1;
            }
            else if (t2 > 0)
            {
                timeToIntercept = t2;
            }
            else
            {
                // Neither solution is positive, so interception is not possible.
                return false;
            }

            // Calculate the interception position based on the target's position and velocity at the calculated time.
            interceptPosition = targetPosition + (targetVelocity * timeToIntercept);

            return true;
        }
    }
}