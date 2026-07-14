namespace Shared.Enums.Jobs;

public enum BackgroundJobStatus : byte
{
    Pending    = 1,
    Processing = 2,
    Completed  = 3,
    Failed     = 4,
    Cancelled  = 5
}
