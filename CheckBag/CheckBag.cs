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
                    "/checkbag list，查看封禁清单（仅生效）",
                    "/checkbag all，查看封禁清单（含失效）",
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
                case "l":
                case "list":
                case "清单":
                    Ban.ListBans(args);
                    break;

                // 全部
                case "all":
                case "a":
                case "全部":
                    Ban.AllBans(args);
                    break;

                // 重载配置
                case "reload":
                case "r":
                case "重载":
                    LoadConfig();
                    args.Player.SendSuccessMessage("[检查背包]重载配置完成");
                    break;

                default:
                    op.SendErrorMessage("语法不正确！输入 /checkbag help 查询用法");
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
                    string tipsDesc = stack > 1 ? "请减少数量" : "请销毁";
                    string opDesc = isCurrent ? "拥有超进度的" : "拥有";
                    if (num < max)
                    {
                        op.SendInfoMessage($"检测到你{opDesc}[i:{id}]{itemDesc}，疑似作弊，{tipsDesc}，若有误判请联系服主！");
                    }
                    else
                    {
                        Ban.Remove(name);
                        op.Disconnect($"你已被封禁！原因：{opDesc}[i:{id}]{itemDesc}。");
                        TSPlayer.All.SendInfoMessage($"{name}已被封禁！原因：{opDesc}[i:{id}]{itemDesc}。");
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
