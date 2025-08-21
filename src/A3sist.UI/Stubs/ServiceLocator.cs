using System;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Stub ServiceLocator class to allow UI project to compile
    /// This is a temporary stub for task 10.1 implementation
    /// </summary>
    public static class ServiceLocator
    {
        /// <summary>
        /// Get a service or return null if not found
        /// </summary>
        public static T? GetServiceOrNull<T>() where T : class
        {
            // Return null for stub implementation
            return null;
        }
    }
}