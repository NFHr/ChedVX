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
    /// Represents a plugin that exports chart data.
    /// </summary>
    public interface IScoreBookExportPlugin : IPlugin
    {
        /// <summary>
        /// Get the filter string used in the file selection dialog.
        /// </summary>
        string FileFilter { get; }

        /// <summary>
        /// Execute the export process.
        /// </summary>
        /// <param name="args">Get information during export<see cref="IScoreBookExportPluginArgs"/></param>
        /// <remarks>If you cancel the process after calling the method, throw <see cref="UserCancelledException"/>. </remarks>
        void Export(IScoreBookExportPluginArgs args);
    }

    /// <summary>
    /// Represents the information passed to the plugin during export.
    /// </summary>
    public interface IScoreBookExportPluginArgs : IDiagnosable
    {
        /// <summary>
        /// Get the <see cref="System.IO.Stream"/> to write data to.
        /// </summary>
        Stream Stream { get; }

        /// <summary>
        /// Gets a value indicating whether additional information must be entered.
        /// If this value is True, there is no need to display a dialog requesting additional information.
        /// </summary>
        bool IsQuick { get; }

        /// <summary>
        /// Gets a value indicating whether additional information must be entered.
        /// If this value is True, there is no need to display a dialog requesting additional information.
        /// </summary>
        ScoreBook GetScoreBook();

        /// <summary>
        /// Get additional information associated with the exported data.
        /// </summary>
        /// <returns>String representing additional information</returns>
        string GetCustomData();

        /// <summary>
        /// Stores additional information associated with the exported data.
        /// </summary>
        /// <param name="data">String representing additional information to save</param>
        void SetCustomData(string data);
    }
}
