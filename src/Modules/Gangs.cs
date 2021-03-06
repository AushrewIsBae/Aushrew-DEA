﻿using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using DEA.Database.Repository;
using System.Linq;
using DEA.Database.Models;
using MongoDB.Driver;

namespace DEA.Modules
{
    public class Gangs : ModuleBase<SocketCommandContext>
    {

        [Command("CreateGang")]
        [Require(Attributes.NoGang)]
        [Summary("Allows you to create a gang at a hefty price.")]
        [Remarks("Create <Name>")]
        public async Task ResetCooldowns([Remainder] string name)
        {
            var user = UserRepository.FetchUser(Context);
            if (user.Cash < Config.GANG_CREATION_COST)
                throw new Exception($"You do not have {Config.GANG_CREATION_COST.ToString("C", Config.CI)}. Balance: {user.Cash.ToString("C", Config.CI)}.");
            var gang = GangRepository.CreateGang(Context.User.Id, Context.Guild.Id, name);
            await UserRepository.EditCashAsync(Context, -Config.GANG_CREATION_COST);
            await ReplyAsync($"{Context.User.Mention}, You have successfully created the {gang.Name} gang!");
        }

        [Command("AddGangMember")]
        [Require(Attributes.InGang, Attributes.GangLeader)]
        [Summary("Allows you to add a member to your gang.")]
        [Remarks("AddGangMember <@GangMember>")]
        public async Task AddToGang(IGuildUser user)
        {
            if (GangRepository.InGang(user.Id, Context.Guild.Id)) throw new Exception("This user is already in a gang.");
            if (GangRepository.IsFull(Context.User.Id, Context.Guild.Id)) throw new Exception("Your gang is already full!");
            GangRepository.AddMember(Context.User.Id, Context.Guild.Id, user.Id);
            await ReplyAsync($"{user} is now a new member of your gang!");
            var channel = await user.CreateDMChannelAsync();
            await channel.SendMessageAsync($"Congrats! You are now a member of {GangRepository.FetchGang(Context).Name}!");
        }

        /*[Command("JoinGang", RunMode = RunMode.Async)]
        [Require("nogang")]
        [Summary("Allows you to request to join a gang.")]
        [Remarks("JoinGang <@GangMember>")]
        private async Task JoinGang(IGuildUser user)
        {
            if (!GangRepository.InGang(user.Id, Context.Guild.Id)) throw new Exception("This user is not in a gang.");
            if (GangRepository.IsFull(user.Id, Context.Guild.Id)) throw new Exception("This gang is already full!");
            var gang = GangRepository.FetchGang(user.Id, user.GuildId);
            var channel = await user.CreateDMChannelAsync();
            await channel.SendMessageAsync($"{Context.User} has requested to join your gang. Reply with \"agree\" within the next 30 seconds to accept this request.");
            await ReplyAsync($"{Context.User.Mention}, The leader of {gang.Name} has been successfully informed of your request to join.");
            var response = await WaitForMessage(Context.User, Context.Channel, TimeSpan.FromSeconds(30));
            await ReplyAsync($"{response.Content}");
            if (response.Content.ToLower() == "agree")
            {
                GangRepository.AddMember(gang.LeaderId, user.GuildId, Context.User.Id);
                await channel.SendMessageAsync($"{Context.User} is now a new member of your gang!");
                var informingChannel = await Context.User.CreateDMChannelAsync();
                await informingChannel.SendMessageAsync($"{Context.User.Mention}, Congrats! You are now a member of {gang.Name}!");
            }
        }*/

        [Command("Gang")]
        [Summary("Gives you all the info about any gang.")]
        [Remarks("Gang [Gang name]")]
        public async Task Gang([Remainder] string gangName = null)
        {
            if (gangName == null && !GangRepository.InGang(Context.User.Id, Context.Guild.Id)) throw new Exception($"You are not in a gang.");
            Gang gang;
            if (gangName == null) gang = GangRepository.FetchGang(Context);
            else gang = GangRepository.FetchGang(gangName, Context.Guild.Id);
            var members = "";
            var leader = "";
            if (Context.Guild.GetUser(gang.LeaderId) != null) leader = $"<@{gang.LeaderId}>";
            foreach (var member in gang.Members)
                if (Context.Guild.GetUser(member) != null) members += $"<@{member}>, ";
            var InterestRate = 0.025f + ((gang.Wealth / 100) * .000075f);
            if (InterestRate > 0.1) InterestRate = 0.1f;
            if (members.Length > 2) members = $"__**Members:**__ {members.Substring(0, members.Length - 2)}\n";
            var builder = new EmbedBuilder()
            {
                Title = gang.Name,
                Color = new Color(0x00AE86),
                Description = $"__**Leader:**__ {leader}\n" +
                              members +
                              $"__**Wealth:**__ {gang.Wealth.ToString("C", Config.CI)}\n" +
                              $"__**Interest rate:**__ {InterestRate.ToString("P")}"
            };
            await ReplyAsync("", embed: builder);
        }

        [Command("GangLb")]
        [Alias("gangs")]
        [Summary("Shows the wealthiest gangs.")]
        [Remarks("Gangs")]
        public async Task Ganglb()
        {
            var gangs = await DEABot.Gangs.FindAsync(y => y.GuildId == Context.Guild.Id);
            var sortedGangs = (await gangs.ToListAsync()).OrderByDescending(x => x.Wealth).ToList();
            string message = "```asciidoc\n= The Wealthiest Gangs =\n";
            int longest = 0;

            for (int i = 0; i < sortedGangs.Count(); i++)
            {
                if (i + 1 >= Config.GANGSLB_CAP) break;
                if (sortedGangs[i].Name.Length > longest) longest = $"{i + 1}. {sortedGangs[i].Name}".Length;
            }

            for (int i = 0; i < sortedGangs.Count(); i++)
            {
                if (i + 1 >= Config.GANGSLB_CAP) break;
                message += $"{i + 1}. {sortedGangs[i].Name}".PadRight(longest + 2) + $" :: {sortedGangs[i].Wealth.ToString("C", Config.CI)}\n";
            }

            await ReplyAsync($"{message}```");
        }

        [Command("LeaveGang")]
        [Require(Attributes.InGang)]
        [Summary("Allows you to break all ties with a gang.")]
        [Remarks("LeaveGang")]
        public async Task LeaveGang()
        {
            var gang = GangRepository.FetchGang(Context);
            var prefix = GuildRepository.FetchGuild(Context.Guild.Id).Prefix;
            if (gang.LeaderId == Context.User.Id)
                throw new Exception($"You may not leave a gang if you are the owner. Either destroy the gang with the `{prefix}DestroyGang` command, or " +
                                    $"transfer the ownership of the gang to another member with the `{prefix}TransferLeadership` command.");
            GangRepository.RemoveMember(Context.User.Id, Context.Guild.Id);
            await ReplyAsync($"{Context.User.Mention}, You have successfully left {gang.Name}");
            var channel = await Context.Client.GetUser(gang.LeaderId).CreateDMChannelAsync();
            await channel.SendMessageAsync($"{Context.User} has left {gang.Name}.");
        }

        [Command("KickGangMember")]
        [Require(Attributes.InGang, Attributes.GangLeader)]
        [Summary("Kicks a user from your gang.")]
        [Remarks("KickGangMember")]
        public async Task KickFromGang([Remainder] IGuildUser user)
        {
            if (!GangRepository.IsMemberOf(Context.User.Id, Context.Guild.Id, user.Id))
                throw new Exception("This user is not a member of your gang!");
            var gang = GangRepository.FetchGang(Context);
            GangRepository.RemoveMember(user.Id, Context.Guild.Id);
            await ReplyAsync($"{Context.User.Mention}, You have successfully kicked {user} from {gang.Name}");
            var channel = await user.CreateDMChannelAsync();
            await channel.SendMessageAsync($"You have been kicked from {gang.Name}.");
        }

        [Command("DestroyGang")]
        [Require(Attributes.InGang, Attributes.GangLeader)]
        [Summary("Destroys a gang entirely taking down all funds with it.")]
        [Remarks("DestroyGang")]
        public async Task DestroyGang()
        {
            GangRepository.DestroyGang(Context.User.Id, Context.Guild.Id);
            await ReplyAsync($"{Context.User.Mention}, You have successfully destroyed your gang.");
        }

        [Command("ChangeGangName")]
        [Alias("ChangeName")]
        [Require(Attributes.InGang, Attributes.GangLeader)]
        [Summary("Changes the name of your gang.")]
        [Remarks("ChangeGangName <New name>")]
        public async Task ChangeGangName([Remainder] string name)
        {
            var user = UserRepository.FetchUser(Context);
            if (user.Cash < Config.GANG_NAME_CHANGE_COST)
                throw new Exception($"You do not have {Config.GANG_NAME_CHANGE_COST.ToString("C", Config.CI)}. Balance: {user.Cash.ToString("C", Config.CI)}.");
            var gangs = await (await DEABot.Gangs.FindAsync(y => y.GuildId == Context.Guild.Id)).ToListAsync();
            if (!gangs.Any(x => x.Name.ToLower() == name.ToLower())) throw new Exception($"There is already a gang by the name {name}.");
            await UserRepository.EditCashAsync(Context, -Config.GANG_NAME_CHANGE_COST);
            GangRepository.Modify(DEABot.GangUpdateBuilder.Set(x => x.Name, name), Context);
            await ReplyAsync($"You have successfully changed your gang name to {name} at the cost of {Config.GANG_NAME_CHANGE_COST.ToString("C", Config.CI)}.");
        }

        [Command("TransferLeadership")]
        [Require(Attributes.InGang, Attributes.GangLeader)]
        [Summary("Transfers the leadership of your gang to another member.")]
        [Remarks("TransferLeadership <@GangMember>")]
        public async Task TransferLeadership(IGuildUser user)
        {
            if (user.Id == Context.User.Id) throw new Exception("You are already the leader of this gang!");
            var gang = GangRepository.FetchGang(Context);
            if (!GangRepository.IsMemberOf(Context.User.Id, Context.Guild.Id, user.Id)) throw new Exception("This user is not a member of your gang!");
            for (int i = 0; i < gang.Members.Length; i++)
                if (gang.Members[i] == user.Id)
                {
                    gang.Members[i] = Context.User.Id;
                    GangRepository.Modify(DEABot.GangUpdateBuilder.Combine(
                        DEABot.GangUpdateBuilder.Set(x => x.LeaderId, user.Id),
                        DEABot.GangUpdateBuilder.Set(x => x.Members, gang.Members)), Context);
                    break;
                }
            await ReplyAsync($"{Context.User.Mention}, You have successfully transferred the leadership of {gang.Name} to {user.Mention}");
        }

        [Command("Deposit")]
        [Require(Attributes.InGang)]
        [Summary("Deposit cash into your gang's funds.")]
        [Remarks("Deposit <Cash>")]
        public async Task Deposit(double cash)
        {
            var user = UserRepository.FetchUser(Context);
            if (cash < Config.MIN_DEPOSIT) throw new Exception($"The lowest deposit is {Config.MIN_DEPOSIT.ToString("C", Config.CI)}.");
            if (user.Cash < cash) throw new Exception($"You do not have enough money. Balance: {user.Cash.ToString("C", Config.CI)}.");
            await UserRepository.EditCashAsync(Context, -cash);
            var gang = GangRepository.FetchGang(Context);
            GangRepository.Modify(DEABot.GangUpdateBuilder.Set(x => x.Wealth, gang.Wealth + cash), Context.User.Id, Context.Guild.Id);
            await ReplyAsync($"{Context.User.Mention}, You have successfully deposited {cash.ToString("C", Config.CI)}. " +
                             $"{gang.Name}'s Wealth: {(gang.Wealth + cash).ToString("C", Config.CI)}");
        }

        [Command("Withdraw")]
        [Require(Attributes.InGang)]
        [RequireCooldown]
        [Summary("Withdraw cash from your gang's funds.")]
        [Remarks("Withdraw <Cash>")]
        public async Task Withdraw(double cash)
        {
            var gang = GangRepository.FetchGang(Context);
            var user = UserRepository.FetchUser(Context);
            if (cash < Config.MIN_WITHDRAW) throw new Exception($"The minimum withdrawal is {Config.MIN_WITHDRAW.ToString("C", Config.CI)}.");
            if (cash > gang.Wealth * Config.WITHDRAW_CAP)
                throw new Exception($"You may only withdraw {Config.WITHDRAW_CAP.ToString("P")} of your gang's wealth, " +
                                    $"that is {(gang.Wealth * Config.WITHDRAW_CAP).ToString("C", Config.CI)}.");
            UserRepository.Modify(DEABot.UserUpdateBuilder.Set(x => x.Withdraw, DateTime.UtcNow), Context);
            GangRepository.Modify(DEABot.GangUpdateBuilder.Set(x => x.Wealth, gang.Wealth - cash), Context.User.Id, Context.Guild.Id);
            await UserRepository.EditCashAsync(Context, +cash);
            await ReplyAsync($"{Context.User.Mention}, You have successfully withdrawn {cash.ToString("C", Config.CI)}. " +
                             $"{gang.Name}'s Wealth: {(gang.Wealth - cash).ToString("C", Config.CI)}");
        }

    }
}
