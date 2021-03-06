﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FoxMud.Game.Command;
using FoxMud.Game.World;

namespace FoxMud.Game.State
{
    class SpammedCommand
    {
        public CommandContext Context { get; set; }
        public CommandInfo Info { get; set; }
    }

    class PlayingState : SessionStateBase
    {
        // for spamming commands
        private readonly Queue<SpammedCommand> queuedCommands = new Queue<SpammedCommand>();
        private bool ready = true;

        private void TryExecuteCommand(string input)
        {
            var commandContext = CommandContext.Create(input);
            var player = Server.Current.Database.Get<Player>(Session.Player.Key);
            var commandInfo = Server.Current.CommandLookup.FindCommand(commandContext.CommandName, player);

            TryExecuteCommand(commandContext, commandInfo);
        }

        private void TryExecuteCommand(CommandContext commandContext, CommandInfo commandInfo)
        {
            if (commandInfo != null)
            {
                if (!ready)
                {
                    //Server.Current.Log(string.Format("not ready. queueing event: {0}", commandContext.CommandName));
                    // queue command
                    queuedCommands.Enqueue(new SpammedCommand()
                    {
                        Context = commandContext,
                        Info = commandInfo
                    });
                    return;
                }

                ready = false;
                //Server.Current.Log(string.Format("executing command: {0}", commandContext.CommandName));
                commandInfo.Command.Execute(Session, commandContext);
                Session.Player.WritePrompt();
                if (queuedCommands.Count > 0)
                    setTimeout(queuedCommands.Dequeue(), commandInfo.TickLength); // command already queued
                else
                    setTimeout(commandInfo.TickLength); // simply delay
            }
            else
            {
                Session.WriteLine("`wCommand not recognized.");
            }
        }

        private void setTimeout(TickDelay tickDelay)
        {
            //Server.Current.Log("generic timeout");
            var t = new System.Timers.Timer()
                {
                    Interval = (long) tickDelay,
                    AutoReset = false,
                };
            
            t.Elapsed += makeReady;
            t.Start(); // fire this in tickDelay ms
        }

        private void setTimeout(SpammedCommand command, TickDelay tickLength)
        {
            //Server.Current.Log(string.Format("command timeout: {0}", command.Context.CommandName));
            var t = new System.Timers.Timer()
            {
                Interval = (long)tickLength,
                AutoReset = false,
            };

            t.Elapsed += (sender, e) => nextCommand(sender, e, command);
            t.Start();
        }

        private void nextCommand(object sender, System.Timers.ElapsedEventArgs e, SpammedCommand command)
        {
            //Server.Current.Log(string.Format("nextCommand: {0}", command.Context.CommandName));
            ready = true;
            TryExecuteCommand(command.Context, command.Info);
        }

        // MAKE READYYYYY!!!!
        private void makeReady(object sender, System.Timers.ElapsedEventArgs e)
        {
            ready = true;
            if (queuedCommands.Count <= 0)
                return;

            // a command was entered in the interim
            var command = queuedCommands.Dequeue();
            //Server.Current.Log(string.Format("makeReady command: {0}", command.Context.CommandName));
            TryExecuteCommand(command.Context, command.Info);
        }

        public override void OnStateEnter()
        {
            if (Session.Player != null)
                Session.Player.LoggedIn = true;

            TryExecuteCommand("look");
            base.OnStateEnter();
        }

        public override void OnInput(string input)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                TryExecuteCommand(input);
            }
            else
            {
                // handles when player just hits enter
                Session.Player.WritePrompt();
            }

            base.OnInput(input);
        }

        public override void OnStateLeave()
        {
            base.OnStateLeave();
        }

        public override void OnStateShutdown()
        {
            if (Session.Player != null)
                Session.Player.LoggedIn = false;

            Server.Current.Database.Save(Session.Player);
            var room = Server.Current.Database.Get<Room>(Session.Player.Location);
            room.RemovePlayer(Session.Player);

            base.OnStateShutdown();
        }
    }
}
