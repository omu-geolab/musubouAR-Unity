using System.Collections;
using System.Collections.Generic;
using System.Device.Location;
using UnityEngine;
using JSONModel;

public static class GlobalAR
{ 
    public static float waterLevel = 0.0f;
    public static DataManager dataManager = new DataManager();
    //public static double userLat = 34.705990;
    //public static double userLng = 135.501720;  135.46561131809713,34.71470599836312
    public static double userLat = 34.71470599836312;
    public static double userLng = 135.46561131809713;
    public static double lat2km = 111319.491;
    public static float heading = -67;
    public static float heightAR = 0;
    public static double disCam = 20;
    public static double kNearMsg = 5;
    public static float noticeUpdateTime = 4;
    public static  GeoCoordinate currentlocation = new GeoCoordinate(34.71470599836312, 135.46561131809713);
    public static GeoCoordinate prelocation = new GeoCoordinate(0, 0);
    public static bool isViewDialog = false;
    public static bool isShowedDialog = false;
    public static Feature currentFeature;
    public static string kInfo = "info"; // ????(????)
    public static string kWarn = "warn"; // ????(?x??(?????Q???N??????????))
    public static string kPhoto = "photo"; // ???^
    public static string kMovie = "movie";// ????
    public static bool isWaterAction = false;
    public static bool isFireAction = false;
    public static List<Feature> insideWarn = new List<Feature>();
    public static List<Feature> nearWarn = new List<Feature>();
    public static List<Feature> insideAR = new List<Feature>();
    public static List<Feature> outsideAR = new List<Feature>();
    public static string currentWarn = "";
    public static bool isWorldLoad=false;
    public static bool resetFlag=false;
    public static AnimationMode demoMode = 0;
    public  enum AnimationMode
    {
        None,
        Water,
        Fire,
        Wall,
        Stone,
        Home,
        Lique
    }
}
