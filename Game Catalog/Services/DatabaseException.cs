using System;

namespace Game_Catalog.Services
{
    public enum DatabaseErrorKind { Constraint, Duplicate, Unavailable, Unknown }

    /// <summary> Represents an exception that occurs during database operations, providing information about the type of error encountered./// </summary>
    public class DatabaseException : Exception
    {
        public DatabaseErrorKind Kind { get; }

        public DatabaseException(DatabaseErrorKind kind, string message, Exception? inner = null)
            : base(message, inner) => Kind = kind;
    }
}