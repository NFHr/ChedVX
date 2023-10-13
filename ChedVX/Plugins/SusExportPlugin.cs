﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Newtonsoft.Json;
using ChedVX.Components.Exporter;
using ChedVX.UI.Windows;

namespace ChedVX.Plugins
{
    public class SusExportPlugin : IScoreBookExportPlugin
    {
        public string DisplayName => "Sliding Universal Score (*.sus)";

        public string FileFilter => "Sliding Universal Score (*.sus)|*.sus";

        public void Export(IScoreBookExportPluginArgs args)
        {
            /*
            var book = args.GetScoreBook();
            SusArgs susArgs = JsonConvert.DeserializeObject<SusArgs>(args.GetCustomData() ?? "") ?? new SusArgs();
            if (!args.IsQuick)
            {
                var vm = new SusExportWindowViewModel(book, susArgs);
                var window = new SusExportWindow() { DataContext = vm };
                var result = window.ShowDialog();
                if (!result.HasValue || !result.Value) throw new UserCancelledException();
                args.SetCustomData(JsonConvert.SerializeObject(susArgs));
            }

            var exporter = new SusExporter(book, susArgs);
            exporter.Export(args.Stream);
            */
        }
    }
}
