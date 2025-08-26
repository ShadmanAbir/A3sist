namespace A3sist.Shared.Enums
{
    /// <summary>
    /// Severity levels for code fixes
    /// </summary>
    public enum FixSeverity
    {
        /// <summary>
        /// Informational fixes
        /// </summary>
        Info,

        /// <summary>
        /// Warning-level fixes
        /// </summary>
        Warning,

        /// <summary>
        /// Error-level fixes
        /// </summary>
        Error,

        /// <summary>
        /// Critical fixes that must be addressed
        /// </summary>
        Critical
    }
}