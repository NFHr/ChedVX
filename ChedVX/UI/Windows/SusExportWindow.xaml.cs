﻿using ChedVX.Components.Exporter;
using ChedVX.Core;
using ChedVX.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace ChedVX.UI.Windows
{
    /// <summary>
    /// Interaction logic for SusExportWindow.xaml
    /// </summary>
    public partial class SusExportWindow : Window
    {
        public SusExportWindow()
        {
            InitializeComponent();
        }
    }

    public class SusExportWindowViewModel : ViewModel
    {
        private SusArgs SusArgs { get; }
        private ScoreBook ScoreBook { get; }

        public string MusicFilter { get; } = Helpers.GetFilterString(FileFilterStrings.AudioFilter, SoundSource.SupportedExtensions);
        public string ImageFilter { get; } = Helpers.GetFilterString(FileFilterStrings.ImageFilter, new[] { ".jpg", ".png", ".bmp" });
        public IEnumerable<string> Levels { get; } = Enumerable.Range(1, 14).SelectMany(p => new string[] { p.ToString(), $"{p}+" });

        public string Title => ScoreBook.Title;
        public string Artist => ScoreBook.ArtistName;
        public string NotesDesigner => ScoreBook.Effector;

        private SusArgs.Difficulty difficulty;
        public SusArgs.Difficulty Difficulty
        {
            get => difficulty;
            set
            {
                if (value == difficulty) return;
                difficulty = value;
                NotifyPropertyChanged();
            }
        }

        private string level;
        public string Level
        {
            get => level;
            set
            {
                if (value == level) return;
                level = value;
                NotifyPropertyChanged();
            }
        }

        private string songId;
        public string SongId
        {
            get => songId;
            set
            {
                if (value == songId) return;
                songId = value;
                NotifyPropertyChanged();
            }
        }

        private string soundFileName;
        public string SoundFileName
        {
            get => soundFileName;
            set
            {
                if (value == soundFileName) return;
                soundFileName = value;
                NotifyPropertyChanged();
            }
        }

        private double soundOffset;
        public double SoundOffset
        {
            get => soundOffset;
            set
            {
                if (value == soundOffset) return;
                soundOffset = value;
                NotifyPropertyChanged();
            }
        }

        private string jacketFileName;
        public string JacketFileName
        {
            get => jacketFileName;
            set
            {
                if (value == jacketFileName) return;
                jacketFileName = value;
                NotifyPropertyChanged();
            }
        }

        private bool hasPaddingBar;
        public bool HasPaddingBar
        {
            get => hasPaddingBar;
            set
            {
                if (value == hasPaddingBar) return;
                hasPaddingBar = value;
                NotifyPropertyChanged();
            }
        }

        private string additionalData;
        public string AdditionalData
        {
            get => additionalData;
            set
            {
                if (value == additionalData) return;
                additionalData = value;
                NotifyPropertyChanged();
            }
        }

        public Action<string> SetSoundFileNameAction => path => SoundFileName = System.IO.Path.GetFileName(path);
        public Action<string> SetJacketFileNameAction => path => JacketFileName = System.IO.Path.GetFileName(path);

        public SusExportWindowViewModel()
        {
        }

        public SusExportWindowViewModel(ScoreBook book, SusArgs susArgs)
        {
            ScoreBook = book;
            SusArgs = susArgs;
        }

        public void BeginEdit()
        {
            Difficulty = SusArgs.PlayDifficulty;
            Level = SusArgs.PlayLevel;
            SongId = SusArgs.SongId;
            SoundFileName = SusArgs.SoundFileName;
            SoundOffset = (double)SusArgs.SoundOffset;
            JacketFileName = SusArgs.JacketFilePath;
            HasPaddingBar = SusArgs.HasPaddingBar;
            AdditionalData = SusArgs.AdditionalData;
        }

        public void CommitEdit()
        {
            SusArgs.PlayDifficulty = Difficulty;
            SusArgs.PlayLevel = Level;
            SusArgs.SongId = SongId;
            SusArgs.SoundFileName = SoundFileName;
            SusArgs.SoundOffset = (decimal)SoundOffset;
            SusArgs.JacketFilePath = JacketFileName;
            SusArgs.HasPaddingBar = HasPaddingBar;
            SusArgs.AdditionalData = AdditionalData;
        }
    }

    public class SusDifficultySourceProvider : EnumSourceProvider<SusArgs.Difficulty>
    {
 
    }
}
