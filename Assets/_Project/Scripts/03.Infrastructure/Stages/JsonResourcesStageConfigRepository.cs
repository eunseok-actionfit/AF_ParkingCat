using System;
using Cysharp.Threading.Tasks;
using ParkingCat.Config;
using ParkingCat.Repository;
using UnityEngine;


namespace ParkingCat.Stages
{
    /// <summary>
    /// Resources의 TextAsset(JSON)에서 StageConfig를 로드한다.
    /// 예: Assets/Resources/StageConfigs/stage_1.json
    /// </summary>
    public sealed class JsonResourcesStageConfigRepository : IStageConfigRepository
    {
        const string _basePath = "StageConfigs";

        public UniTask<StageConfig> LoadAsync(int stageId)
        {
            var path = $"{_basePath}/stage_{stageId}";
            var ta = Resources.Load<TextAsset>(path);
            if (ta == null)
                throw new Exception($"StageConfig JSON not found at Resources: {path}.json");

            var cfg = JsonUtility.FromJson<StageConfig>(ta.text);
            if (cfg == null)
                throw new Exception($"Failed to parse StageConfig JSON: {path}.json");

            Validate(cfg);

            return UniTask.FromResult(cfg);
        }

        static void Validate(StageConfig cfg)
        {
            if (cfg.Width <= 0 || cfg.Height <= 0)
                throw new Exception($"Invalid size: {cfg.Width}x{cfg.Height}");

            int expected = cfg.Width * cfg.Height;
            if (cfg.Road01 == null || cfg.Road01.Length != expected)
                throw new Exception($"Road01 length mismatch. expected={expected}, actual={(cfg.Road01 == null ? -1 : cfg.Road01.Length)}");

            if (cfg.Cars == null)
                cfg.Cars = Array.Empty<StageConfig.CarConfig>();

            // 선택: 값 범위 sanity
            for (int i = 0; i < cfg.Cars.Length; i++)
            {
                var c = cfg.Cars[i];
                if (c.Length <= 0) throw new Exception($"Car length invalid. carId={c.CarId}");
            }
        }
    }
}