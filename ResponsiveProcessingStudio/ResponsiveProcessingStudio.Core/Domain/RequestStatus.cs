namespace ResponsiveProcessingStudio.Core.Domain;

public enum RequestStatus
{
    Created = 0,
    Waiting = 1,
    Classifying = 2,
    Validating = 3,
    Assigning = 4,
    Processing = 5,
    Completed = 6,
    Failed = 7,
    Cancelled = 8
}