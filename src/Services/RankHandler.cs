﻿using DEA.Database.Models;
using DEA.Database.Repository;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DEA
{
    public static class RankHandler
    {
        public static async Task Handle(IGuild guild, ulong userId)
        {
            if (!((await guild.GetCurrentUserAsync()).GuildPermissions.ManageRoles)) return;
            double cash = (UserRepository.FetchUser(userId, guild.Id)).Cash;
            var user = await guild.GetUserAsync(userId); //FETCHES THE USER
            var currentUser = await guild.GetCurrentUserAsync() as SocketGuildUser; //FETCHES THE BOT'S USER
            var guildData = GuildRepository.FetchGuild(guild.Id);
            List<IRole> rolesToAdd = new List<IRole>();
            List<IRole> rolesToRemove = new List<IRole>();
            if (guild != null && user != null && guildData.RankRoles != null)
            {
                //CHECKS IF THE ROLE EXISTS AND IF IT IS LOWER THAN THE BOT'S HIGHEST ROLE
                foreach (var rankRole in guildData.RankRoles)
                {
                    var role = guild.GetRole(Convert.ToUInt64(rankRole.Name));
                    if (role != null && role.Position < currentUser.Roles.OrderByDescending(x => x.Position).First().Position)
                    {
                        if (cash >= rankRole.Value && !user.RoleIds.Any(x => x.ToString() == rankRole.Name)) rolesToAdd.Add(role);
                        if (cash < rankRole.Value && user.RoleIds.Any(x => x.ToString() == rankRole.Name)) rolesToRemove.Add(role);
                    }
                    else
                    {
                        guildData.RankRoles.Remove(rankRole.Name);
                        await DEABot.Guilds.UpdateOneAsync(x => x.Id == guild.Id, DEABot.GuildUpdateBuilder.Set(x => x.RankRoles, guildData.RankRoles));
                    }
                }
                if (rolesToAdd.Count >= 1)
                    await user.AddRolesAsync(rolesToAdd);
                else if (rolesToRemove.Count >= 1)
                    await user.RemoveRolesAsync(rolesToRemove);
            }
        }

        public static IRole FetchRank(SocketCommandContext context)
        {
            var guild = GuildRepository.FetchGuild(context.Guild.Id);
            var cash = UserRepository.FetchUser(context).Cash;
            IRole role = null;
            if (guild.RankRoles != null)
                foreach (var rankRole in guild.RankRoles.OrderBy(x => x.Value))
                    if (cash >= rankRole.Value)
                        role = context.Guild.GetRole(Convert.ToUInt64(rankRole.Name));
            return role;
        }
    }
}