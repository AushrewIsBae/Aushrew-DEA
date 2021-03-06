﻿using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using DEA.Database.Repository;
using Discord.WebSocket;
using System.Collections.Generic;

namespace DEA.Modules
{
    [RequireCooldown]
    public class Crime : ModuleBase<SocketCommandContext>
    {

        [Command("Whore")]
        [Summary("Sell your body for some quick cash.")]
        [Remarks("Whore")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        public async Task Whore()
        {
            UserRepository.Modify(DEABot.UserUpdateBuilder.Set(x => x.Whore, DateTime.UtcNow), Context);
            var user = UserRepository.FetchUser(Context);
            int roll = new Random().Next(1, 101);
            if (roll > Config.WHORE_ODDS)
            {
                await UserRepository.EditCashAsync(Context, -Config.WHORE_FINE);
                await ReplyAsync($"{Context.User.Mention}, what are the fucking odds that one of your main clients was a cop... " +
                                 $"You are lucky you only got a {Config.WHORE_FINE.ToString("C", Config.CI)} fine. Balance: {(user.Cash - Config.WHORE_FINE).ToString("C", Config.CI)}");
            }
            else
            {
                double moneyWhored = (double)(new Random().Next((int)(Config.MIN_WHORE) * 100, (int)(Config.MAX_WHORE) * 100)) / 100;
                await UserRepository.EditCashAsync(Context, moneyWhored);
                await ReplyAsync($"{Context.User.Mention}, you whip it out and manage to rake in {moneyWhored.ToString("C", Config.CI)}. Balance: {(user.Cash + moneyWhored).ToString("C", Config.CI)}");
            }
        }

        [Command("Jump")]
        [Require(Attributes.Jump)]
        [Summary("Jump some random nigga in the hood.")]
        [Remarks("Jump")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        public async Task Jump()
        {
            UserRepository.Modify(DEABot.UserUpdateBuilder.Set(x => x.Jump, DateTime.UtcNow), Context);
            var user = UserRepository.FetchUser(Context);
            int roll = new Random().Next(1, 101);
            if (roll > Config.JUMP_ODDS)
            {
                await UserRepository.EditCashAsync(Context, -Config.JUMP_FINE);
                await ReplyAsync($"{Context.User.Mention}, turns out the nigga was a black belt, whooped your ass, and brought you in. " +
                                 $"Court's final ruling was a {Config.JUMP_FINE.ToString("C", Config.CI)} fine. Balance: {(user.Cash - Config.JUMP_FINE).ToString("C", Config.CI)}");
            }
            else
            {
                double moneyJumped = (double)(new Random().Next((int)(Config.MIN_JUMP) * 100, (int)(Config.MAX_JUMP) * 100)) / 100;
                await UserRepository.EditCashAsync(Context, moneyJumped);
                await ReplyAsync($"{Context.User.Mention}, you jump some random nigga on the streets and manage to get {moneyJumped.ToString("C", Config.CI)}. Balance: {(user.Cash + moneyJumped).ToString("C", Config.CI)}");
            }
        }

        [Command("Steal")]
        [Require(Attributes.Steal)]
        [Summary("Snipe some goodies from your local stores.")]
        [Remarks("Steal")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        public async Task Steal()
        {
            UserRepository.Modify(DEABot.UserUpdateBuilder.Set(x => x.Steal, DateTime.UtcNow), Context);
            var user = UserRepository.FetchUser(Context);
            int roll = new Random().Next(1, 101);
            if (roll > Config.STEAL_ODDS)
            {
                await UserRepository.EditCashAsync(Context, -Config.STEAL_FINE);
                await ReplyAsync($"{Context.User.Mention}, you were on your way out with the cash, but then some hot chick asked you if you " +
                                 $"wanted to bust a nut. Turns out she was cop, and raped you before turning you in. Since she passed on some " +
                                 $"nice words to the judge about you not resisting arrest, you managed to walk away with only a " +
                                 $"{Config.STEAL_FINE.ToString("C", Config.CI)} fine. Balance: {(user.Cash - Config.STEAL_FINE).ToString("C", Config.CI)}");
            }
            else
            {
                double moneyStolen = (double)(new Random().Next((int)(Config.MIN_STEAL) * 100, (int)(Config.MAX_STEAL) * 100)) / 100;
                await UserRepository.EditCashAsync(Context, moneyStolen);
                string randomStore = Config.STORES[new Random().Next(1, Config.STORES.Length) - 1];
                await ReplyAsync($"{Context.User.Mention}, you walk in to your local {randomStore}, point a fake gun at the clerk, and manage to walk away " +
                                 $"with {moneyStolen.ToString("C", Config.CI)}. Balance: {(user.Cash + moneyStolen).ToString("C", Config.CI)}");
            }
        }

        [Command("Bully")]
        [Require(Attributes.Bully)]
        [Summary("Bully anyone's nickname to whatever you please.")]
        [Remarks("Bully <@User> <Nickname>")]
        [RequireBotPermission(GuildPermission.ManageNicknames)]
        public async Task Bully(SocketGuildUser userToBully, [Remainder] string nickname)
        {
            if (nickname.Length > 32) throw new Exception("The length of a nickname may not be longer than 32 characters.");
            await userToBully.ModifyAsync(x => x.Nickname = nickname);
            await ReplyAsync($"{userToBully.Mention} just got ***BULLIED*** by {Context.User.Mention} with his new nickname: \"{nickname}\".");
        }

        [Command("Rob")]
        [Require(Attributes.Rob)]
        [Summary("Lead a large scale operation on a local bank.")]
        [Remarks("Rob <Amount of cash to spend on resources>")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        public async Task Rob(double resources)
        {
            var user = UserRepository.FetchUser(Context);
            if (user.Cash < resources) throw new Exception($"You do not have enough money. Balance: {user.Cash.ToString("C", Config.CI)}");
            if (resources < Config.MIN_RESOURCES) throw new Exception($"The minimum amount of money to spend on resources for rob is {Config.MIN_RESOURCES.ToString("C", Config.CI)}.");
            if (resources > Config.MAX_RESOURCES) throw new Exception($"The maximum amount of money to spend on resources for rob is {Config.MAX_RESOURCES.ToString("C", Config.CI)}.");
            UserRepository.Modify(DEABot.UserUpdateBuilder.Set(x => x.Rob, DateTime.UtcNow), Context);
            Random rand = new Random();
            double succesRate = rand.Next(Config.MIN_ROB_ODDS * 100, Config.MAX_ROB_ODDS * 100) / 10000f;
            double moneyStolen = resources / (succesRate / 1.50f); 
            string randomBank = Config.BANKS[rand.Next(1, Config.BANKS.Length) - 1];
            if (rand.Next(10000) / 10000f >= succesRate)
            {
                await UserRepository.EditCashAsync(Context, moneyStolen);
                await ReplyAsync($"{Context.User.Mention}, with a {succesRate.ToString("P")} chance of success, you successfully stole " +
                $"{moneyStolen.ToString("C", Config.CI)} from the {randomBank}. Balance: {(user.Cash + moneyStolen).ToString("C", Config.CI)}$.");
            }
            else
            {
                await UserRepository.EditCashAsync(Context, -resources);
                await ReplyAsync($"{Context.User.Mention}, with a {succesRate.ToString("P")} chance of success, you failed to steal " +
                $"{moneyStolen.ToString("C", Config.CI)} from the {randomBank}, losing all resources in the process. Balance: {(user.Cash - resources).ToString("C", Config.CI)}.");
            }
        }

        [Command("Cooldowns")]
        [Remarks("Cooldowns")]
        [Summary("Check when you can sauce out more cash.")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        public async Task Cooldowns()
        {
            var user = UserRepository.FetchUser(Context);
            var cooldowns = new Dictionary<String, TimeSpan>();
            cooldowns.Add("Whore", Config.WHORE_COOLDOWN.Subtract(DateTime.UtcNow.Subtract(user.Whore)));
            cooldowns.Add("Jump", Config.JUMP_COOLDOWN.Subtract(DateTime.UtcNow.Subtract(user.Jump)));
            cooldowns.Add("Steal", Config.STEAL_COOLDOWN.Subtract(DateTime.UtcNow.Subtract(user.Steal)));
            cooldowns.Add("Rob", Config.ROB_COOLDOWN.Subtract(DateTime.UtcNow.Subtract(user.Rob)));
            cooldowns.Add("Withdraw", Config.WITHDRAW_COOLDOWN.Subtract(DateTime.UtcNow.Subtract(user.Withdraw)));
            var description = "";
            foreach (var cooldown in cooldowns)
            {
                if (cooldown.Value.TotalMilliseconds > 0)
                    description += $"{cooldown.Key}: {cooldown.Value.Hours}:{cooldown.Value.Minutes.ToString("D2")}:{cooldown.Value.Seconds.ToString("D2")}\n";
            }
            if (description.Length == 0) throw new Exception("All your commands are available for use!");
            var builder = new EmbedBuilder()
            {
                Title = $"All cooldowns for {Context.User}",
                Description = description,
                Color = new Color(49, 62, 255)
            };
            await Context.Channel.SendMessageAsync("", embed: builder);
        }
    }
}
