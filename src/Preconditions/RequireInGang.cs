﻿using DEA.SQLite.Models;
using DEA.SQLite.Repository;
using System;
using System.Threading.Tasks;

namespace Discord.Commands
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RequireInGangAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
        {
            using (var db = new DbContext())
            {
                
                if (!(await GangRepository.InGangAsync(context.User.Id, context.Guild.Id))) return PreconditionResult.FromError("You must be in a gang to use this command.");
            }
            return PreconditionResult.FromSuccess();
        }
    }
}