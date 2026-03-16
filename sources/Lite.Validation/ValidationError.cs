namespace Lite.Validation
{
    public readonly struct ValidationError
    {
        public ValidationError(string propertyName, string details)
        {
            PropertyName = propertyName;
            Details = details;
        }

        public string PropertyName { get; }
        public string Details { get; }
    }
}