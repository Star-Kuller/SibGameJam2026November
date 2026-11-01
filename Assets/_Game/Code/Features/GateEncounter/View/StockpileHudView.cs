using System;
using _Game.Code.Features.GateEncounter.Models;
using R3;
using UnityEngine.UIElements;
using VContainer;

namespace _Game.Code.Features.GateEncounter.View
{
    public sealed class StockpileHudView : DocumentSectionView
    {
        private StockpileModel _stockpile;
        private IDisposable _binding;

        [Inject]
        public void Bind(StockpileModel stockpile)
        {
            _stockpile = stockpile;
            Subscribe();
        }

        protected override void BindElements()
        {
            Subscribe();
        }

        protected override void UnbindElements()
        {
            _binding?.Dispose();
            _binding = null;
        }

        private void Subscribe()
        {
            _binding?.Dispose();

            if (_stockpile != null && Root != null)
            {
                _binding = _stockpile.Changed.Subscribe(_ => ShowStockpile());
            }
        }

        private void ShowStockpile()
        {
            Element<Label>("metal").text = "МЕТАЛЛ  " + _stockpile.Metal;
            var rounds = Element<Label>("rounds");
            rounds.text = _stockpile.Rounds.ToString("D3");
            rounds.EnableInClassList("danger-text", !_stockpile.HasRounds);
        }
    }
}
