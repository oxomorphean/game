using System;
using System.Collections.Generic;
using System.Linq;

namespace LinhThu.Domain.Core
{
    public enum DomainError
    {
        None,
        InvalidInput,
        NotFound,
        WrongOwner,
        InvalidState,
        InsufficientResource,
        OnCooldown,
        OutOfRange,
        Blocked,
        CapacityExceeded,
        MissingDefinition,
        InvalidSave,
        UnsupportedSchema,
        NoEffect
    }

    public readonly struct DomainResult
    {
        public bool Succeeded => ErrorCode == DomainError.None;
        public DomainError ErrorCode { get; }

        public string Message { get; }

        private DomainResult(DomainError error, string message)
        {
            ErrorCode = error;
            Message = message;
        }

        public static DomainResult Ok() => new DomainResult(DomainError.None, "");
        public static DomainResult Fail(DomainError error, string message = "")
        {
            if (error == DomainError.None)
                throw new ArgumentException(nameof(error));
            return new DomainResult(error, message);
        }
    }

    public readonly struct DomainResult<T>
    {
        public bool Succeeded { get; }

        public DomainError ErrorCode { get; }

        public string Message { get; }

        public T Value { get; }

        private DomainResult(bool success, T value, DomainError error, string message)
        {
            Succeeded = success;
            Value = value;
            ErrorCode = error;
            Message = message;
        }

        public static DomainResult<T> Ok(T value) => new DomainResult<T>(true, value, DomainError.None, "");
        public static DomainResult<T> Fail(DomainError error, string message = "") => new DomainResult<T>(false, default, error, message);
    }

    internal static class Guard
    {
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static float Number(float value, string name, float min = 0, float max = 1e9f)
        {
            if (!Finite(value) || value < min || value > max)
                throw new ArgumentException(name);
            return value;
        }

        internal static string Id(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
                throw new ArgumentException("Invalid definition ID.");
            return value;
        }

        internal static T EnumValue<T>(T value)
            where T : struct
        {
            if (!Enum.IsDefined(typeof(T), value))
                throw new ArgumentException(typeof(T).Name);
            return value;
        }

        internal static IReadOnlyList<T> Copy<T>(IEnumerable<T> values) => Array.AsReadOnly((values ?? throw new ArgumentNullException(nameof(values))).ToArray());
        internal static void Delta(float delta) => Number(delta, nameof(delta), 0, .1f);
    }
}
