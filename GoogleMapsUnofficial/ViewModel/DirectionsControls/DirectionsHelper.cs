﻿using GoogleMapsUnofficial.ViewModel.VoiceNavigation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI;
using Windows.UI.Xaml.Controls.Maps;
using Windows.Web.Http;

namespace GoogleMapsUnofficial.ViewModel.DirectionsControls
{
    public class DirectionsHelper
    {
        /// <summary>
        /// Available modes for Direction
        /// </summary>
        public enum DirectionModes
        {
            driving, walking, bicycling, transit
        }
        /// <summary>
        /// Get Directions from a Origin to a Destination
        /// </summary>
        /// <param name="Origin">The Origin BasicGeoposition</param>
        /// <param name="Destination">The Destination BasicGeoposition</param>
        /// <param name="Mode">Mode for example Driving, walking or etc.</param>
        /// <param name="WayPoints">Points you want to go in your way</param>
        /// <exception cref="ArgumentOutOfRangeException">Waypoints are not available in transit mode.</exception>
        /// <returns></returns>
        public static async Task<Rootobject> GetDirections(BasicGeoposition Origin, BasicGeoposition Destination, DirectionModes Mode = DirectionModes.driving, List<BasicGeoposition> WayPoints = null)
        {
            try
            {
                if (Mode == DirectionModes.transit && WayPoints != null)
                {
                    throw new ArgumentOutOfRangeException(MultilingualHelpToolkit.GetString("StringWayPointsNotAvailableInTransit", "Text"));
                }
                var m = Mode.ToString();
                var requestUrl = String.Format("http://maps.google.com/maps/api/directions/json?origin=" + Origin.Latitude + "," + Origin.Longitude + "&destination=" + Destination.Latitude + "," + Destination.Longitude + "&units=metric&mode=" + Mode + "&lang=" + AppCore.GoogleMapRequestsLanguage);
                if (WayPoints != null && WayPoints.Count != 0)
                {
                    requestUrl += "&waypoints=";
                    for (int i = 0; i <= WayPoints.Count - 1; i++)
                    {
                        if (i < WayPoints.Count - 1)
                            requestUrl += $"{WayPoints[i].Latitude},{WayPoints[i].Longitude}|";
                        else
                            requestUrl += $"{WayPoints[i].Latitude},{WayPoints[i].Longitude}";
                    }
                }
                //requestUrl += $"&key={AppCore.GoogleMapAPIKey}";
                var http = new HttpClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd(AppCore.HttpUserAgent);
                var s = await http.GetStringAsync(new Uri(requestUrl, UriKind.RelativeOrAbsolute));
                return JsonConvert.DeserializeObject<Rootobject>(s);
            }
            catch (Exception)
            {
                return null;
            }
        }
        /// <summary>
        /// Create a MapPolyLine from RootObject and select FirstOrDefault route to show on map control
        /// </summary>
        /// <param name="FuncResp">Convert first or default route to mappolyline to show on map</param>
        /// <returns></returns>
        public static MapPolyline GetDirectionAsRoute(Rootobject FuncResp, Color ResultColor)
        {
            var loclist = new List<BasicGeoposition>();
            //var points = DecodePolylinePoints(FuncResp.routes.FirstOrDefault().overview_polyline.points);
            var points = DecodePolylinePoints(FuncResp.routes.FirstOrDefault().legs.FirstOrDefault().steps.ToList());
            foreach (var item in points)
            {
                loclist.Add(item);
            }
            //foreach (var leg in FuncResp.routes.FirstOrDefault().legs)
            //{
            //    foreach (var step in leg.steps)
            //    {
            //        loclist.Add(
            //            new BasicGeoposition()
            //            { Latitude = step.start_location.lat, Longitude = step.start_location.lng });
            //        loclist.Add(
            //            new BasicGeoposition()
            //            { Latitude = step.end_location.lat, Longitude = step.end_location.lng });
            //    }
            //}
            MapPolyline line = new MapPolyline()
            {
                StrokeThickness = 5,
                StrokeDashed = true,
                StrokeColor = ResultColor,
                Path = new Geopath(loclist)
            };
            var voice = new VoiceHelper(FuncResp.routes.FirstOrDefault());
            return line;
        }
        /// <summary>
        /// Create a MapPolyLine from Route to show on map control
        /// </summary>
        /// <param name="Route">Selected route for converting to mappolyline</param>
        /// <returns></returns>
        public static MapPolyline GetDirectionAsRoute(Route Route, Color ResultColor)
        {
            var loclist = new List<BasicGeoposition>();
            //var points = DecodePolylinePoints(Route.overview_polyline.points);
            var points = DecodePolylinePoints(Route.legs.FirstOrDefault().steps.ToList());
            foreach (var item in points)
            {
                loclist.Add(item);
            }
            //foreach (var leg in Route.legs)
            //{
            //    foreach (var step in leg.steps)
            //    {
            //        loclist.Add(
            //            new BasicGeoposition()
            //            { Latitude = step.start_location.lat, Longitude = step.start_location.lng });
            //        loclist.Add(
            //            new BasicGeoposition()
            //            { Latitude = step.end_location.lat, Longitude = step.end_location.lng });
            //    }
            //}
            MapPolyline line = new MapPolyline()
            {
                StrokeThickness = 5,
                StrokeDashed = true,
                StrokeColor = ResultColor,
                Path = new Geopath(loclist)
            };
            var voice = new VoiceHelper(Route);
            return line;
        }

        public static string GetDistance(Route Route)
        {
            var Distance = 0;
            foreach (var item in Route.legs)
            {
                Distance += item.distance.value;
            }
            if (SettingsView.SettingsSetters.GetLengthUnit() == 0)
            {
                //Metric
                if (Distance <= 2000)
                    return $"{Distance} {MultilingualHelpToolkit.GetString("StringMeters", "Text")}";
                else return $"{Distance / 1000f} {MultilingualHelpToolkit.GetString("StringKiloMeters", "Text")}";
            }
            else if (SettingsView.SettingsSetters.GetLengthUnit() == 1)
            {
                //Imperial
                if (Distance <= 2000)
                    return $"{string.Format("{0:0.00}", Distance * 1.093613f)} {MultilingualHelpToolkit.GetString("StringYards", "Text")}";
                else return $"{string.Format("{0:0.00}", Distance * 0.000621371f)} {MultilingualHelpToolkit.GetString("StringMiles", "Text")}";
            }
            else
            {
                //US 
                if (Distance <= 2000)
                    return $"{string.Format("{0:0.00}", Distance * 3.28084f)} {MultilingualHelpToolkit.GetString("StringFeet", "Text")}";
                else return $"{string.Format("{0:0.00}", Distance * 0.000621371f)} {MultilingualHelpToolkit.GetString("StringMiles", "Text")}";
            }
        }

        public static string GetTotalEstimatedTime(Route Route)
        {
            var EstimatedTime = 0;
            foreach (var item in Route.legs)
            {
                EstimatedTime += item.duration.value;
            }
            if (Convert.ToInt32((EstimatedTime / 60)) < 60)
                return $"{Convert.ToInt32((EstimatedTime / 60))} {MultilingualHelpToolkit.GetString("StringMinutes", "Text")}";
            else return $"{Convert.ToInt32(((EstimatedTime / 60) / 60))} {MultilingualHelpToolkit.GetString("StringHours", "Text")}";
        }

        public static List<BasicGeoposition> DecodePolylinePoints(string encodedPoints)
        {
            if (encodedPoints == null || encodedPoints == "") return null;
            List<BasicGeoposition> poly = new List<BasicGeoposition>();
            char[] polylinechars = encodedPoints.ToCharArray();
            int index = 0;

            int currentLat = 0;
            int currentLng = 0;
            int next5bits;
            int sum;
            int shifter;

            try
            {
                while (index < polylinechars.Length)
                {
                    // calculate next latitude
                    sum = 0;
                    shifter = 0;
                    do
                    {
                        next5bits = (int)polylinechars[index++] - 63;
                        sum |= (next5bits & 31) << shifter;
                        shifter += 5;
                    } while (next5bits >= 32 && index < polylinechars.Length);

                    if (index >= polylinechars.Length)
                        break;

                    currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                    //calculate next longitude
                    sum = 0;
                    shifter = 0;
                    do
                    {
                        next5bits = (int)polylinechars[index++] - 63;
                        sum |= (next5bits & 31) << shifter;
                        shifter += 5;
                    } while (next5bits >= 32 && index < polylinechars.Length);

                    if (index >= polylinechars.Length && next5bits >= 32)
                        break;

                    currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);
                    BasicGeoposition p = new BasicGeoposition();
                    p.Latitude = Convert.ToDouble(currentLat) / 100000.0;
                    p.Longitude = Convert.ToDouble(currentLng) / 100000.0;
                    poly.Add(p);
                }
            }
            catch
            {
                // logo it
            }
            return poly;
        }
        public static List<BasicGeoposition> DecodePolylinePoints(List<Step> encodedPoints)
        {
            if (encodedPoints == null) return null;
            List<BasicGeoposition> poly = new List<BasicGeoposition>();
            foreach (var encodedPoint in encodedPoints)
            {
                if (encodedPoint != null)
                {
                    char[] polylinechars = encodedPoint.polyline.points.ToCharArray();
                    int index = 0;

                    int currentLat = 0;
                    int currentLng = 0;
                    int next5bits;
                    int sum;
                    int shifter;

                    try
                    {
                        while (index < polylinechars.Length)
                        {
                            // calculate next latitude
                            sum = 0;
                            shifter = 0;
                            do
                            {
                                next5bits = (int)polylinechars[index++] - 63;
                                sum |= (next5bits & 31) << shifter;
                                shifter += 5;
                            } while (next5bits >= 32 && index < polylinechars.Length);

                            if (index >= polylinechars.Length)
                                break;

                            currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                            //calculate next longitude
                            sum = 0;
                            shifter = 0;
                            do
                            {
                                next5bits = (int)polylinechars[index++] - 63;
                                sum |= (next5bits & 31) << shifter;
                                shifter += 5;
                            } while (next5bits >= 32 && index < polylinechars.Length);

                            if (index >= polylinechars.Length && next5bits >= 32)
                                break;

                            currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);
                            BasicGeoposition p = new BasicGeoposition();
                            p.Latitude = Convert.ToDouble(currentLat) / 100000.0;
                            p.Longitude = Convert.ToDouble(currentLng) / 100000.0;
                            poly.Add(p);
                        }
                    }
                    catch
                    {
                        // logo it
                    }
                }
            }
            return poly;
        }

        public class Rootobject
        {
            public Geocoded_Waypoints[] geocoded_waypoints { get; set; }
            public Route[] routes { get; set; }
            public string status { get; set; }
        }

        public class Geocoded_Waypoints
        {
            public string geocoder_status { get; set; }
            public string place_id { get; set; }
            public string[] types { get; set; }
        }

        public class Route
        {
            public Bounds bounds { get; set; }
            public string copyrights { get; set; }
            public Leg[] legs { get; set; }
            public Overview_Polyline overview_polyline { get; set; }
            public string summary { get; set; }
            public string[] warnings { get; set; }
            public object[] waypoint_order { get; set; }
        }

        public class Bounds
        {
            public Northeast northeast { get; set; }
            public Southwest southwest { get; set; }
        }

        public class Northeast
        {
            public float lat { get; set; }
            public float lng { get; set; }
        }

        public class Southwest
        {
            public float lat { get; set; }
            public float lng { get; set; }
        }

        public class Overview_Polyline
        {
            public string points { get; set; }
        }

        public class Leg
        {
            public Distance distance { get; set; }
            public Duration duration { get; set; }
            public string end_address { get; set; }
            public End_Location end_location { get; set; }
            public string start_address { get; set; }
            public Start_Location start_location { get; set; }
            public Step[] steps { get; set; }
            public object[] traffic_speed_entry { get; set; }
            public object[] via_waypoint { get; set; }
        }

        public class Distance
        {
            public string text { get; set; }
            public int value { get; set; }
        }

        public class Duration
        {
            public string text { get; set; }
            public int value { get; set; }
        }

        public class End_Location
        {
            public float lat { get; set; }
            public float lng { get; set; }
        }

        public class Start_Location
        {
            public float lat { get; set; }
            public float lng { get; set; }
        }

        public class Step
        {
            public Distance1 distance { get; set; }
            public Duration1 duration { get; set; }
            public End_Location1 end_location { get; set; }
            public string html_instructions { get; set; }
            public Polyline polyline { get; set; }
            public Start_Location1 start_location { get; set; }
            public Transit_Details transit_details { get; set; }
            public string travel_mode { get; set; }
            public string maneuver { get; set; }
        }

        public class Transit_Details
        {
            public Arrival_Stop arrival_stop { get; set; }
            public Arrival_Time arrival_time { get; set; }
            public Departure_Stop departure_stop { get; set; }
            public Departure_Time departure_time { get; set; }
            public string headsign { get; set; }
            public Line line { get; set; }
            public int num_stops { get; set; }
        }

        public class Arrival_Stop
        {
            public Location location { get; set; }
            public string name { get; set; }
        }

        public class Location
        {
            public float lat { get; set; }
            public float lng { get; set; }
        }

        public class Arrival_Time
        {
            public string text { get; set; }
            public string time_zone { get; set; }
            public int value { get; set; }
        }

        public class Departure_Stop
        {
            public Location location { get; set; }
            public string name { get; set; }
        }

        public class Departure_Time
        {
            public string text { get; set; }
            public string time_zone { get; set; }
            public int value { get; set; }
        }

        public class Line
        {
            public Agency[] agencies { get; set; }
            public string color { get; set; }
            public string name { get; set; }
            public string short_name { get; set; }
            public string text_color { get; set; }
            public Vehicle vehicle { get; set; }
        }

        public class Vehicle
        {
            public string icon { get; set; }
            public string name { get; set; }
            public string type { get; set; }
        }

        public class Agency
        {
            public string name { get; set; }
            public string phone { get; set; }
            public string url { get; set; }
        }


        public class Distance1
        {
            public string text { get; set; }
            public int value { get; set; }
        }

        public class Duration1
        {
            public string text { get; set; }
            public int value { get; set; }
        }

        public class End_Location1
        {
            public float lat { get; set; }
            public float lng { get; set; }
        }

        public class Polyline
        {
            public string points { get; set; }
        }

        public class Start_Location1
        {
            public float lat { get; set; }
            public float lng { get; set; }
        }

    }
}
