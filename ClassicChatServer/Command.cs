namespace ClassicChatServer
{
    public class Command
    {
        public static List<Command> Commands = new List<Command>()
    {
        new KickCommand(),
        new BanCommand(),
        new PlayerListCommand(),
        new CmdsCommand(),
        new ShutdownCommand()
    };

        public static void ExecuteCommand(Player executor, string message)
        {
            var cmdparts = message.Split(" ", 2);
            if (cmdparts.Length < 1) return;
            Command? command = null;
            foreach (var c in Commands)
                if (c.Name.ToLower() == cmdparts[0].ToLower())
                    command = c;
            if (command == null)
            {
                executor.SendMessage($"&cCouldn't find command \"&e{cmdparts[0]}&c\"! &eUse &d/cmds&e!");
                return;
            }
            if (command.Rank > executor.Rank)
            {
                executor.SendMessage($"&cRank too low to use &e{command.Name}&c (&e{command.Rank}+&c)!");
                return;
            }

            command.Execute(executor, cmdparts.Length > 1 ? cmdparts[1] : "");
        }

        public virtual string Name => "name";
        public virtual int Rank => 0;
        public virtual void Execute(Player executor, string message)
        {

        }

        public class KickCommand : Command
        {
            public override string Name => "kick";
            public override int Rank => 100;
            public override void Execute(Player executor, string message)
            {
                string[] args = message.Split(" ", 2);
                var player = Util.FindPlayer(args[0]);
                if (player == null) { executor.SendMessage($"&cCouldn't find player \"&e{args[0]}&c\"!"); return; }
                if (args.Length < 2) { executor.SendMessage("&cMissing reason argument!"); }
                player.Kick(args[1]);
                Server.Broadcast($"&7{player.ColouredName}&e was kicked by &7{executor.ColouredName}&e, Reason: &7 {args[1]}");
            }
        }
        public class BanCommand : Command
        {
            public override string Name => "ban";
            public override int Rank => 100;
            public override void Execute(Player executor, string message)
            {
                string[] args = message.Split(" ", 2);
                var player = Util.FindPlayer(args[0]);
                if (player == null) { executor.SendMessage($"&cCouldn't find player \"&e{args[0]}&c\"!"); return; }
                if (args.Length < 2) { executor.SendMessage("&cMissing reason argument!"); }
                player.Ban($"Banned: {args[1]}");
                Server.Broadcast($"&7{player.ColouredName}&e was banned by &7{executor.ColouredName}&e, Reason: &7 {args[1]}");
            }
        }

        public class CmdsCommand : Command
        {
            public override string Name => "cmds";
            public override int Rank => 0;
            public override void Execute(Player executor, string message)
            {
                executor.SendMessage("Usable commmands:");
                foreach (var c in Command.Commands.Where((x) => { return x.Rank <= executor.Rank; }))
                    executor.SendMessage($"&e - &d/{c.Name}");
            }
        }

        public class PlayerListCommand : Command
        {
            public override string Name => "playerlist";
            public override int Rank => 0;
            public override void Execute(Player executor, string message)
            {
                executor.SendMessage($"&eThere are &a{Server.PlayerList.Count} &eplayers online!");
                foreach (var pl in Server.PlayerList)
                    executor.SendMessage("&7 - " + pl.ColouredName);
            }
        }

        public class ShutdownCommand : Command
        {
            public override string Name => "shutdown";
            public override int Rank => 100;
            public override void Execute(Player executor, string message)
            {
                Server.Shutdown();
            }
        }

    }

}
