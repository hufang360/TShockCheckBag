using System;
using System.Collections.Generic;
using System.Linq;
using TShockAPI;
using TShockAPI.DB;

namespace CheckBag
{
    public class Ban
    {
        static string BanningUser = "CheckBag";
        static Dictionary<string, int> _bans = new();


        /// <summary>
        /// 触发规则
        /// </summary>
        public static int Trigger(string name)
        {
            if (_bans.ContainsKey(name))
            {
                _bans[name]++;
            }
            else
            {
                _bans.Add(name, 1);
            }
            return _bans[name];
        }


        /// <summary>
        /// 移除记录
        /// </summary>
        public static void Remove(string name)
        {
            if (_bans.ContainsKey(name))
            {
                _bans.Remove(name);
            }
        }


        /// <summary>
        /// 添加封禁
        /// </summary>
        public static AddBanResult AddBan(string playerName, string reason, int durationSeconds)
        {
            DateTime expiration = DateTime.UtcNow.AddSeconds(durationSeconds);
            AddBanResult banResult = TShock.Bans.InsertBan("acc:" + playerName, reason, BanningUser, DateTime.UtcNow, expiration);
            if (banResult.Ban != null)
            {
                TShock.Log.Info($"已封禁{playerName}。输入 /ban del {banResult.Ban.TicketNumber} 解除封禁。");
            }
            else
            {
                TShock.Log.Info($"封禁{playerName}失败！原因: {banResult.Message}。");
            }
            return banResult;
        }


        /// <summary>
        /// 列出封禁
        /// </summary>
        public static void ListBans(CommandArgs args)
        {
            var lines = GetBanLines(true);
            if (!lines.Any())
            {
                args.Player.SendInfoMessage("没有生效的封禁，输入/checkbag all可查看全部封禁。");
                return;
            }
            if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber))
            {
                return;
            }
            PaginationTools.SendPage(args.Player, pageNumber, lines, new PaginationTools.Settings
            {
                HeaderFormat = "生效的封禁 ({0}/{1})：",
                FooterFormat = "输入/checkbag list {{0}}查看更多".SFormat(Commands.Specifier)
            });
        }


        /// <summary>
        /// 所有封禁
        /// </summary>
        public static void AllBans(CommandArgs args)
        {
            var lines = GetBanLines(false);
            if (!lines.Any())
            {
                args.Player.SendInfoMessage("无封禁记录！看来没人作弊(*^▽^*)");
                return;
            }
            if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber))
            {
                return;
            }
            PaginationTools.SendPage(args.Player, pageNumber, lines, new PaginationTools.Settings
            {
                HeaderFormat = "全部封禁 ({0}/{1})：",
                FooterFormat = "输入/checkbag all {{0}}查看更多".SFormat(Commands.Specifier)
            });
        }


        /// <summary>
        /// 获得封禁记录
        /// </summary>
        /// <param name="filterInvaid">过滤失效的</param>
        /// <returns></returns>
        static List<string> GetBanLines(bool filterInvaid)
        {
            if (filterInvaid)
            {
                var lines = from ban in TShock.Bans.Bans
                            where (ban.Value.ExpirationDateTime > DateTime.UtcNow) && ban.Value.BanningUser == BanningUser
                            orderby ban.Value.ExpirationDateTime descending
                            select $"{ban.Value.Identifier.Substring(4)}, 原因：{ban.Value.Reason}, 截止：{ban.Value.ExpirationDateTime.ToLocalTime():yyyy-dd-HH hh:mm:ss}, 解封：/ban del {ban.Key}";
                return lines.ToList();
            }
            else
            {
                var lines = from ban in TShock.Bans.Bans
                            where ban.Value.BanningUser == BanningUser
                            orderby ban.Value.ExpirationDateTime descending
                            select $"{ban.Value.Identifier.Substring(4)}, 原因：{ban.Value.Reason}, 截止：{ban.Value.ExpirationDateTime.ToLocalTime():yyyy-dd-HH hh:mm:ss}, 解封：/ban del {ban.Key}";
                return lines.ToList();
            }

        }
    }
}
