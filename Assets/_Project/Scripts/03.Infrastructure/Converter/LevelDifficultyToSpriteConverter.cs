using System;
using AES.Tools;
using ParkingCat.ValueObjects.Level;
using UnityEngine;


// LevelDifficulty 네임스페이스에 맞게 조정

namespace ParkingCat.Converter
{
    [CreateAssetMenu(
        fileName = "LevelDifficultyToSpriteConverter",
        menuName = "AES/Converters/LevelDifficulty To Sprite"
    )]
    public sealed class LevelDifficultyToSpriteConverter : ValueConverterSOBase
    {
        [Header("Sprites")]
        [SerializeField]
        Sprite normal;
        [SerializeField] Sprite hard;
        [SerializeField] Sprite veryHard;

        [Header("Optional")]
        [Tooltip("매핑이 없거나 파싱 실패 시 반환할 기본 스프라이트(없으면 null).")]
        [SerializeField]
        Sprite fallback;

        public override object Convert(object value, Type targetType, object parameter, IFormatProvider provider)
        {
            // ImageSpriteBinding에서 typeof(Sprite)로 요청하므로 방어적으로 체크
            if (targetType != typeof(Sprite))
                return null;

            if (value == null)
                return fallback;

            // 1) 이미 LevelDifficulty로 들어오는 케이스
            if (value is LevelDifficulty diff)
                return GetSprite(diff);

            // 2) int로 들어오는 케이스(바인딩 소스에 따라)
            if (value is int i && Enum.IsDefined(typeof(LevelDifficulty), i))
                return GetSprite((LevelDifficulty)i);

            // 3) string으로 들어오는 케이스("Normal"/"Hard"/"VeryHard")
            if (value is string s && Enum.TryParse(s, true, out LevelDifficulty parsed))
                return GetSprite(parsed);

            return fallback;
        }

        Sprite GetSprite(LevelDifficulty diff)
        {
            switch (diff)
            {
                case LevelDifficulty.Normal:   return normal != null ? normal : fallback;
                case LevelDifficulty.Hard:     return hard != null ? hard : fallback;
                case LevelDifficulty.VeryHard: return veryHard != null ? veryHard : fallback;
                default: return fallback;
            }
        }
    }
}