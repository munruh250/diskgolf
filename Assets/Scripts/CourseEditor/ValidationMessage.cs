namespace DiskGolf.CourseEditor
{
    public enum ValidationSeverity { Error, Warning }

    public readonly struct ValidationMessage
    {
        public string Code { get; }
        public ValidationSeverity Severity { get; }
        public string Text { get; }

        public ValidationMessage(string code, ValidationSeverity severity, string text)
        {
            Code = code;
            Severity = severity;
            Text = text;
        }
    }
}
