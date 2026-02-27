namespace ParkingCat.Entities
{
    public sealed class PlayerProgress
    {
        public int LastClearedLevel { get; private set; }

        public PlayerProgress(int lastClearedLevel)
        {
            if (lastClearedLevel < 0) lastClearedLevel = 0;

            LastClearedLevel = lastClearedLevel;
        }

        /// <summary>
        /// 해당 스테이지가 해금되어 있는지 여부
        /// </summary>
        public bool IsStageUnlocked(int stageId)
        {
            // 1스테이지는 항상 열려 있다고 가정
            if (stageId <= 1) return true;

            // 최고 클리어 스테이지 + 1 까지 입장 허용
            var maxPlayable = LastClearedLevel + 1;
            if (maxPlayable < 1) maxPlayable = 1;

            return stageId <= maxPlayable;
        }

        /// <summary>
        /// 스테이지 클리어 시 진행도 갱신
        /// </summary>
        public void UpdateLastCleared(int stageId)
        {
            if (stageId <= 0) return;

            // 이미 더 높은 스테이지를 클리어 했다면 무시
            if (LastClearedLevel >= stageId) return;

            LastClearedLevel = stageId;
        }

        /// <summary>
        /// 기본 진입 스테이지 결정 (최고 클리어 + 1, 범위 클램프)
        /// </summary>
        public int GetDefaultStage(int minStageId, int maxStageId)
        {
            var next = LastClearedLevel + 1;

            if (next < minStageId) next = minStageId;
            if (next > maxStageId) next = maxStageId;

            return next;
        }
    }
}