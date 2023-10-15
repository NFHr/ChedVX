using ChedVX.Core.Notes;
using ChedVX.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChedVX.Core.Constants;

namespace ChedVX.UI.Operations
{
    public abstract class EditNoteOperation : IOperation
    {
        protected NoteBase Note { get; }
        public abstract string Description { get; }

        public EditNoteOperation(NoteBase note)
        {
            Note = note;
        }

        public abstract void Redo();
        public abstract void Undo();
    }

    public class MoveNoteTickOperation : EditNoteOperation
    {
        public override string Description { get { return "Move Notes' tick"; } }

        protected NotePosition BeforePosition { get; }
        protected NotePosition AfterPosition { get; }

        public MoveNoteTickOperation(NoteBase note, NotePosition before, NotePosition after) : base(note)
        {
            BeforePosition = before;
            AfterPosition = after;
        }

        public override void Redo()
        {
            Note.StartTick = AfterPosition.StartTick;
        }

        public override void Undo()
        {
            Note.StartTick = BeforePosition.StartTick;
        }

        public struct NotePosition
        {
            public int StartTick { get; }

            public NotePosition(int tick)
            {
                StartTick = tick;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is NotePosition)) return false;
                NotePosition other = (NotePosition)obj;
                return StartTick == other.StartTick;
            }

            public override int GetHashCode()
            {
                return StartTick;
            }

            public static bool operator ==(NotePosition a, NotePosition b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(NotePosition a, NotePosition b)
            {
                return !a.Equals(b);
            }
        }
    }

    public class ChangeDurationOnlyOperation : IOperation
    {
        public string Description { get { return "Change FX/BT Long Length"; } }

        protected NoteBase Note { get; }
        protected int BeforeDuration { get; }
        protected int AfterDuration { get; }

        public ChangeDurationOnlyOperation(NoteBase note, int beforeDuration, int afterDuration)
        {
            Note = note;
            BeforeDuration = beforeDuration;
            AfterDuration = afterDuration;
        }

        public void Redo()
        {
            Note.Duration = AfterDuration;
        }

        public void Undo()
        {
            Note.Duration = BeforeDuration;
        }
    }


    public class MoveLaserOperation : IOperation
    {
        public string Description { get { return "Move Slide"; } }

        protected LaserNote Note;
        protected LaserPosition BeforePosition { get; }
        protected LaserPosition AfterPosition { get; }

        public MoveLaserOperation(LaserNote note, LaserPosition before, LaserPosition after)
        {
            Note = note;
            BeforePosition = before;
            AfterPosition = after;
        }

        public void Redo()
        {
            Note.StartTick = AfterPosition.StartTick;
            Note.SetPosition(AfterPosition.StartPosition, AfterPosition.EndPosition);
        }

        public void Undo()
        {
            Note.StartTick = BeforePosition.StartTick;
            Note.SetPosition(BeforePosition.StartPosition, BeforePosition.EndPosition);
        }

        public struct LaserPosition
        {
            public int StartTick { get; }
            public int StartPosition { get; }
            public int EndPosition { get; }

            public LaserPosition(int startTick, int startPosition, int endPosition)
            {
                StartTick = startTick;
                StartPosition = startPosition;
                EndPosition = endPosition;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is LaserPosition)) return false;
                LaserPosition other = (LaserPosition)obj;
                return StartTick == other.StartTick && StartPosition == other.StartPosition && EndPosition == other.EndPosition;
            }

            public override int GetHashCode()
            {
                return StartTick ^ StartPosition ^ EndPosition;
            }

            public static bool operator ==(LaserPosition a, LaserPosition b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(LaserPosition a, LaserPosition b)
            {
                return !a.Equals(b);
            }
        }
    }

    public class ChangeLaserScaleOperation : IOperation
    {
        public string Description { get { return "Change Laser Scale"; } }

        protected LaserNote Note { get; }
        protected int BeforeScale { get; }
        protected int AfterScale { get; }

        public ChangeLaserScaleOperation(LaserNote note, int beforeDuration, int afterDuration)
        {
            Note = note;
            BeforeScale = beforeDuration;
            AfterScale = afterDuration;
        }

        public void Redo()
        {
            Note.Scale = AfterScale;
        }

        public void Undo()
        {
            Note.Scale = BeforeScale;
        }
    }

    public class ChangeLaserEasingOperation : IOperation
    {
        public string Description { get { return "Change Laser Easing"; } }

        protected LaserNote Note { get; }
        protected Easing BeforeEasing { get; }
        protected Easing AfterEasing { get; }

        public ChangeLaserEasingOperation(LaserNote note, Easing beforeEasing, Easing afterEasing)
        {
            Note = note;
            BeforeEasing = beforeEasing;
            AfterEasing = afterEasing;
        }

        public void Redo()
        {
            Note.LineEasing = AfterEasing;
        }

        public void Undo()
        {
            Note.LineEasing = BeforeEasing;
        }
    }

    public class FlipLaserOperation : IOperation
    {
        public string Description { get { return "Filp laser"; } }

        protected LaserNote Note;

        public FlipLaserOperation(LaserNote note)
        {
            Note = note;
        }

        public void Redo()
        {
            Note.Flip();
        }

        public void Undo()
        {
            Note.Flip();
        }
    }
}
