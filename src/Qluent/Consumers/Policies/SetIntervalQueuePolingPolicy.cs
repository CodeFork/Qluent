namespace Qluent.Consumers.Policies
{
    using System;

    /// <inheritdoc />
    /// <summary>
    /// An <see cref="T:Qluent.Consumers.Policies.IMessageConsumerQueuePolingPolicy" /> which always returns a static interval regardless of previous success/failure
    /// </summary>
    /// <seealso cref="T:Qluent.Consumers.Policies.IMessageConsumerQueuePolingPolicy" />
    public class SetIntervalQueuePolingPolicy : IMessageConsumerQueuePolingPolicy
    {
        private readonly double _intervalMilliseconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetIntervalQueuePolingPolicy"/> class.
        /// </summary>
        /// <param name="intervalMilliseconds">The interval in milliseconds.</param>
        public SetIntervalQueuePolingPolicy(double intervalMilliseconds)
        {
            _intervalMilliseconds = intervalMilliseconds;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the next delay interval
        /// </summary>
        /// <param name="lastOperationSucceeded">if set to <c>true</c> the last dequeue operation succeeded.</param>
        /// <returns>
        /// A <see cref="T:System.TimeSpan" /> specifying the next delay
        /// </returns>
        public TimeSpan NextDelay(bool lastOperationSucceeded)
        {
            return TimeSpan.FromMilliseconds(_intervalMilliseconds);
        }

        /// <inheritdoc />
        /// <summary>
        /// Resets the policy to its default delay interval
        /// </summary>
        public void Reset()
        {
            
        }
    }
}