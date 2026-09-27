using UnityEngine;
namespace Astra.Performance
{
    // A single registry shared by archive navigation, camera framing and captions.
    public static class CastCatalog
    {
        public static readonly string[] Models={"Lin047","He112","Yu203","Xian318","Ruk509","Fu640"};
        public static readonly string[] Names={"林","赫","余","弦","砾","伏"};
        public static readonly string[] Numbers={"047","112","203","318","509","640"};
        public static readonly string[] Species={"人类 · 维修员","人类 · 旧航路","人类 · 未登记","站立外星人","半兽人","四肢爬行外星人"};
        public static readonly string[] Tabs={"01 林 / 047","02 赫 / 112","03 余 / 203","04 弦 / 站立外星人","05 砾 / 半兽人","06 伏 / 四肢爬行"};
        public static readonly string[] Entries={"lin01","he01","yu01","xian01","ruk01","fu01"};
        public const int Count=6;
        public static bool Crawling(ActorId actor)=>actor==ActorId.Fu;
    }
}
