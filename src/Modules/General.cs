﻿using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using DEA.Database.Models;
using DEA.Database.Repository;
using System.Linq;
using MongoDB.Driver;

namespace DEA.Modules
{
    public class General : ModuleBase<SocketCommandContext>
    {
        [Command("Investments")]
        [Alias("Investements", "Investement", "Investment")]
        [Summary("Increase your money per message")]
        [Remarks("Investments [investment]")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        public async Task Invest(string investString = null)
        {
            var guild = GuildRepository.FetchGuild(Context.Guild.Id);
            var user = UserRepository.FetchUser(Context);
            double cash = user.Cash;
            switch (investString)
            {
                case "line":
                    if (Config.LINE_COST > cash)
                    {
                        await ReplyAsync($"{Context.User.Mention}, you do not have enough money. Balance: {cash.ToString("C", Config.CI)}");
                        break;
                    }
                    if (user.MessageCooldown == Config.LINE_COOLDOWN.TotalMilliseconds)
                    {
                        await ReplyAsync($"{Context.User.Mention}, you have already purchased this investment.");
                        break;
                    }
                    await UserRepository.EditCashAsync(Context, -Config.LINE_COST);
                    UserRepository.Modify(DEABot.UserUpdateBuilder.Set(x => x.MessageCooldown, Config.LINE_COOLDOWN.TotalMilliseconds), Context);
                    await ReplyAsync($"{Context.User.Mention}, don't forget to wipe your nose when you are done with that line.");
                    break;
                case "pound":
                case "lb":
                    if (Config.POUND_COST > cash)
                    {
                        await ReplyAsync($"{Context.User.Mention}, you do not have enough money. Balance: {cash.ToString("C", Config.CI)}");
                        break;
                    }
                    if (user.InvestmentMultiplier >= Config.POUND_MULTIPLIER)
                    {
                        await ReplyAsync($"{Context.User.Mention}, you already purchased this investment.");
                        break;
                    }
                    await UserRepository.EditCashAsync(Context, -Config.POUND_COST);
                    UserRepository.Modify(DEABot.UserUpdateBuilder.Set(x => x.InvestmentMultiplier, Config.POUND_MULTIPLIER), Context);
                    await ReplyAsync($"{Context.User.Mention}, ***DOUBLE CASH SMACK DAB CENTER NIGGA!***");
                    break;
                case "kg":
                case "kilo":
                case "kilogram":
                    if (Config.KILO_COST > cash)
                    {
                        await ReplyAsync($"{Context.User.Mention}, you do not have enough money. Balance: {cash.ToString("C", Config.CI)}");
                        break;
                    }
                    if (user.InvestmentMultiplier != Config.POUND_MULTIPLIER)
                    {
                        await ReplyAsync($"{Context.User.Mention}, you must purchase the pound of cocaine investment before buying this one.");
                        break;
                    }
                    if (user.InvestmentMultiplier >= Config.KILO_MULTIPLIER)
                    {
                        await ReplyAsync($"{Context.User.Mention}, you already purchased this investment.");
                        break;
                    }
                    await UserRepository.EditCashAsync(Context, -Config.KILO_COST);
                    UserRepository.Modify(DEABot.UserUpdateBuilder.Set(x => x.InvestmentMultiplier, Config.KILO_MULTIPLIER), Context);
                    await ReplyAsync($"{Context.User.Mention}, only the black jews would actually enjoy 4$/msg.");
                    break;
                default:
                    var builder = new EmbedBuilder()
                    {
                        Title = "Current Available Investments:",
                        Color = new Color(0x0000FF),
                        Description = ($"\n**Cost: {Config.LINE_COST}$** | Command: `{guild.Prefix}investments line` | Description: " +
                        $"One line of blow. Seems like nothing, yet it's enough to lower the message cooldown from 30 to 25 seconds." +
                        $"\n**Cost: {Config.POUND_COST}$** | Command: `{guild.Prefix}investments pound` | Description: " +
                        $"This one pound of coke will double the amount of cash you get per message\n**Cost: {Config.KILO_COST}$** | Command: " +
                        $"`{guild.Prefix}investments kilo` | Description: A kilo of cocaine is more than enough to " +
                        $"quadruple your cash/message.\n These investments stack with the chatting multiplier. However, they will not stack with themselves."),
                    };
                    await ReplyAsync("", embed: builder);
                    break;
            }
        }

        [Command("Leaderboards")]
        [Alias("lb", "rankings", "highscores", "leaderboard", "highscore")]
        [Summary("View the richest Drug Traffickers.")]
        [Remarks("Leaderboards")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        public async Task Leaderboards()
        {
            var users = DEABot.Users.Find(x => x.GuildId == Context.Guild.Id).ToList();
            var sorted = users.OrderByDescending(x => x.Cash);
            string description = "";
            int position = 1;

            foreach (User user in sorted)
            {
                if (Context.Guild.GetUser(user.UserId) == null)
                {
                    await DEABot.Users.DeleteOneAsync(x => x.Id == user.Id);
                    continue;
                }

                description += $"{position}. <@{user.UserId}>: {user.Cash.ToString("C", Config.CI)}\n";
                if (position >= Config.LEADERBOARD_CAP) break;
                position++;
            }

            var builder = new EmbedBuilder()
            {
                Title = $"The Richest Traffickers",
                Color = new Color(0x00AE86),
                Description = description
            };

            await ReplyAsync("", embed: builder);
        }

        [Command("Rates")]
        [Alias("highestrate", "ratehighscore", "bestrate", "highestrates", "ratelb", "rateleaderboards")]
        [Summary("View the richest Drug Traffickers.")]
        [Remarks("Rates")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        public async Task Chatters()
        {
            var users = DEABot.Users.Find(y => y.GuildId == Context.Guild.Id).ToList();
            var sorted = users.OrderByDescending(x => x.TemporaryMultiplier);
            string description = "";
            int position = 1;

            foreach (User user in sorted)
            {
                if (Context.Guild.GetUser(user.UserId) == null)
                {
                    await DEABot.Users.DeleteOneAsync(x => x.Id == user.Id);
                    continue;
                }

                description += $"{position}. <@{user.UserId}>: {user.TemporaryMultiplier.ToString("N2")}\n";
                if (position >= Config.RATELB_CAP) break;
                position++;
            }

            var builder = new EmbedBuilder()
            {
                Title = $"The Best Chatters",
                Color = new Color(0x00AE86),
                Description = description
            };

            await ReplyAsync("", embed: builder);
        }

        [Command("Donate")]
        [Alias("Sauce")]
        [Summary("Sauce some cash to one of your mates.")]
        [Remarks("Donate <@User> <Amount of cash>")]
        public async Task Donate(IGuildUser userMentioned, double money)
        {
            var user = UserRepository.FetchUser(Context);
            if (userMentioned.Id == Context.User.Id) throw new Exception("Hey kids! Look at that retard, he is trying to give money to himself!");
            if (money < Config.DONATE_MIN) throw new Exception($"Lowest donation is {Config.DONATE_MIN}$.");
            if (user.Cash < money) throw new Exception($"You do not have enough money. Balance: {user.Cash.ToString("C", Config.CI)}.");
            await UserRepository.EditCashAsync(Context, -money);
            double deaMoney = money * Config.DEA_CUT / 100;
            await UserRepository.EditCashAsync(Context, userMentioned.Id, money - deaMoney);
            await UserRepository.EditCashAsync(Context, Context.Guild.CurrentUser.Id, deaMoney);
            await ReplyAsync($"Successfully donated {money.ToString("C", Config.CI)} to {userMentioned.Mention}. DEA has taken a {deaMoney.ToString("C", Config.CI)} cut out of this donation. Balance: {(user.Cash + money - deaMoney).ToString("C", Config.CI)}.");
        }

        [Command("Rank")]
        [Summary("View the detailed ranking information of any user.")]
        [Remarks("Rank [@User]")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        public async Task Rank([Remainder] IGuildUser userToView = null)
        {
            userToView = userToView ?? Context.User as IGuildUser;
            var users = DEABot.Users.Find(y => y.GuildId == Context.Guild.Id).ToList();
            var sorted = users.OrderByDescending(x => x.Cash).ToList();
            IRole rank = null;
            rank = RankHandler.FetchRank(Context);
            var builder = new EmbedBuilder()
            {
                Title = $"Ranking of {userToView}",
                Color = new Color(0x00AE86),
                Description = $"Balance: {UserRepository.FetchUser(userToView.Id, userToView.GuildId).Cash.ToString("C", Config.CI)}\n" +
                              $"Position: #{sorted.FindIndex(x => x.UserId == userToView.Id) + 1}\n"
            };
            if (rank != null)
                builder.Description += $"Rank: {rank.Mention}";
            await ReplyAsync("", embed: builder);
        }

        [Command("Money")]
        [Alias("Cash", "Balance", "Bal")]
        [Summary("View the wealth of anyone.")]
        [Remarks("Money [@User]")]
        public async Task Money([Remainder] IGuildUser userToView = null)
        {
            userToView = userToView ?? Context.User as IGuildUser;
            var builder = new EmbedBuilder()
            {
                Title = $"{userToView}'s balance: {(UserRepository.FetchUser(userToView.Id, Context.Guild.Id)).Cash.ToString("C", Config.CI)}.",
                Color = new Color(0x00AE86),
            };
            await ReplyAsync("", embed: builder);
        }

        [Command("Ranks")]
        [Summary("View all ranks.")]
        [Remarks("Ranks")]
        public async Task Ranks()
        {
            var guild = GuildRepository.FetchGuild(Context.Guild.Id);
            if (guild.RankRoles == null) throw new Exception("There are no ranks yet!");
            var description = "";
            foreach (var rank in guild.RankRoles.OrderBy(x => x.Value))
            {
                var role = Context.Guild.GetRole(Convert.ToUInt64(rank.Name));
                if (role == null)
                {
                    guild.RankRoles.Remove(rank.Name);
                    GuildRepository.Modify(DEABot.GuildUpdateBuilder.Set(x => x.RankRoles, guild.RankRoles), Context.Guild.Id);
                    continue;
                }
                description += $"{rank.Value.AsDouble.ToString("C", Config.CI)}: {role.Mention}\n";
            }
            if (description.Length > 2048) throw new Exception("You have too many ranks to be able to use this command.");
            var builder = new EmbedBuilder()
            {
                Title = "Ranks",
                Color = new Color(0x00AE86),
                Description = description
            };
            await ReplyAsync("", embed: builder);
        }

        [Command("ModRoles")]
        [Alias("ModeratorRoles", "ModRole")]
        [Summary("View all the moderator roles.")]
        [Remarks("ModRoles")]
        public async Task ModRoles()
        {
            var guild = GuildRepository.FetchGuild(Context.Guild.Id);
            if (guild.ModRoles == null) throw new Exception("There are no moderator roles yet!");
            var description = "";
            foreach (var modRole in guild.ModRoles.OrderBy(x => x.Value))
            {
                var role = Context.Guild.GetRole(Convert.ToUInt64(modRole.Name));
                if (role == null)
                {
                    guild.ModRoles.Remove(modRole.Name);
                    GuildRepository.Modify(DEABot.GuildUpdateBuilder.Set(x => x.ModRoles, guild.ModRoles), Context.Guild.Id);
                    continue;
                }
                description += $"{role.Mention}: Pemission level {modRole.Value}";
            }
            if (description.Length > 2048) throw new Exception("You have too many mod roles to be able to use this command.");
            var builder = new EmbedBuilder()
            {
                Title = "Moderator Roles",
                Color = new Color(0x00AE86),
                Description = description
            };
            await ReplyAsync("", embed: builder);
        }

        [Command("Rate")]
        [Summary("View the money/message rate of anyone.")]
        [Remarks("Rate [@User]")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        public async Task Rate(IGuildUser userToView = null)
        {
            userToView = userToView ?? Context.User as IGuildUser;
            var user = UserRepository.FetchUser(Context);
            var builder = new EmbedBuilder()
            {
                Title = $"Rate of {userToView}",
                Color = new Color(0x00AE86),
                Description = $"Chatting multiplier: {user.TemporaryMultiplier.ToString("N2")}\n" +
                $"Investment multiplier: {user.InvestmentMultiplier.ToString("N2")}\n" + 
                $"Message cooldown: {user.MessageCooldown / 1000} seconds"
            };
            await ReplyAsync("", embed: builder);
        }
    }
}
