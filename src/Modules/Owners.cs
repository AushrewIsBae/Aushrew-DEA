﻿using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using DEA.SQLite.Models;
using DEA.SQLite.Repository;

namespace DEA.Modules
{
    public class Owners : ModuleBase<SocketCommandContext>
    {
        [Command("Reset")]
        [RequireBotOwner]
        [Alias("Reset")]
        [Summary("Resets all cooldowns for a specific user.")]
        [Remarks("Reset [@User]")]
        public async Task ResetCooldowns(IGuildUser user = null)
        {
            user = user ?? Context.User as IGuildUser;
            using (var db = new DbContext())
            {
                
                var time = DateTime.Today.AddDays(-5).ToString();
                await UserRepository.ModifyAsync(x => {
                    x.LastWhore = time;
                    x.LastJump = time;
                    x.LastSteal = time;
                    x.LastRob = time;
                    x.LastMessage = time;
                    x.LastWithdraw = time;
                    return Task.CompletedTask;
                    }, Context.User.Id);
                await ReplyAsync($"Successfully reset all of {user.Mention} cooldowns.");
            }
        }

        [Command("Give")]
        [RequireBotOwner]
        [Summary("Inject cash into a user's balance.")]
        [Remarks("Give <@User> <Amount of cash>")]
        public async Task Give(IGuildUser userMentioned, double money) {
            using (var db = new DbContext()) {
                
                await UserRepository.EditOtherCashAsync(Context, userMentioned.Id, +money);
                await ReplyAsync($"Successfully given {money.ToString("C2")} to <@{userMentioned.Id}>.");
            }
        }
    }
}
