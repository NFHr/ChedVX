using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChedVX.UI.Shortcuts
{
    public interface IShortcutCommandSource
    {
        IEnumerable<string> Commands { get; }

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <returns>True if the command was executed</returns>
        bool ExecuteCommand(string command);

        bool ResolveCommandName(string command, out string name);
    }

    public class NullShortcutCommandSource : IShortcutCommandSource
    {
        public IEnumerable<string> Commands => Enumerable.Empty<string>();

        // Do nothing
        public bool ExecuteCommand(string command) => false;

        public bool ResolveCommandName(string command, out string name)
        {
            name = null;
            return false;
        }
    }

    public class ShortcutCommandSource : IShortcutCommandSource
    {
        private Dictionary<string, (string Name, Action Action)> commands { get; } = new Dictionary<string, (string, Action)>();

        public IEnumerable<string> Commands => commands.Keys;

        public void RegisterCommand(string command, string name, Action action)
        {
            if (commands.ContainsKey(command)) throw new InvalidOperationException("The command is already registered.");
            commands.Add(command, (name, action));
        }

        public bool ExecuteCommand(string command)
        {
            if (!commands.ContainsKey(command)) return false;
            commands[command].Action();
            return true;
        }

        public bool ResolveCommandName(string command, out string name)
        {
            name = null;
            if (!commands.ContainsKey(command)) return false;
            name = commands[command].Name;
            return true;
        }
    }
}
