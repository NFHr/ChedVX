using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.UI.Shortcuts
{
    public static class Commands
    {
        // MenuStrip
        public static string NewFile => "files.new";
        public static string OpenFile => "files.open";
        public static string Save => "files.save";
        public static string SaveAs => "files.saveAs";
        public static string ReExport => "files.reExport";
        public static string ShowScoreBookProperties => "editor.action.showScoreBookProperties";
        public static string ShowShortcutSettings => "application.showShortcutSettings";

        public static string Undo => "editor.action.undo";
        public static string Redo => "editor.action.redo";

        public static string Cut => "editor.action.clipboardCut";
        public static string Copy => "editor.action.clipboardCopy";
        public static string Paste => "editor.action.clipboardPaste";
        public static string PasteFlip => "editor.action.clipboardPasteFlip";
        public static string RemoveSelectedNotes => "editor.action.removeSelectedNotes";

        public static string SelectAll => "editor.action.selectAll";
        public static string SelectToBegin => "editor.action.selectToBegin";
        public static string SelectToEnd => "editor.action.selectToEnd";
        public static string Deselect => "editor.action.DeSelect";

        public static string FlipSelectedNotes => "editor.action.flipSelectedNotes";
        public static string SwitchLaserColor => "editor.action.SwitchLaserColor";

        public static string CopyLaneTiltEvents => "editor.action.CopyLaneTiltEvents";
        public static string RemoveLaneTiltEvents => "editor.action.RemoveLaneTiltEvents";

        public static string CopyLaneZoomEvents => "editor.action.CopyLaneZoomEvents";
        public static string RemoveLaneZoomEvents => "editor.action.RemoveLaneZoomEvents";

        public static string RemoveSelectedEvents => "editor.action.RemoveSelectedEvents";

        public static string RemoveSelectedMeasurement => "editor.action.RemoveSelectedMeasurement";
        public static string AddBlankMeasurement => "editor.action.AddBlankMeasurement";

        public static string RemoveAllComment => "editor.action.RemoveAllComment";

        public static string ClearChartContent => "editor.action.ClearChartContent";

        public static string SwitchScorePreviewMode => "editor.view.switchScorePreviewMode";

        public static string WidenLaneSpacing => "editor.view.widenLaneSpacing";
        public static string NarrowLaneSpacing => "editor.view.narrowLaneSpacing";
        public static string InsertBpmChange => "editor.action.insertBpmChange";
        public static string InsertTimeSignatureChange => "editor.action.insertTimeSignatureChange";
        public static string InsertHighSpeedChange => "editor.action.insertHighSpeedChange";

        public static string PlayPreview => "editor.view.playPreview";

        public static string ShowHelp => "application.showHelp";

        // ToolStrip
        public static string SelectPen => "editor.selectPen";
        public static string SelectSelection => "editor.selectSelection";
        public static string SelectEraser => "editor.selectEraser";

        public static string ZoomIn => "editor.view.zoomIn";
        public static string ZoomOut => "editor.view.zoomOut";

        public static string Property => "editor.editProperty";
        public static string SelectBTChip => "editor.selectBTChip";
        public static string SelectBTLong => "editor.selectBTLong";
        public static string SelectFXChip => "editor.selectFXChip";
        public static string SelectFXLong => "editor.selectFXLong";
        public static string SelectLeftLaser => "editor.selectLeftLaser";
        public static string SelectRightLaser => "editor.selectRightLaser";

    }
}
