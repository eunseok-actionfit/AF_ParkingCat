using UnityEngine;


public interface ITutorialProgress
{
    bool IsCompleted(int stageId);
    void MarkCompleted(int stageId);
}

public sealed class PlayerPrefsTutorialProgress : ITutorialProgress
{
    const string KeyPrefix = "tutorial_done_stage_";
    public bool IsCompleted(int stageId) => PlayerPrefs.GetInt(KeyPrefix + stageId, 0) == 1;
    public void MarkCompleted(int stageId) => PlayerPrefs.SetInt(KeyPrefix + stageId, 1);
}