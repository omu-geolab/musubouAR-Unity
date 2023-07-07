using System;
using System.Collections.Generic;
using JSONModel;
using System.Diagnostics;
public class DataManager
{
    public List<Feature> infoBox = new List<Feature>(); // 情報タグ用
    public List<Feature> warnBox = new List<Feature>(); // 警告タグ用
                                             // 定数
    string kInfo = "info"; // 種別(情報)
    string kWarn = "warn"; // 種別(警告(今災害が起こっている))
    string kPhoto = "photo"; // 写真
    string kMovie = "movie"; // 動画
    public bool hasData = false;

    public void storeData(Root root)
    {
        var features = root.features;
        if (features != null)
        {
            for (int i = 0; i < features.Count; i++)
            {
                if (features[i].properties != null &&
                    features[i].properties.info_type != null)
                {
                    if (features[i].properties.info_type == kInfo)
                    {
                        features[i].id = Guid.NewGuid().ToString();
                        infoBox.Add(features[i]);
                    }else if (features[i].properties.info_type == kWarn)
                    {
                        features[i].id = Guid.NewGuid().ToString();
                        if (features[i].properties.water_level == null)
                        {
                            features[i].properties.water_level = 1.0;
                        }

                        DateTime dateTime;

                        if (DateTime.TryParse(features[i].properties.stop, out dateTime))
                        {
                            if (DateTime.Now < dateTime)
                            {
                                warnBox.Add(features[i]);
                            }
                        }
                
                        
                    }
                }
               
            }
            hasData = true;
        }
    }
}
