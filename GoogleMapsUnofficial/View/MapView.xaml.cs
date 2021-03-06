﻿using GoogleMapsUnofficial.ViewModel.GeocodControls;
using GoogleMapsUnofficial.ViewModel.PlaceControls;
using GoogleMapsUnofficial.ViewModel.SettingsView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Calls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Networking.Connectivity;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GoogleMapsUnofficial.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MapView : Page
    {
        public Geopoint SearchResultPoint
        {
            get
            {
                return (Geopoint)GetValue(SearchResultPointProperty);
            }
            set
            {
                SetValue(SearchResultPointProperty, value);
                RunMapRightTapped(Map, value);
            }
        }
        public static readonly DependencyProperty SearchResultPointProperty = DependencyProperty.Register(
         "SearchResultPoint",
         typeof(Geopoint),
         typeof(MapView),
         new PropertyMetadata(null)
        );
        Geopoint LastRightTap { get; set; }
        string LastPlaceID { get; set; }
        public static MapControl MapControl;
        public static MapView StaticMapView { get; set; }
        public MapView()
        {
            this.InitializeComponent();
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
            MapControl = Map;
            StaticMapView = this;
            //Map.RotateInteractionMode = MapInteractionMode.GestureOnly;
            Map.RotateInteractionMode = SettingsSetters.GetRotationControlsVisible();
            var ZoomInteractionMode = SettingsSetters.GetZoomControlsVisible();
            if (ZoomInteractionMode == MapInteractionMode.Auto || ZoomInteractionMode == MapInteractionMode.ControlOnly || ZoomInteractionMode == MapInteractionMode.GestureAndControl || ZoomInteractionMode == MapInteractionMode.PointerKeyboardAndControl)
                ZoomUserControl.Visibility = Visibility.Visible;
            else ZoomUserControl.Visibility = Visibility.Collapsed;
            if (ZoomInteractionMode == MapInteractionMode.Auto || ZoomInteractionMode == MapInteractionMode.GestureAndControl || ZoomInteractionMode == MapInteractionMode.GestureOnly || ZoomInteractionMode == MapInteractionMode.PointerAndKeyboard || ZoomInteractionMode == MapInteractionMode.PointerKeyboardAndControl || ZoomInteractionMode == MapInteractionMode.PointerOnly)
                Map.ZoomInteractionMode = MapInteractionMode.GestureOnly;
            else Map.ZoomInteractionMode = MapInteractionMode.Disabled;

            //if (ClassInfo.DeviceType() == ClassInfo.DeviceTypeEnum.Phone)
            //{
            //    ChangeViewControl.Margin = new Thickness(28, 95, 28, 0);
            //    Searchbar.Margin = new Thickness(28, 40, 28, 0);
            //    DirectionsControl.Margin = new Thickness(28, 0, 28, 0);
            //    MyLocationControl.Margin = new Thickness(28, 28, 28, 28);
            //}
        }

        private async void NetworkInformation_NetworkStatusChanged(object sender)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
            {
                if (!InternalHelper.InternetConnection())
                {
                    Map.TileSources.Clear();
                    Map.TileSources.Add(new MapTileSource(new LocalMapTileDataSource("ms-appdata:///local/MahMaps/mah_x_{x}-y_{y}-z_{zoomlevel}.jpeg")) { AllowOverstretch = false, IsFadingEnabled = SettingsSetters.GetFadeAnimationEnabled() });
                }
                else { ChangeViewControl.UseGoogleMaps(ChangeViewControl.CurrentMapMode, ChangeViewControl.ShowTrafficIsOn, true, ChangeViewControl.AllowOverstretch, ChangeViewControl.FadeAnimationEnabled); }
            });
        }

        private async void ShowStreetsideView()
        {
            // Check if Streetside is supported.
            if (Map.IsStreetsideSupported)
            {
                // Find a panorama near Avenue Gustave Eiffel.
                BasicGeoposition cityPosition = new BasicGeoposition() { Latitude = 48.858, Longitude = 2.295 };
                Geopoint cityCenter = new Geopoint(cityPosition);
                StreetsidePanorama panoramaNearCity = await StreetsidePanorama.FindNearbyAsync(cityCenter);

                // Set the Streetside view if a panorama exists.
                if (panoramaNearCity != null)
                {
                    // Create the Streetside view.
                    StreetsideExperience ssView = new StreetsideExperience(panoramaNearCity);
                    ssView.OverviewMapVisible = true;
                    Map.CustomExperience = ssView;
                }
            }
            else
            {
                // If Streetside is not supported
                ContentDialog viewNotSupportedDialog = new ContentDialog()
                {
                    Title = "Streetside is not supported",
                    Content = "\nStreetside views are not supported on this device.",
                    PrimaryButtonText = "OK"
                };
                await viewNotSupportedDialog.ShowAsync();
            }
        }
        Geopoint GeopointFromPoint(Point point)
        {
            Geopoint geoPoint = null;

            this.Map.GetLocationFromOffset(point, out geoPoint);

            return (geoPoint);
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            MapControl = Map;
            if (e.NavigationMode == NavigationMode.Back)
                ChangeViewControl.Map = Map;
            var AllowOverstretch = SettingsSetters.GetAllowOverstretch();
            var FadeAnimationEnabled = SettingsSetters.GetFadeAnimationEnabled();
            if (InternalHelper.InternetConnection())
            {
                ChangeViewControl.UseGoogleMaps(OnMapControls.ChangeViewUserControl.MapMode.Standard, AllowOverstretch: AllowOverstretch, IsFadingEnabled: FadeAnimationEnabled, ShowTraffic: SettingsSetters.GetShowTrafficOnLaunch());
            }
            else
            {
                Map.TileSources.Add(new MapTileSource(new LocalMapTileDataSource("ms-appdata:///local/MahMaps/mah_x_{x}-y_{y}-z_{zoomlevel}.jpeg")) { AllowOverstretch = false, IsFadingEnabled = FadeAnimationEnabled });
            }
            inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;
            var drawingAttr = this.inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
            drawingAttr.PenTip = PenTipShape.Rectangle;
            drawingAttr.Size = new Size(4, 4);
            drawingAttr.IgnorePressure = true;
            drawingAttr.Color = (Color)Resources["SystemControlBackgroundAccentBrush"];
            this.inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttr);
            if (e.Parameter != null)
            {
                //Google Maps Override
                if (e.Parameter.ToString().StartsWith("http"))
                {
                    //Search Uri association handler
                    if (((Uri)e.Parameter).Segments[2].ToLower() == "search/")
                    {
                        //Searchgrid.PopUP = true;
                        //Searchgrid.SearchText = ((Uri)e.Parameter).DecodeQueryParameters().Where(x => x.Key == "query").FirstOrDefault().Value;
                    }
                    //Directions Uri association handler
                    if (((Uri)e.Parameter).Segments[2].ToLower() == "dir/")
                    {
                        var parameters = ((Uri)e.Parameter).DecodeQueryParameters();
                        var origin = parameters.Where(x => x.Key == "origin").FirstOrDefault();
                        var destination = parameters.Where(x => x.Key == "destination").FirstOrDefault();
                        var travelmode = parameters.Where(x => x.Key == "travelmode").FirstOrDefault();
                        var waypoints = parameters.Where(x => x.Key == "waypoints").FirstOrDefault();
                        ViewModel.DirectionsControls.DirectionsHelper.DirectionModes Mode = ViewModel.DirectionsControls.DirectionsHelper.DirectionModes.walking;
                        Geopoint OriginPoint = null;
                        Geopoint DestinationPoint = null;
                        List<BasicGeoposition> lst = null;
                        if (travelmode.Value != null)
                        {
                            if (travelmode.Value.ToString() == "driving") Mode = ViewModel.DirectionsControls.DirectionsHelper.DirectionModes.driving;
                            else if (travelmode.Value.ToString() == "bicycling ") Mode = ViewModel.DirectionsControls.DirectionsHelper.DirectionModes.bicycling;
                            else if (travelmode.Value.ToString() == "transit") Mode = ViewModel.DirectionsControls.DirectionsHelper.DirectionModes.transit;
                        }
                        if (origin.Value != null)
                        {
                            var latlng = origin.Value.Split(',');
                            var Latitude = Convert.ToDouble(latlng[0]);
                            var Longitude = Convert.ToDouble(latlng[1]);
                            OriginPoint = new Geopoint(new BasicGeoposition()
                            {
                                Latitude = Latitude,
                                Longitude = Longitude
                            });
                        }
                        if (destination.Value != null)
                        {
                            var latlng = destination.Value.Split(',');
                            var Latitude = Convert.ToDouble(latlng[0]);
                            var Longitude = Convert.ToDouble(latlng[1]);
                            DestinationPoint = new Geopoint(new BasicGeoposition()
                            {
                                Latitude = Latitude,
                                Longitude = Longitude
                            });
                        }
                        if (waypoints.Value != null)
                        {
                            lst = new List<BasicGeoposition>();
                            var latlngs = destination.Value.Split('|');
                            foreach (var item in latlngs)
                            {
                                var latlng = item.Split(',');
                                BasicGeoposition point = new BasicGeoposition();
                                point.Latitude = Convert.ToDouble(latlng[0]);
                                point.Longitude = Convert.ToDouble(latlng[1]);
                                lst.Add(point);
                            }
                        }
                        if (OriginPoint != null && DestinationPoint != null)
                        {
                            ViewModel.DirectionsControls.DirectionsHelper.Rootobject Result = null;
                            if (lst == null)
                                Result = await ViewModel.DirectionsControls.DirectionsHelper.GetDirections(OriginPoint.Position, DestinationPoint.Position, Mode);
                            else
                                Result = await ViewModel.DirectionsControls.DirectionsHelper.GetDirections(OriginPoint.Position, DestinationPoint.Position, Mode, lst);
                            if (Result != null)
                            {
                                Map.MapElements.Add(ViewModel.DirectionsControls.DirectionsHelper.GetDirectionAsRoute(Result, (Color)Resources["SystemControlBackgroundAccentBrush"]));
                            }
                        }
                    }
                    //Display a map
                    if (((Uri)e.Parameter).Segments[2].ToLower().StartsWith("@"))
                    {
                        await Task.Delay(1500);
                        try
                        {
                            if (!e.Parameter.ToString().Contains("searchplace"))
                            {
                                var parameters = ((Uri)e.Parameter).DecodeQueryParameters();
                                var mapaction = parameters.Where(x => x.Key == "map_action").FirstOrDefault();
                                if (mapaction.Value != null && mapaction.Value == "pano")
                                {
                                    await new MessageDialog("StreetView Not Supported yet").ShowAsync();
                                }
                                var center = parameters.Where(x => x.Key == "center").FirstOrDefault();
                                var zoom = parameters.Where(x => x.Key == "zoom").FirstOrDefault();
                                var cp = center.Value.Split(',');
                                BasicGeoposition pointer = new BasicGeoposition() { Latitude = Convert.ToDouble(cp[0]), Longitude = Convert.ToDouble(cp[1]) };
                                Map.Center = new Geopoint(pointer);
                                if (zoom.Value != null)
                                    await Map.TryZoomToAsync(Convert.ToDouble(zoom.Value));
                                RunMapRightTapped(Map, new Geopoint(pointer));
                            }
                            else
                            {
                                var search = ((Uri)e.Parameter).ToString().Replace("https://google.com/maps/@searchplace=", "");
                                //var search = parameters.Where(x => x.Key == "searchplace").FirstOrDefault();
                                var res = await ViewModel.PlaceControls.SearchHelper.TextSearch(search, Location: Map.Center, Radius: 15000);
                                if (res == null || res.results.Length == 0)
                                {
                                    await new MessageDialog("No search results found").ShowAsync();
                                    return;
                                }
                                var ploc = res.results.FirstOrDefault().geometry.location;
                                var geopoint = new Geopoint(new BasicGeoposition() { Latitude = ploc.lat, Longitude = ploc.lng });
                                Map.Center = geopoint;
                                await MapView.MapControl.TryZoomToAsync(16);
                                SearchResultPoint = geopoint;
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                //Windows Maps Override
                else
                {
                    var parameters = ((Uri)e.Parameter).DecodeQueryParameters();
                    string cp = "";
                    int zoomlevel = 0;
                    string Querry = "";
                    string Where = "";
                    //{bingmaps:?where=Tabarsi Square%2C north side of the Shrine%2C Mashhad%2C 2399%2C Īrān}
                    if (parameters.Where(x => x.Key == "where").Any())
                        Where = Uri.UnescapeDataString(parameters.Where(x => x.Key == "where").FirstOrDefault().Value.NoHTMLString());
                    if (parameters.Where(x => x.Key == "cp").Any())
                        cp = parameters.Where(x => x.Key == "cp").FirstOrDefault().Value;
                    if (parameters.Where(x => x.Key == "lvl").Any())
                        zoomlevel = Convert.ToInt32(parameters.Where(x => x.Key == "lvl").FirstOrDefault().Value);
                    if (parameters.Where(x => x.Key == "q").Any())
                        Querry = parameters.Where(x => x.Key == "q").FirstOrDefault().Value;
                    if (parameters.Where(x => x.Key == "collection").Any())
                    {
                        var point = parameters.Where(x => x.Key == "collection").FirstOrDefault().Value;
                        var pointargs = point.Split('_');
                        var latitude = pointargs[0].Split('.')[1] + "." + pointargs[0].Split('.')[2];
                        var longitude = pointargs[1];
                        cp = $"{latitude}~{longitude}";
                        if (parameters.Count >= 3)
                            Map.MapElements.Add(new MapIcon() { Location = new Geopoint(new BasicGeoposition() { Latitude = Convert.ToDouble(latitude), Longitude = Convert.ToDouble(longitude) }), Title = pointargs[2].Replace("+", " ") });
                        else
                            Map.MapElements.Add(new MapIcon() { Location = new Geopoint(new BasicGeoposition() { Latitude = Convert.ToDouble(latitude), Longitude = Convert.ToDouble(longitude) }), Title = "Point" });
                    }
                    if (Where != "")
                    {
                        await Task.Delay(500);
                        var res = await SearchHelper.TextSearch(Where);
                        if (res != null)
                        {
                            if (res.results != null && res.results.Any())
                            {
                                var loc = res.results.FirstOrDefault().geometry.location;
                                //var rgc = await ReverseGeoCode.GetLocation(Where);
                                Map.Center = new Geopoint(new BasicGeoposition() { Latitude = loc.lat, Longitude = loc.lng });
                            }
                            else
                            {
                                var rgc = await ReverseGeoCode.GetLocation(Where);
                                Map.Center = rgc;
                            }
                        }
                        else
                        {
                            var rgc = await ReverseGeoCode.GetLocation(Where);
                            Map.Center = rgc;
                        }
                    }
                    if (cp != "")
                    {
                        await Task.Delay(500);
                        var bgp = new BasicGeoposition();
                        bgp.Latitude = Convert.ToDouble(cp.Split('~')[0]);
                        bgp.Longitude = Convert.ToDouble(cp.Split('~')[1]);
                        Map.Center = new Geopoint(bgp);
                        Map.MapElements.Add(new MapIcon() { Location = new Geopoint(bgp), Title = "Point" });
                    }
                    if (zoomlevel != 0) await Map.TryZoomToAsync( zoomlevel );
                    else await MapView.MapControl.TryZoomToAsync(16);
                    if (Querry != "")
                    {
                        await Task.Delay(1500);
                        Searchbar.SearchQuerry = Querry;
                    }
                }
            }

            if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.AcrylicBrush"))
            {
                var ac = new Windows.UI.Xaml.Media.AcrylicBrush();
                var brush = Resources["SystemControlChromeLowAcrylicWindowBrush"] as Windows.UI.Xaml.Media.AcrylicBrush;
                ac = brush;
                ac.TintOpacity = 0.8;
                ac.BackgroundSource = Windows.UI.Xaml.Media.AcrylicBackgroundSource.HostBackdrop;
                InfoPane.PaneBackground = ac;
            }

            //MapViewVM.LoadPage();
        }

        private void Hm_UriRequested(HttpMapTileDataSource sender, MapTileUriRequestedEventArgs args)
        {
            var res = TileCoordinate.ReverseGeoPoint(args.X, args.Y, args.ZoomLevel);
            args.Request.Uri = new Uri($"https://maps.googleapis.com/maps/api/staticmap?center={res.Latitude},{res.Longitude}&zoom={args.ZoomLevel}&maptype=traffic&size=256x256&key={AppCore.GoogleMapAPIKey}", UriKind.RelativeOrAbsolute);
        }
        async void RightTapFunction()
        {
            //try
            //{
            var t = (await SearchHelper.NearbySearch(LastRightTap.Position, 5));
            if (t != null)
            {
                var pic = t.results.Where(x => LastRightTap.DistanceTo(new Geopoint(new BasicGeoposition() { Latitude = x.geometry.location.lat, Longitude = x.geometry.location.lng })) < 1)
                    .OrderBy(x => LastRightTap.DistanceTo(new Geopoint(new BasicGeoposition() { Latitude = x.geometry.location.lat, Longitude = x.geometry.location.lng }))).FirstOrDefault();
                //var pic = t.results.Where(x => x.photos != null).LastOrDefault();
                if (pic != null)
                {
                    LastPlaceID = pic.place_id;
                    if (pic.photos != null)
                    {
                        PlaceImage.Source = new BitmapImage()
                        {
                            UriSource = ViewModel.PhotoControls.PhotosHelper.GetPhotoUri(pic.photos.FirstOrDefault().photo_reference, 350, 350)
                        };
                    }
                    var det = await PlaceDetailsHelper.GetPlaceDetails(pic.place_id);
                    if (det != null)
                    {
                        PlaceAddress.Text = det.result.formatted_address;
                        PlaceName.Text = det.result.name;
                        if (det.result.formatted_phone_number != null)
                        {
                            PlacePhone.Text = det.result.formatted_phone_number;
                            PlacePhoneItem.IsEnabled = true;
                        }
                        if (det.result.website != null)
                        {
                            PlaceWebSite.Text = det.result.website;
                            PlaceWebSiteItem.IsEnabled = true;
                        }
                        if (det.result.opening_hours != null)
                        {
                            var hours = det.result.opening_hours.weekday_text;
                            string MyStr = "Is open : " + det.result.opening_hours.open_now;
                            if (hours != null)
                                foreach (var item in hours)
                                {
                                    MyStr += Environment.NewLine + item;
                                }
                            PlaceOpenNow.Text = MyStr;
                            PlaceOpenNowItem.IsEnabled = true;
                        }
                        PlaceRate.Text = det.result.rating.ToString();
                        PlaceRateItem.IsEnabled = true;
                        if (det.result.reviews != null)
                        {
                            PlaceReviewsItem.ItemsSource = det.result.reviews;
                            PlaceReviewsItem.IsEnabled = true;
                        }
                    }
                    else
                    {
                        PlaceName.Text = pic.name;
                        PlaceAddress.Text = pic.vicinity;
                    }
                }
                //else
                //{
                //    var res = (await GeocodeHelper.GetInfo(Location)).results.FirstOrDefault();
                //    if (res != null)
                //    {
                //        PlaceName.Text = res.address_components.FirstOrDefault().short_name;
                //        PlaceAddress.Text = res.formatted_address;
                //    }
                //    else
                //    {
                //        await new MessageDialog("We didn't find anything here. Maybe an internet connection issue.").ShowAsync();
                //        InfoPane.IsPaneOpen = false;
                //    }
                //}
            }
            else
            {
                await new MessageDialog("We didn't find anything here. Maybe an internet connection issue.").ShowAsync();
                InfoPane.IsPaneOpen = false;
                return;
            }
            //}
            //catch (Exception ex)
            //{
            //    await new MessageDialog("Error in RightTapFunction" + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace).ShowAsync();
            //}
        }
        private async void RunMapRightTapped(MapControl sender, Geopoint Location)
        {
            InfoPane.IsPaneOpen = true;
            LastRightTap = Location;
            await Task.Delay(10);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, RightTapFunction);
        }

        private void Map_MapRightTapped(MapControl sender, MapRightTappedEventArgs args)
        {
            RunMapRightTapped(Map, args.Location);
        }

        private void GetDirections_Click(object sender, RoutedEventArgs e)
        {
            if (DirectionsControl.Origin == null)
            {
                DirectionsControl.Origin = ViewModel.MapViewVM.UserLocation.Location;
            }
            DirectionsControl.Destination = LastRightTap;
            DirectionsControl.DirectionFinder();
            InfoPane.IsPaneOpen = false;
        }

        private void AddWaypoint_Click(object sender, TappedRoutedEventArgs e)
        {
            DirectionsControl.Waypoints.Add(LastRightTap);
            InfoPane.IsPaneOpen = false;
        }

        private void ShareLocation_Click(object sender, TappedRoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
            DataTransferManager.ShowShareUI();
            InfoPane.IsPaneOpen = false;
        }

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            request.Data.SetWebLink(new Uri($"https://www.google.com/maps/@?api=1&map_action=map&center={LastRightTap.Position.Latitude},{LastRightTap.Position.Longitude}&zoom={Convert.ToInt16(Map.ZoomLevel)}",
                UriKind.RelativeOrAbsolute));
            request.Data.Properties.Title = $"{PlaceName.Text} on Google maps";
            request.Data.Properties.Description = $"See {PlaceName.Text} on Google Maps. Shared using WinGo Maps for Windows 10.";
        }

        private async void AddBookmark_Click(object sender, TappedRoutedEventArgs e)
        {
            var c = new BookmarkAddNeedsClass();
            c.Location = LastRightTap;
            c.PlaceName = PlaceName.Text;
            await new BookmarkAdd(c).ShowAsync();
            InfoPane.IsPaneOpen = false;
        }

        private void PlacePhone_Click(object sender, TappedRoutedEventArgs e)
        {
            PhoneCallManager.ShowPhoneCallUI(PlacePhone.Text, PlaceName.Text);
        }

        private async void PlaceWebsite_Click(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(PlaceWebSite.Text, UriKind.RelativeOrAbsolute));
        }

        private void InfoPane_PaneClosed(SplitView sender, object args)
        {
            LastPlaceID = "";
            PlaceRate.Text = "0";
            PlaceRateItem.IsEnabled = false;
            PlaceImage.Source = null;
            PlaceAddress.Text = "";
            PlaceName.Text = "";
            PlaceOpenNow.Text = "";
            PlaceOpenNowItem.IsEnabled = false;
            PlacePhone.Text = "";
            PlacePhoneItem.IsEnabled = false;
            PlaceWebSite.Text = "";
            PlaceWebSiteItem.IsEnabled = false;
            MoreInfoGrid.Visibility = Visibility.Collapsed;
            MoreInfoHyperLink.Visibility = Visibility.Visible;
            PlaceReviewsItem.ItemsSource = null;
            PlaceReviewsItem.IsEnabled = false;
        }

        private void MoreInfoHyperLink_Click(object sender, RoutedEventArgs e)
        {
            MoreInfoGrid.Visibility = Visibility.Visible;
            MoreInfoHyperLink.Visibility = Visibility.Collapsed;
        }

        private void SetOrigin_Click(object sender, TappedRoutedEventArgs e)
        {
            DirectionsControl.Origin = LastRightTap;
        }

        private void Map_MapTapped(MapControl sender, MapInputEventArgs args)
        {
            RunMapRightTapped(Map, args.Location);
        }

        private void InkingBTN_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas.Visibility == Visibility.Collapsed)
            {
                inkCanvas.Visibility = Visibility.Visible;
                DirectionsControl.Visibility = Visibility.Collapsed;
            }
            else
            {
                inkCanvas.Visibility = Visibility.Collapsed;
                DirectionsControl.Visibility = Visibility.Visible;
            }
        }

        private async void RatePlace_Click(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://search.google.com/local/writereview?placeid=" + LastPlaceID));
        }

        private async void AddMissingPlace_Click(object sender, TappedRoutedEventArgs e)
        {
            //Add missing
            //https://www.google.com/maps/@36.2968808,59.5824495,18.73z/data=!10m1!1e2
            var redir = "https://www.google.com/maps/@" + LastRightTap.Position.Latitude + "," + LastRightTap.Position.Latitude + "," + Map.ZoomLevel.ToString("0.00") + "z/data=!10m1!1e2";
            await Launcher.LaunchUriAsync(new Uri(redir));

        }
    }
}
