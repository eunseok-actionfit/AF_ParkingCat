using AES.Tools;
using Cysharp.Threading.Tasks;
using ParkingCat.Entities;
using UnityEngine;


namespace ParkingCat.Repository.Settings
{
    public sealed class SettingsRepository : ISettingsRepository
    {
        readonly IStorageService _storage;

        public SettingsRepository(IStorageService storage)
        {
            _storage = storage;
        }

        public async UniTask<UserSettings> LoadAsync()
        {
            var dto = await _storage.LoadAsync<SettingsData>();

            if (dto == null)
            {
                dto = new SettingsData
                {
                    language = GetSystemLanguageCode()
                };
            }

            return new UserSettings(
                dto.bgmVolume,
                dto.sfxVolume,
                dto.vibration,
                dto.language,
                dto.isBgmMuted,
                dto.isSfxMuted
            );
        }


        public async UniTask SaveAsync(UserSettings settings)
        {
            var dto = new SettingsData
            {
                bgmVolume = settings.BgmVolume,
                sfxVolume = settings.SfxVolume,
                vibration = settings.Vibration,
                language = settings.Language,
                isBgmMuted = settings.IsBgmMute,
                isSfxMuted = settings.IsSfxMute
            };

            await _storage.SaveAsync(dto);
        }
        
        static string GetSystemLanguageCode()
        {
            return Application.systemLanguage switch
            {
                SystemLanguage.Korean   => "ko-KR",
                SystemLanguage.Japanese => "ja-JP",
                _                       => "en-US"
            };
        }
    }

}


