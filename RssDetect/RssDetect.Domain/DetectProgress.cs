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
    public static readonly DetectProgress Instance = new IncreaseDetectProgress();

    private IncreaseDetectProgress(){}

    public override void Match(Action<StartDetectProgress> startDetect, Action<IncreaseDetectProgress> updateDetect, Action<FinishDetectProgress> finishDetect) 
        => updateDetect(this);
}

public class FinishDetectProgress : DetectProgress
{
    public static readonly DetectProgress Instance = new FinishDetectProgress();

    private FinishDetectProgress(){}

    public override void Match(Action<StartDetectProgress> startDetect, Action<IncreaseDetectProgress> updateDetect, Action<FinishDetectProgress> finishDetect) 
        => finishDetect(this);
}