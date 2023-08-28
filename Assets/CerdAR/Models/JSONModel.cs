using System;
using System.Collections.Generic;
namespace JSONModel
{
    public class Geometry
    {
        public string type { get; set; }
        public List<double> coordinates { get; set; }
    }

    public class Feature
    {
        public string type { get; set; }
        public Properties properties { get; set; }
        public Geometry geometry { get; set; }
        public string id { get; set; }
    }

    public class Root
    {
        public string type { get; set; }
        public List<Feature> features { get; set; }
    }
#nullable enable
    public class Properties
    {
        public string? icon { get; set; }
        public string? id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public string? info_type { get; set; }
        public string? Name { get; set; }
        public int? range { get; set; }
        public string? start { get; set; }
        public string? stop { get; set; }
        public string? message1 { get; set; }
        public string? message2 { get; set; }
        public int? risk_type { get; set; }
        public string? movie { get; set; }
        public string? pic_type { get; set; }
        public string? photo { get; set; }
        public double? water_level { get; set; }
    }



}

