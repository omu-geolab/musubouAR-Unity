using System;
using UnityEngine;
using System.Device.Location;
public class GeoTool
{
    public static Vector3 ConvertCoordinate(GeoCoordinate currentCoordinate, GeoCoordinate targetCoordinate, float currentHeading, float altitude)
    {
        double distance = currentCoordinate.GetDistanceTo(targetCoordinate); // ????????????
        double bearing = CalculateBearing(currentCoordinate, targetCoordinate); // ?????????????
        // tan(90)
        if (Mathf.Approximately(currentHeading, (float)bearing))
        {
            return new Vector3(0, (float)altitude, (float)distance);
            // tan(-90)
        }
        else if (Mathf.Approximately(currentHeading, (float)-bearing))
        {
            return new Vector3(0, (float)altitude, (float)-distance);
        }
        else
        {
            double angleInRadian = ToRadian(bearing - currentHeading); // ??????????????
            return new Vector3(
                (float)(Math.Sin(angleInRadian) * distance),
                (float)altitude,
                (float)(Math.Cos(angleInRadian) * distance)
            );
        }
    }
    public static Vector3 ConvertCoordinateFromCurrentLocation(GeoCoordinate targetCoordinate, float altitude, float adjust)
    {
        GeoCoordinate currentCoordinate = GlobalAR.currentlocation;
        double distance = currentCoordinate.GetDistanceTo(targetCoordinate) - adjust; // ????????????
        double bearing = CalculateBearing(currentCoordinate, targetCoordinate); // ?????????????
        float currentHeading = GlobalAR.heading;
        // tan(90)
        if (Mathf.Approximately(currentHeading, (float)bearing))
        {
            return new Vector3(0, (float)altitude, (float)distance);
            // tan(-90)
        }
        else if (Mathf.Approximately(currentHeading, (float)-bearing))
        {
            return new Vector3(0, (float)altitude, (float)-distance);
        }
        else
        {
            double angleInRadian = ToRadian(bearing - currentHeading); // ??????????????
            return new Vector3(
                (float)(Math.Sin(angleInRadian) * distance),
                (float)altitude,
                (float)(Math.Cos(angleInRadian) * distance)
            );
        }
    }
    public static Vector3 ConvertGPS2Position(GeoCoordinate targetCoordinate, float altitude, Vector3 adjust)
    {
        GeoCoordinate currentCoordinate = GlobalAR.currentlocation;
        double distance = currentCoordinate.GetDistanceTo(targetCoordinate);
        double bearing = CalculateBearing(currentCoordinate, targetCoordinate); // ?????????????
        float currentHeading = GlobalAR.heading;
        double angleInRadian = ToRadian(bearing); // ??????????????
        return new Vector3(
            (float)(Math.Sin(angleInRadian) * distance + adjust.x),
            (float)altitude,
            (float)(Math.Cos(angleInRadian) * distance + +adjust.z)
        );
    }
    // https://www.movable-type.co.uk/scripts/latlong.html
    public static double CalculateBearing(GeoCoordinate origin, GeoCoordinate target)
    {
        double theta1 = ToRadian(origin.Latitude);
        double theta2 = ToRadian(target.Latitude);
        double lamda1 = ToRadian(origin.Longitude);
        double lamda2 = ToRadian(target.Longitude);
        double y = Math.Sin(lamda2 - lamda1) * Math.Cos(theta2);
        double x = Math.Cos(theta1) * Math.Sin(theta2) - Math.Sin(theta1) * Math.Cos(theta2) * Math.Cos(lamda2 - lamda1);
        double theta = Math.Atan2(y, x);
        double bearing = (ToDegree(theta) + 360) % 360;
        return bearing;
    }
    public static double ToRadian(double degree)
    {
        return degree * Math.PI / 180;
    }
    public static double ToDegree(double radian)
    {
        return radian * 180 / Math.PI;
    }
}