using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace CheckBag
{
    [ApiVersion(2, 1)]
    public class CheckBag : TerrariaPlugin
    {
        public override string Name => "检查背包";
        public override string Author => "hufang360";
        public override string Description => "定时检查玩家背包，并封禁对应玩家。";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        string SaveDir = Path.Combine(TShock.SavePath, "CheckBag");
        Config _config;
        int Count = -1;

        public CheckBag(Main game) : base(game)
        {
        }


        /// <summary>
        /// 初始化
        /// </summary>
        public override void Initialize()
        {
            if (!Directory.Exists(SaveDir))
            {
                Directory.CreateDirectory(SaveDir);
            }
            LoadConfig();

            Commands.ChatCommands.Add(new Command("checkbag", CBCommand, "checkbag", "检查背包") { HelpText = "检查背包" });
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        }


        /// <summary>
        /// 加载配置文件
        /// </summary>
        void LoadConfig()
        {
            _config = Config.Load(Path.Combine(SaveDir, "config.json"));
        }

        /// <summary>
        /// 处理指令
        /// </summary>
        void CBCommand(CommandArgs args)
        {
            TSPlayer op = args.Player;
            void Help()
            {
                List<string> lines = new()
                {
                    "/checkbag ban，列出封禁记录",
                    "/checkbag item，列出违规物品",
                    "/checkbag reload，重载配置",
                };
                op.SendInfoMessage(string.Join("\n", lines));
            }

            if (args.Parameters.Count == 0)
            {
                op.SendErrorMessage("语法错误，输入 /checkbag help 查询用法");
                return;
            }

            switch (args.Parameters[0].ToLowerInvariant())
            {
                // 帮助
                case "help":
                case "帮助":
                    Help();
                    break;

                // 查看封禁
                case "ban":
                case "b":
                    Ban.ListBans(args);
                    break;

                // 物品
                case "item":
                case "i":
                    ListItems(args);
                    break;

                // 重载配置
                case "reload":
                case "r":
                    LoadConfig();
                    args.Player.SendSuccessMessage("[检查背包]重载配置完成");
                    break;

                default:
                    op.SendErrorMessage("语法不正确！输入/checkbag help查询用法");
                    break;
            }
        }


        /// <summary>
        /// 世界更新（有玩家时，每秒更新60次）
        /// </summary>
        void OnGameUpdate(EventArgs args)
        {
            if (Count != -1 && Count < _config.检测间隔 * 60)
            {
                Count++;
                return;
            }
            Count = 0;

            TShock.Players.Where(p => p != null && p.Active).ToList().ForEach(p =>
            {
                ScanPlayer(p);
            });
        }


        /// <summary>
        /// 检查玩家背包
        /// </summary>
        void ScanPlayer(TSPlayer op)
        {
            Player plr = op.TPlayer;
            Dictionary<int, int> dict = new();
            List<Item> list = new();
            list.AddRange(plr.inventory); // 背包,钱币/弹药,手持
            list.Add(plr.trashItem); // 垃圾桶
            list.AddRange(plr.armor); // 装备,时装
            list.AddRange(plr.dye); // 染料
            list.AddRange(plr.miscEquips); // 工具栏
            list.AddRange(plr.miscDyes); // 工具栏染料
            list.AddRange(plr.bank.item); // 储蓄罐
            list.AddRange(plr.bank2.item); // 保险箱
            list.AddRange(plr.bank3.item); // 护卫熔炉
            list.AddRange(plr.bank4.item); // 虚空保险箱
            for (int i = 0; i < plr.Loadouts.Length; i++)
            {
                // 装备1,装备2,装备3
                list.AddRange(plr.Loadouts[i].Armor); // 装备,时装
                list.AddRange(plr.Loadouts[i].Dye); // 染料
            }

            list.Where(item => item != null && item.active).ToList().ForEach(item =>
            {
                if (dict.ContainsKey(item.netID))
                {
                    dict[item.netID] += item.stack;
                }
                else
                {
                    dict.Add(item.netID, item.stack);
                }
            });

            bool Check(List<ItemData> li, bool isCurrent)
            {
                ItemData data = null;
                foreach (var d in li)
                {
                    var id = d.id;
                    var stack = d.数量;
                    if (dict.ContainsKey(id) && dict[id] >= stack)
                    {
                        data = d;
                        break;
                    }
                }

                if (data != null)
                {
                    var name = op.Name;
                    var id = data.id;
                    var stack = data.数量;
                    var max = _config.违规次数;
                    var num = Ban.Trigger(name);
                    string itemName = Lang.GetItemNameValue(id);
                    string itemDesc = stack > 1 ? $"{itemName}x{stack}" : itemName;
                    string opDesc = isCurrent ? "拥有超进度的" : "拥有";
                    var desc = $"{opDesc}[i/s{stack}:{id}]{itemDesc}";
                    if (num < max)
                    {
                        string tips = stack > 1 ? "请减少数量" : "请销毁";
                        op.SendInfoMessage($"检测到你{desc}，疑似作弊，{tips}，若有误判请联系服主！");
                    }
                    else
                    {
                        Ban.Remove(name);
                        TSPlayer.All.SendInfoMessage($"{name}已被封禁！原因：{desc}。");
                        op.Disconnect($"你已被封禁！原因：{opDesc}{itemDesc}。");
                        Ban.AddBan(name, $"{opDesc}{itemDesc}", _config.封禁时长 * 60);
                        return false;
                    }
                }
                return true;
            }

            if (!Check(_config.全时期, false))
                return;
            Check(_config.Current(), true);
        }

        /// <summary>
        /// 列出违规物品
        /// </summary>
        public void ListItems(CommandArgs args)
        {
            static string FormatData(ItemData data)
            {
                var id = data.id;
                var stack = data.数量;
                var itemName = Lang.GetItemNameValue(id);
                var itemDesc = stack > 1 ? $"{itemName}x{stack}" : itemName;
                return $"[i/s{stack}:{id}]{itemDesc}";
            }

            var lines = new List<string>();
            var datas = _config.全时期;
            var lines2 = datas.Select(d => FormatData(d)).ToList();
            lines.AddRange(WarpLines(lines2));
            if (datas.Count > 0)
            {
                lines[0] = "全时期：" + lines[0];
            }

            datas = _config.Current();
            lines2 = datas.Select(d => FormatData(d)).ToList();
            lines.AddRange(WarpLines(lines2));
            if (datas.Count > 0)
            {
                var curIndex = _config.全时期.Count;
                lines[curIndex] = "当前进度：" + lines[curIndex];
            }

            if (!lines.Any())
            {
                if (_config.IsEmpty())
                {
                    args.Player.SendInfoMessage("你未配置任何违规物品数据！");
                }
                else
                {
                    args.Player.SendInfoMessage("没有符合当前进度的物品！");
                }
                return;
            }

            if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber))
            {
                return;
            }
            PaginationTools.SendPage(args.Player, pageNumber, lines, new PaginationTools.Settings
            {
                HeaderFormat = "违规物品 ({0}/{1})：",
                FooterFormat = "输入/checkbag item {{0}}查看更多".SFormat(Commands.Specifier)
            });
        }

        /// <summary>
        /// 将字符串换行
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="column">列数，1行显示多个</param>
        /// <returns></returns>
        static List<string> WarpLines(List<string> lines, int column = 5)
        {
            List<string> li1 = new();
            List<string> li2 = new();
            foreach (var line in lines)
            {
                if (li2.Count % column == 0)
                {
                    if (li2.Count > 0)
                    {
                        li1.Add(string.Join(", ", li2));
                        li2.Clear();
                    }
                }
                li2.Add(line);
            }
            if (li2.Any())
            {
                li1.Add(string.Join(", ", li2));
            }
            return li1;
        }

        /// <summary>
        /// 销毁
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            }
            base.Dispose(disposing);
        }

    }
}
