﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.Item;
using FoxMud.Game.World;

namespace FoxMud.Game.Command.Visual
{
    [Command("look", false)]
    [Command("l", false)]
    class LookCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: look");
            session.WriteLine("Syntax: look <player>");
            session.WriteLine("Syntax: look <item>");
        }

        public static void WriteNullRoomDescription(Session session)
        {
            session.WriteLine("The Void.");
            session.WriteLine("Something went terribly wrong and you've ended up in the void.\r\n" +
                              "You're not supposed to be here. Contact a staff member and they'll get you back\r\n" +
                              "to where you belong.");
        }

        private static void WriteRoomPlayerList(Session session, Room room)
        {
            foreach (var player in room.GetPlayers())
            {
                if (player == session.Player)
                    continue;

                session.WriteLine("{0} is here.\n", session.Player.GetOtherPlayerDescription(player));
            }

            foreach (var npc in room.GetNpcs())
            {
                session.WriteLine("{0} is here.\n", npc.Name);
            }
        }

        private static void WriteRoomDescription(Session session, Room room)
        {
            session.WriteLine("{0}", room.Title);
            session.WriteLine(room.Description);
        }

        private static void WriteAvailableExits(Session session, Room room)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Available exits: [ ");

            foreach (var exit in room.Exits.Keys)
            {
                builder.Append(string.Format("{0} ", exit));
            }

            builder.Append("]");
            session.WriteLine(builder.ToString());
            session.WriteLine("");
        }

        private static void WriteItemsOnFloor(Session session, Room room)
        {
            foreach (var item in room.Items)
            {
                var actualItem = Server.Current.Database.Get<PlayerItem>(item.Key);
                session.WriteLine("{0} lies here.", actualItem.Description);
            }
        }

        private static void PerformLookAtRoom(Session session)
        {
            var room = Server.Current.Database.Get<Room>(session.Player.Location);

            if (room == null)
            {
                WriteNullRoomDescription(session);
                return;
            }

            WriteRoomDescription(session, room);
            WriteAvailableExits(session, room);
            WriteItemsOnFloor(session, room);
            WriteRoomPlayerList(session, room);
        }

        private void PerformLookAtPlayer(Session session, Room room, Player player)
        {
            session.Player.Send("You look at %d", player);
            session.WriteLine(player.Description);
            player.Send("%d looks at you.", session.Player);
            room.SendPlayers("%d looks at %D", session.Player, player, session.Player, player);
        }

        private void PerformLookAtNpc(Session session, NonPlayer npc)
        {
            session.WriteLine("You look at {0}", npc.Name.ToLower());
            session.WriteLine("{0}", npc.Description);
        }

        public void Execute(Session session, CommandContext context)
        {
            if (context.Arguments.Count == 0)
            {
                PerformLookAtRoom(session);
                return;
            }

            // todo: look at a directional exit

            Room room = Server.Current.Database.Get<Room>(session.Player.Location);
            if (room == null)
            {
                session.WriteLine("Couldn't find anything to look at");
                return;
            }

            Player player = room.LookUpPlayer(session.Player, context.ArgumentString);
            if (player != null)
            {
                PerformLookAtPlayer(session, room, player);
                return;
            }

            NonPlayer npc = room.LookUpNpc(context.ArgumentString);
            if (npc != null)
            {
                PerformLookAtNpc(session, npc);
                return;
            }

            // find item to look at
            foreach (var key in session.Player.Inventory.Keys)
            {
                var item = Server.Current.Database.Get<PlayerItem>(key);
                if (item != null)
                {
                    item.LookAt(session);
                    return;
                }
            }

            session.WriteLine("Couldn't find anything to look at");
        }
    }
}
