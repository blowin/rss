namespace RssDetect.Domain;

public abstract class DetectProgress
{
    public abstract void Match(Action<StartDetectProgress> startDetect, Action<IncreaseDetectProgress> updateDetect, Action<FinishDetectProgress> finishDetect);
}

public class StartDetectProgress : DetectProgress
{
    public int MaxOperation { get; }

    public StartDetectProgress(int maxOperation)
    {
        MaxOperation = maxOperation;
    }

    public override void Match(Action<StartDetectProgress> startDetect, Action<IncreaseDetectProgress> updateDetect, Action<FinishDetectProgress> finishDetect) 
        => startDetect(this);
}

public class IncreaseDetectProgress : DetectProgress
{
    public override void Match(Action<StartDetectProgress> startDetect, Action<IncreaseDetectProgress> updateDetect, Action<FinishDetectProgress> finishDetect) 
        => updateDetect(this);
}

public class FinishDetectProgress : DetectProgress
{
    public override void Match(Action<StartDetectProgress> startDetect, Action<IncreaseDetectProgress> updateDetect, Action<FinishDetectProgress> finishDetect) 
        => finishDetect(this);
}