using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Terraria;

namespace CheckBag
{
    public class Config
    {
        public string 配置说明 = "检测间隔为xx秒。封禁时长为xx分钟。物品名称仅作参考，可不写。";
        public string 物品查询 = "https://terraria.wiki.gg/zh/wiki/Item_IDs";
        public int 检测间隔 = 60;  // 秒
        public int 封禁时长 = 60; // 分钟
        public int 违规次数 = 3;  // 次
        public List<ItemData> 全时期 = new();

        public List<ItemData> 骷髅王前 = new();
        public List<ItemData> 肉前 = new();
        public List<ItemData> 一王前 = new();
        public List<ItemData> 三王前 = new();
        public List<ItemData> 猪鲨前 = new();
        public List<ItemData> 光女前 = new();
        public List<ItemData> 花前 = new();
        public List<ItemData> 石前 = new();


        /// <summary>
        /// 默认配置
        /// </summary>
        public void Init()
        {
            全时期 = new List<ItemData> {
                new ItemData(74, 999, "铂金币")
            };

            骷髅王前 = new List<ItemData> {
                new ItemData(29, 20, "生命水晶")
            };

            一王前 = new List<ItemData> {
                new ItemData(1291, 2, "生命果"),
            };

            三王前 = new List<ItemData> {
                new ItemData(1006, 2, "叶绿锭"),
            };

            猪鲨前 = new List<ItemData> {
                new ItemData(2622, 1, "利刃台风"),
            };

            光女前 = new List<ItemData> {
                new ItemData(5055, 1, "泰拉棱镜"),
            };

            石前 = new List<ItemData> {
                new ItemData(4956, 1, "天顶剑"),
                new ItemData(3381, 1, "星尘头盔"),
                new ItemData(3382, 1, "星尘板甲"),
                new ItemData(3383, 1, "星尘护腿"),
            };
        }

        /// <summary>
        /// 超进度物品
        /// </summary>
        public List<ItemData> Current()
        {
            List<ItemData> list = new();
            if (!NPC.downedBoss3) list.AddRange(骷髅王前);
            if (!Main.hardMode) list.AddRange(肉前);
            if (!NPC.downedMechBossAny) list.AddRange(一王前);
            if (!NPC.downedMechBoss1 || !NPC.downedMechBoss2 || !NPC.downedMechBoss3) list.AddRange(三王前);
            if (!NPC.downedFishron) list.AddRange(猪鲨前);
            if (!NPC.downedEmpressOfLight) list.AddRange(光女前);
            if (!NPC.downedPlantBoss) list.AddRange(花前);
            if (!NPC.downedGolemBoss) list.AddRange(石前);
            return list;
        }

        /// <summary>
        /// 违规物品记录是否为空（会排除全时期）
        /// </summary>
        public bool IsEmpty()
        {
            if (
                骷髅王前.Count +
                肉前.Count +
                一王前.Count +
                三王前.Count +
                猪鲨前.Count +
                光女前.Count +
                花前.Count +
                石前.Count > 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 加载配置文件（文件不存在时会自动创建）
        /// </summary>
        public static Config Load(string path)
        {
            if (File.Exists(path))
            {
                return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path), new JsonSerializerSettings()
                {
                    Error = (sender, error) => error.ErrorContext.Handled = true
                });
            }
            else
            {
                Config con = new();
                con.Init();
                File.WriteAllText(path, JsonConvert.SerializeObject(con, Formatting.Indented, new JsonSerializerSettings()
                {
                    DefaultValueHandling = DefaultValueHandling.Ignore
                }));
                return con;
            }
        }


    }


    public class ItemData
    {
        public int id = 0;

        public int 数量 = 1;

        public string 名称 = "";

        public ItemData()
        {

        }

        public ItemData(int _id, int stack, string name = "")
        {
            id = _id;
            数量 = stack;
            名称 = name;
        }
    }
}