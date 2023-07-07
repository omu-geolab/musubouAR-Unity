using System;
public class TagAR
{
    // 共通タグ
    public string commonId { get; set; }    // 共通ID(必要なのか？)
    public string name { get; set; }     // タグの名前
    public string inforType { get; set; }   // 種別(info or warn)
    public string icon { get; set; }    // 使用する画像
    public string descript { get; set; }   // 内容の解説文
    public double lat { get; set; }     // 緯度
    public double lon { get; set; }       // 経度

    public double direction { get; set; }  // 各目的地への方角

    public int pinNum { get; set; }     // ピン番号
    public double elevation { get; set; }

    public double distance { get; set; }    // 現在地から目的地までの距離

    // 情報の独自タグ
    public string picType { get; set; }    // 写真か動画か
    public string photo { get; set; }     // 写真のURL
    public string movie { get; set; }       // 動画のURL

    // 災害の独自タグ
    public int range { get; set; }        // 災害の範囲
    public DateTime start { get; set; }      // 災害の開始時間
    public DateTime stop { get; set; }        // 災害の終了時間
    public string message1 { get; set; }   // 警告範囲に近づいてきた時のメッセージ
    public string message2 { get; set; }  // 警告範囲に侵入した時のメッセージ
    public int riskType { get; set; }       // 災害の種類(0:火災,1:浸水,2:落橋,3:土砂崩れ)
    public bool isFullRange { get; set; } // 災害の範囲拡大フラグ
}
