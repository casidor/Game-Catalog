using System;

namespace Game_Catalog.Data
{
    /// <summary> Kind of a database failure, used to pick a user-friendly message. </summary>
    public enum DatabaseErrorKind { Constraint, Duplicate, Unavailable, Unknown }

    /// <summary> Exception raised for failed database operations. </summary>
    public class DatabaseException : Exception
    {
        /// <summary> Category of the failure. </summary>
        public DatabaseErrorKind Kind { get; }

        public DatabaseException(DatabaseErrorKind kind, string message, Exception? inner = null)
            : base(message, inner) => Kind = kind;
    }
}