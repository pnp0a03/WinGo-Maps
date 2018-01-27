﻿using GoogleMapsUnofficial.View;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Devices.Geolocation;
using Windows.UI.Core;

namespace GoogleMapsUnofficial.ViewModel.PlaceControls
{
    class ACSuggestionProviderVM : INotifyPropertyChanged
    {
        private ObservableCollection<PlaceAutoComplete.Prediction> _searchres;

        public event PropertyChangedEventHandler PropertyChanged;
        
        public ObservableCollection<PlaceAutoComplete.Prediction> SearchResults
        {
            get
            {
                return _searchres;
            }
            set
            {
                _searchres = value;
                PropertyChanged?.Invoke(this , new PropertyChangedEventArgs("SearchResults"));
            }
        }

        public ACSuggestionProviderVM()
        {
            SearchResults = new ObservableCollection<PlaceAutoComplete.Prediction>();
            SearchResults.CollectionChanged += SuggestedApps_CollectionChanged;
        }
        public async void SuggestForSearch(string searchExpression)
        {
            await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async delegate
            {
                SearchResults.Clear();
                var s = await PlaceAutoComplete.GetAutoCompleteResults(searchExpression, location: MapView.MapControl.Center, radius: 50000);
                if (s == null) return;
                SearchResults.Add(new PlaceAutoComplete.Prediction() { description = "MyLocation" });
                foreach (var item in s.predictions)
                {
                    SearchResults.Add(item);
                }
            });

        }
        private void SuggestedApps_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SearchResults"));
        }
    }
}
