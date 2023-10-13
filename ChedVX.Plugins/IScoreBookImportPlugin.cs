using ChedVX.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Plugins
{
    /// <summary>
    /// Represents a plugin that imports chart data.
    /// </summary>
    public interface IScoreBookImportPlugin : IPlugin
    {
        /// <summary>
        /// Get the filter string used when selecting files.
        /// </summary>
        string FileFilter { get; }

        /// <summary>
        /// Perform the import process of the musical score data.
        /// </summary>
        /// <param name="args">Represents information passed during import<see cref="IScoreBookImportPluginArgs"/></param>
        /// <returns>Represents the musical score to be imported <see cref="ScoreBook"/></returns>
        ScoreBook Import(IScoreBookImportPluginArgs args);
    }

    /// <summary>
    /// Represents the information passed when importing music score data.
    /// </summary>
    public interface IScoreBookImportPluginArgs : IDiagnosable
    {
        /// <summary>
        /// Get the stream to read data from.
        /// </summary>
        Stream Stream { get; }
    }
}
