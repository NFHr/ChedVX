﻿using ChedVX.Core;
using ChedVX.Core.Notes;
using ConcurrentPriorityQueue;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Components.Exporter
{
    public class SusExporter
    {
   
        protected ScoreBook ScoreBook { get; }
        protected BarIndexCalculator BarIndexCalculator { get; }
        protected int StandardBarTick => ScoreBook.Score.TicksPerBeat * 4;
        protected int BarIndexOffset => CustomArgs.HasPaddingBar ? 1 : 0;
        public SusArgs CustomArgs { get; }

        public SusExporter(ScoreBook book, SusArgs susArgs)
        {
            ScoreBook = book;
            CustomArgs = susArgs;
            BarIndexCalculator = new BarIndexCalculator(book.Score.TicksPerBeat, book.Score.Events.TimeSignatureChangeEvents);
        }

        public void Export(Stream stream)
        {
            /*
            var book = ScoreBook;
            SusArgs args = CustomArgs;
            var notes = book.Score.Notes;
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("This file was generated by Ched {0}.", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString());

                writer.WriteLine("#TITLE \"{0}\"", book.Title);
                writer.WriteLine("#ARTIST \"{0}\"", book.ArtistName);
                writer.WriteLine("#DESIGNER \"{0}\"", book.NotesDesignerName);
                writer.WriteLine("#DIFFICULTY {0}", (int)args.PlayDifficulty + (string.IsNullOrEmpty(args.ExtendedDifficulty) ? "" : ":" + args.ExtendedDifficulty));
                writer.WriteLine("#PLAYLEVEL {0}", args.PlayLevel);
                writer.WriteLine("#SONGID \"{0}\"", args.SongId);
                writer.WriteLine("#WAVE \"{0}\"", args.SoundFileName);
                writer.WriteLine("#WAVEOFFSET {0}", args.SoundOffset.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("#JACKET \"{0}\"", args.JacketFilePath);
                writer.WriteLine();

                if (!string.IsNullOrEmpty(args.AdditionalData))
                {
                    writer.WriteLine(args.AdditionalData);
                    writer.WriteLine();
                }

                writer.WriteLine("#REQUEST \"ticks_per_beat {0}\"", book.Score.TicksPerBeat);
                writer.WriteLine();

                var timeSignatures = BarIndexCalculator.TimeSignatures.Select(p => new SusDataLine(p.StartBarIndex, barIndex => string.Format("#{0:000}02: {1}", barIndex, 4f * p.TimeSignature.Numerator / p.TimeSignature.Denominator), p.StartBarIndex == 0));
                WriteLinesWithOffset(writer, timeSignatures);
                writer.WriteLine();

                var bpmlist = book.Score.Events.BpmChangeEvents
                    .GroupBy(p => p.Bpm)
                    .SelectMany((p, i) => p.Select(q => new { Index = i, Value = q, BarPosition = BarIndexCalculator.GetBarPositionFromTick(q.Tick) }))
                    .ToList();

                if (bpmlist.Count >= 36 * 36) throw new ArgumentException("BPM定義数が上限を超えました。");

                var bpmIdentifiers = EnumerateIdentifiers(2, NumChars.Concat(AlphaChars)).Skip(1).Take(bpmlist.Count).ToList();
                foreach (var item in bpmlist.GroupBy(p => p.Index).Select(p => p.First()))
                {
                    writer.WriteLine("#BPM{0}: {1}", bpmIdentifiers[item.Index], item.Value.Bpm.ToString(CultureInfo.InvariantCulture));
                }

                // 小節オフセット追加用に初期BPM定義だけ1行に分離
                var bpmChanges = bpmlist.GroupBy(p => p.Value.Tick == 0).SelectMany(p => p.GroupBy(q => q.BarPosition.BarIndex).Select(eventInBar =>
                {
                    var sig = BarIndexCalculator.GetTimeSignatureFromBarIndex(eventInBar.Key);
                    int barLength = StandardBarTick * sig.Numerator / sig.Denominator;
                    var items = eventInBar.Select(q => (q.BarPosition.TickOffset, bpmIdentifiers[q.Index]));
                    return new SusDataLine(eventInBar.Key, barIndex => string.Format("#{0:000}08: {1}", barIndex, GenerateLineData(barLength, items)), p.Key);
                }));
                WriteLinesWithOffset(writer, bpmChanges);
                writer.WriteLine();

                var speeds = book.Score.Events.HighSpeedChangeEvents.Select(p =>
                {
                    var barPos = BarIndexCalculator.GetBarPositionFromTick(p.Tick);
                    return string.Format("{0}'{1}:{2}", barPos.BarIndex + (p.Tick == 0 ? 0 : BarIndexOffset), barPos.TickOffset, p.SpeedRatio.ToString(CultureInfo.InvariantCulture));
                });
                writer.WriteLine("#TIL00: \"{0}\"", string.Join(", ", speeds));
                writer.WriteLine("#HISPEED 00");
                writer.WriteLine("#MEASUREHS 00");
                writer.WriteLine();

                var shortNotes = notes.Taps.Cast<TappableBase>().Select(p => new { Type = '1', Note = p })
                    .Concat(notes.ExTaps.Cast<TappableBase>().Select(p => new { Type = '2', Note = p }))
                    .Concat(notes.Flicks.Cast<TappableBase>().Select(p => new { Type = '3', Note = p }))
                    .Concat(notes.Damages.Cast<TappableBase>().Select(p => new { Type = '4', Note = p }))
                    .Select(p => (p.Note.Tick, p.Note.LaneIndex, p.Type + ToLaneWidthString(p.Note.Width)));
                WriteLinesWithOffset(writer, GetShortNoteLines("1", shortNotes));
                writer.WriteLine();

                var airs = notes.Airs.Select(p =>
                {
                    string type = "";
                    switch (p.HorizontalDirection)
                    {
                        case HorizontalAirDirection.Center:
                            type = p.VerticalDirection == VerticalAirDirection.Up ? "1" : "2";
                            break;

                        case HorizontalAirDirection.Left:
                            type = p.VerticalDirection == VerticalAirDirection.Up ? "3" : "5";
                            break;

                        case HorizontalAirDirection.Right:
                            type = p.VerticalDirection == VerticalAirDirection.Up ? "4" : "6";
                            break;
                    }

                    return (p.Tick, p.LaneIndex, type + ToLaneWidthString(p.Width));
                });
                WriteLinesWithOffset(writer, GetShortNoteLines("5", airs));
                writer.WriteLine();

                var identifier = new IdentifierAllocationManager();

                var holds = book.Score.Notes.Holds
                    .OrderBy(p => p.StartTick)
                    .Select(p => new
                    {
                        Identifier = identifier.Allocate(p.StartTick, p.Duration),
                        StartTick = p.StartTick,
                        EndTick = p.StartTick + p.Duration,
                        Width = p.Width,
                        LaneIndex = p.LaneIndex
                    })
                    .SelectMany(hold =>
                    {
                        var items = new[]
                        {
                            (hold.StartTick, hold.LaneIndex, "1" + ToLaneWidthString(hold.Width)),
                            (hold.EndTick, hold.LaneIndex, "2" + ToLaneWidthString(hold.Width))
                        };
                        return GetLongNoteLines("2", hold.Identifier.ToString(), items);
                    });
                WriteLinesWithOffset(writer, holds);
                writer.WriteLine();
                identifier.Clear();

                var slides = notes.Slides
                    .OrderBy(p => p.StartTick)
                    .Select(p => new
                    {
                        Identifier = identifier.Allocate(p.StartTick, p.GetDuration()),
                        Note = p
                    })
                    .SelectMany(slide =>
                    {
                        var start = new[] { new
                        {
                            Tick = slide.Note.StartTick,
                            LaneIndex = slide.Note.StartLaneIndex,
                            Width = slide.Note.StartWidth,
                            Type = "1"
                        } };
                        var steps = slide.Note.StepNotes.OrderBy(p => p.TickOffset).Select(p => new
                        {
                            Tick = p.Tick,
                            LaneIndex = p.LaneIndex,
                            Width = p.Width,
                            Type = p.IsVisible ? "3" : "5"
                        }).Take(slide.Note.StepNotes.Count - 1);
                        var endNote = slide.Note.StepNotes.OrderBy(p => p.TickOffset).Last();
                        var end = new[] { new
                        {
                            Tick = endNote.Tick,
                            LaneIndex = endNote.LaneIndex,
                            Width = endNote.Width,
                            Type = "2"
                        } };
                        var slideNotes = start.Concat(steps).Concat(end);
                        var items = slideNotes.Select(p => (p.Tick, p.LaneIndex, p.Type + ToLaneWidthString(p.Width)));
                        return GetLongNoteLines("3", slide.Identifier.ToString(), items);
                    });
                WriteLinesWithOffset(writer, slides);
                writer.WriteLine();
                identifier.Clear();

                var airActions = notes.AirActions
                    .OrderBy(p => p.StartTick)
                    .Select(p => new
                    {
                        Identifier = identifier.Allocate(p.StartTick, p.GetDuration()),
                        Note = p
                    })
                    .SelectMany(airAction =>
                    {
                        var start = new[] { new
                        {
                            Tick = airAction.Note.StartTick,
                            Type = "1"
                        } };
                        var actions = airAction.Note.ActionNotes.OrderBy(p => p.Offset).Select(p => new
                        {
                            Tick = p.ParentNote.StartTick + p.Offset,
                            Type = "3"
                        }).Take(airAction.Note.ActionNotes.Count - 1);
                        var endNote = airAction.Note.ActionNotes.OrderBy(p => p.Offset).Last();
                        var end = new[] { new
                        {
                            Tick = airAction.Note.StartTick + endNote.Offset,
                            Type = "2"
                        } };
                        var actionNotes = start.Concat(actions).Concat(end);
                        var items = actionNotes.Select(p => (p.Tick, airAction.Note.ParentNote.LaneIndex, p.Type + ToLaneWidthString(airAction.Note.ParentNote.Width)));
                        return GetLongNoteLines("4", airAction.Identifier.ToString(), items);
                    });
                WriteLinesWithOffset(writer, airActions);
            }
            */
        }

        protected void WriteLinesWithOffset(TextWriter writer, IEnumerable<SusDataLine> items)
        {
            int GetActualBarIndex(SusDataLine line) => line.BarIndex + (line.IsInitialEvent ? 0 : BarIndexOffset);
            var grouped = items.GroupBy(p => GetActualBarIndex(p) / 1000).OrderBy(p => p.Key);
            bool shifted = false;
            foreach (var chunk in grouped)
            {
                if (chunk.Key > 0)
                {
                    shifted = true;
                    writer.WriteLine($"#MEASUREBS {chunk.Key * 1000}");
                }

                foreach (var item in chunk) writer.WriteLine(item.ResolveWithBarIndex(GetActualBarIndex(item) % 1000));
            }
            if (shifted)
            {
                writer.WriteLine("#MEASUREBS 0");
            }
        }

        IEnumerable<SusDataLine> GetShortNoteLines(string type, IEnumerable<(int Tick, int LaneIndex, string Data)> items)
        {
            foreach (var itemsInBar in items.Select(p => new { BarPosition = BarIndexCalculator.GetBarPositionFromTick(p.Tick), Item = p }).GroupBy(p => p.BarPosition.BarIndex))
            {
                foreach (var itemsInLane in itemsInBar.GroupBy(p => p.Item.LaneIndex))
                {
                    var sig = BarIndexCalculator.GetTimeSignatureFromBarIndex(itemsInBar.Key);
                    int barLength = StandardBarTick * sig.Numerator / sig.Denominator;

                    var offsetGroups = itemsInLane.GroupBy(p => p.BarPosition.TickOffset).Select(p => p.ToList()).ToList();
                    var separatedItemList = Enumerable.Range(0, offsetGroups.Max(p => p.Count)).Select(p => offsetGroups.Where(q => q.Count >= p + 1).Select(q => q[p]));

                    foreach (var separatedItems in separatedItemList)
                    {
                        var lineItems = separatedItems.Select(p => (p.BarPosition.TickOffset, p.Item.Data));
                        yield return new SusDataLine(itemsInBar.Key, barIndex => string.Format("#{0:000}{1}{2}: {3}", barIndex, type, itemsInLane.Key.ToString("x"), GenerateLineData(barLength, lineItems)));
                    }
                }
            }
        }

        IEnumerable<SusDataLine> GetLongNoteLines(string type, string key, IEnumerable<(int Tick, int LaneIndex, string Data)> elements)
        {
            foreach (var itemsInBar in elements.Select(p => new { BarPosition = BarIndexCalculator.GetBarPositionFromTick(p.Tick), Item = p }).GroupBy(p => p.BarPosition.BarIndex))
            {
                foreach (var itemsInLane in itemsInBar.GroupBy(p => p.Item.LaneIndex))
                {
                    var sig = BarIndexCalculator.GetTimeSignatureFromBarIndex(itemsInBar.Key);
                    int barLength = StandardBarTick * sig.Numerator / sig.Denominator;
                    var lineItems = itemsInLane.Select(p => (p.BarPosition.TickOffset, p.Item.Data));
                    yield return new SusDataLine(itemsInBar.Key, barIndex => string.Format("#{0:000}{1}{2}{3}: {4}", barIndex, type, itemsInLane.Key.ToString("x"), key, GenerateLineData(barLength, lineItems)));
                }
            }
        }

        protected string GenerateLineData(int barTick, IEnumerable<(int TickOffset, string Data)> items)
        {
            if (items.Any(p => p.TickOffset < 0 || p.TickOffset >= barTick)) throw new ArgumentException("Invalid TickOffset");
            if (items.Any(p => p.Data.Length != 2)) throw new ArgumentException("The data string length is not equal to 2.");

            int gcd = items.Select(p => p.TickOffset).Aggregate(barTick, (p, q) => GetGcd(p, q));
            var data = items.ToDictionary(p => p.TickOffset, p => p.Data);
            var sb = new StringBuilder();
            for (int i = 0; i * gcd < barTick; i++)
            {
                int tickOffset = i * gcd;
                if (data.ContainsKey(tickOffset)) sb.Append(data[tickOffset]);
                else sb.Append("00");
            }
            return sb.ToString();
        }

        public static int GetGcd(int a, int b)
        {
            if (a < b) return GetGcd(b, a);
            if (b == 0) return a;
            return GetGcd(b, a % b);
        }

        public static string ToLaneWidthString(int width)
        {
            return width == 16 ? "g" : width.ToString("x");
        }

        public static readonly IEnumerable<string> NumChars = Enumerable.Range(0, 10).Select(p => ((char)('0' + p)).ToString());
        public static readonly IEnumerable<string> AlphaChars = Enumerable.Range(0, 26).Select(p => ((char)('A' + p)).ToString());

        private static IEnumerable<string> EnumerateIdentifiers(int digits, IEnumerable<string> seq)
        {
            if (digits < 1) throw new ArgumentOutOfRangeException("digits");
            if (digits == 1) return seq;
            return EnumerateIdentifiers(digits - 1, seq).SelectMany(p => seq.Select(q => p + q));
        }

        public class SusDataLine
        {
            private readonly Func<int, string> resolver;

            public int BarIndex { get; }
            public bool IsInitialEvent { get; }

            public string ResolveWithBarIndex(int barIndex)
            {
                if (barIndex < 0 || barIndex > 1000) throw new ArgumentOutOfRangeException("barIndex", "Invalid bar index");
                return resolver(barIndex);
            }

            public SusDataLine(int barIndex, Func<int, string> resolver) : this(barIndex, resolver, false)
            {
            }

            public SusDataLine(int barIndex, Func<int, string> resolver, bool isInitialEvent)
            {
                BarIndex = barIndex;
                this.resolver = resolver;
                IsInitialEvent = isInitialEvent;
            }
        }

        public class IdentifierAllocationManager
        {
            private int lastStartTick;
            private Stack<char> IdentifierStack;
            private ConcurrentPriorityQueue<Tuple<int, char>, int> UsedIdentifiers;

            public IdentifierAllocationManager()
            {
                Clear();
            }

            public void Clear()
            {
                lastStartTick = 0;
                IdentifierStack = new Stack<char>(EnumerateIdentifiers(1, AlphaChars.Concat(NumChars)).Select(p => p.Single()).Reverse());
                UsedIdentifiers = new ConcurrentPriorityQueue<Tuple<int, char>, int>();
            }

            public char Allocate(int startTick, int duration)
            {
                if (startTick < lastStartTick) throw new InvalidOperationException("startTick must not be less than last called value.");
                while (UsedIdentifiers.Count > 0 && UsedIdentifiers.Peek().Item1 < startTick)
                {
                    IdentifierStack.Push(UsedIdentifiers.Dequeue().Item2);
                }
                char c = IdentifierStack.Pop();
                int endTick = startTick + duration;
                UsedIdentifiers.Enqueue(Tuple.Create(endTick, c), -endTick);
                lastStartTick = startTick;
                return c;
            }
        }
    }

    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class SusArgs
    {
        [Newtonsoft.Json.JsonProperty]
        private string playLevel;
        [Newtonsoft.Json.JsonProperty]
        private Difficulty playDificulty;
        [Newtonsoft.Json.JsonProperty]
        private string extendedDifficulty;
        [Newtonsoft.Json.JsonProperty]
        private string songId;
        [Newtonsoft.Json.JsonProperty]
        private string soundFileName;
        [Newtonsoft.Json.JsonProperty]
        private decimal soundOffset;
        [Newtonsoft.Json.JsonProperty]
        private string jacketFilePath;
        [Newtonsoft.Json.JsonProperty]
        private bool hasPaddingBar;
        [Newtonsoft.Json.JsonProperty]
        private string additionalData;

        public string PlayLevel
        {
            get { return playLevel; }
            set { playLevel = value; }
        }

        public Difficulty PlayDifficulty
        {
            get { return playDificulty; }
            set { playDificulty = value; }
        }

        public string ExtendedDifficulty
        {
            get { return extendedDifficulty; }
            set { extendedDifficulty = value; }
        }

        public string SongId
        {
            get { return songId; }
            set { songId = value; }
        }

        public string SoundFileName
        {
            get { return soundFileName; }
            set { soundFileName = value; }
        }

        public decimal SoundOffset
        {
            get { return soundOffset; }
            set { soundOffset = value; }
        }

        public string JacketFilePath
        {
            get { return jacketFilePath; }
            set { jacketFilePath = value; }
        }

        public bool HasPaddingBar
        {
            get => hasPaddingBar;
            set => hasPaddingBar = value;
        }

        public string AdditionalData
        {
            get => additionalData;
            set => additionalData = value;
        }

        public enum Difficulty
        {
            [Description("BASIC")]
            Basic,
            [Description("ADVANCED")]
            Advanced,
            [Description("EXPERT")]
            Expert,
            [Description("MASTER")]
            Master,
            [Description("WORLD'S END")]
            WorldsEnd
        }
    }
}
