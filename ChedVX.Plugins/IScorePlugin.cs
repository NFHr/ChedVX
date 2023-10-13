using ChedVX.Core;
using ChedVX.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Plugins
{
    /// <summary>
    /// Represents a plugin that handles chart data.
    /// </summary>
    public interface IScorePlugin : IPlugin
    {
        void Run(IScorePluginArgs args);
    }

    /// <summary>
    /// Represents the information passed when <see cref="IScorePlugin"/> is executed.
    /// </summary>
    public interface IScorePluginArgs
    {
        Score GetCurrentScore();
        SelectionRange GetSelectedRange();
        void UpdateScore(Score score);
    }
}
