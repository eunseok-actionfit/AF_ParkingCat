using System;
using AES.Tools;
using AES.Tools.UI.Popups;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace ParkingCat.Game.StageFailed
{
    public sealed class ExplanationPopupView : PopupViewBase
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Image image;

        [SerializeField] MonoContext context;

        public ExplanationDatabaseSO database;

        IDisposable _disposable;

        void Start()
        {
            var vm = context.ViewModel as ExplanationPopupVM;
            if (vm == null) return;
            _disposable = vm.Type.Subscribe(SetUp);
        }

        void OnDestroy() => _disposable?.Dispose();

        void SetUp(ExplanationType explanationType)
        {
            if (!database.TryGetEntry(explanationType, out var entry)) return;

            titleText.text = entry.TitleLocKey.GetLocalizedString();
            descriptionText.text = entry.DescLocKey.GetLocalizedString();
            image.sprite = entry.Image;
        }
    }
}