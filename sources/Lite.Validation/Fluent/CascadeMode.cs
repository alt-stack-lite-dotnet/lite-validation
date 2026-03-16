namespace Lite.Validation.Fluent
{
    /// <summary>
    /// Defines how validation should behave when a rule fails.
    /// </summary>
    public enum CascadeMode
    {
        /// <summary>
        /// Continue validating all rules regardless of failures.
        /// </summary>
        Continue,

        /// <summary>
        /// Stop validating further rules for the current property when a rule fails,
        /// but continue validating other properties.
        /// </summary>
        Stop
    }
}
