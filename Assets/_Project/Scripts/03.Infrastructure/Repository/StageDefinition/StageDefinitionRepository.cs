// using System;
// using System.Linq;
// using System.Security.Cryptography;
// using System.Text;
// using System.Threading;
// using AES.Tools;
// using AES.Tools.Core;
// using Cysharp.Threading.Tasks;
// using Domain;
// using Domain.Stage;
// using UnityEngine;
//
//
// namespace Infrastructure
// {
//     public sealed class StageDefinitionRepository : IStageDefinitionRepository
//     {
//         private readonly IStageLoadService _content;
//         private readonly IAssetProvider _assets;
//         private readonly ISlotService _slot;
//
//         public StageDefinitionRepository(
//             IStageLoadService content,
//             IAssetProvider assets,
//             ISlotService slot)
//         {
//             _content = content;
//             _assets = assets;
//             _slot = slot;
//         }
//
//         public async UniTask<bool> LoadDefinitionAsync(BoardData data, CancellationToken ct = default)
//         {
//             if (data == null)
//                 throw new ArgumentNullException(nameof(data));
//
//             var key = await GetDeterministicVariantKeyAsync(data, ct);
//
//             // var result = await _content.LoadDataAsync(key, data, ct);
//             // if (!result.IsSuccess)
//             // {
//             //     Debug.LogError($"[StageDefinitionRepository] Load failed: key={key}, error={result.Error}");
//             //     return false;
//             // }
//
//             Debug.Log($"[StageDefinitionRepository] Loaded stage definition: key={key}");
//
//             data.VariantStep += 1;
//             return true;
//         }
//
//
//         async UniTask<string> GetDeterministicVariantKeyAsync(BoardData data, CancellationToken ct)
//         {
//             string label = $"stage-l{data.Level}";
//             var all = await _assets.LoadByLabelAsync<TextAsset>(label, ct);
//             var ordered = all.OrderBy(a => a.name, StringComparer.Ordinal).ToArray();
//             int variantCount = ordered.Length;
//
//             if (variantCount == 0)
//                 throw new Exception($"No stage variants for label={label}");
//
//             var slotId = _slot.CurrentSlotId;
//             int step   = data.VariantStep;
//
//             int idx = GetDeterministicVariantIndex(slotId, data.Level, step, variantCount);
//
//             if (variantCount > 1 && data.LastVariantIndex >= 0 && data.LastVariantIndex < variantCount)
//             {
//                 if (idx == data.LastVariantIndex)
//                     idx = (idx + 1) % variantCount;
//             }
//
//             data.LastVariantIndex = idx;
//
//             var chosen = ordered[idx];
//
//             if (!TryParseStageName(chosen.name, out int parsedLevel, out int variant))
//                 throw new Exception($"Invalid stage name: {chosen.name}");
//
//             return $"stages/{parsedLevel}/{variant}";
//         }
//
//         // "stage_{level}_{variant}" 파싱
//         static bool TryParseStageName(string name, out int level, out int variant)
//         {
//             level = variant = -1;
//             var parts = name.Split('_');
//             if (parts.Length != 3) return false;
//             if (parts[0] != "stage") return false;
//             return int.TryParse(parts[1], out level) && int.TryParse(parts[2], out variant);
//         }
//
//         static int GetDeterministicVariantIndex(
//             string slotId,
//             int level,
//             int variantStep,
//             int variantCount)
//         {
//             if (variantCount <= 0) return 0;
//
//             using var sha = SHA256.Create();
//             string input = $"{slotId}:{level}:{variantStep}";
//             byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
//
//             uint raw = BitConverter.ToUInt32(hash, 0);
//             return (int)(raw % variantCount);
//         }
//     }
// }
