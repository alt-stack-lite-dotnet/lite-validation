using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Lite.Validation
{
    public struct ValidationResult
    {
        private List<ValidationError>? _errors;

        public void Add(string propertyName, string details) =>
            (_errors ??= new List<ValidationError>()).Add(new ValidationError(propertyName, details));

        public void AddRange(IEnumerable<ValidationError> errors)
        {
            if (_errors is null) _errors = new List<ValidationError>();
            _errors.AddRange(errors);
        }

        public bool IsSuccess
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _errors is null || _errors.Count == 0;
        }

        public int ErrorCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _errors?.Count ?? 0;
        }

        public IReadOnlyList<ValidationError> Errors
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _errors ?? throw new InvalidOperationException();
        }
    }
}