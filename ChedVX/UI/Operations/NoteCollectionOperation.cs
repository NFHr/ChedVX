using ChedVX.Core.Notes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.UI.Operations
{
    public abstract class NoteCollectionOperation<T> : IOperation
    {
        protected T Note { get; }
        protected NoteView.NoteCollection Collection { get; }
        public abstract string Description { get; }

        public NoteCollectionOperation(NoteView.NoteCollection collection, T note)
        {
            Collection = collection;
            Note = note;
        }

        public abstract void Undo();
        public abstract void Redo();
    }

    public class InsertBTOperation : NoteCollectionOperation<BTNote>
    {
        public override string Description { get { return "Insert BT Note"; } }

        public InsertBTOperation(NoteView.NoteCollection collection, BTNote note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Add(Note);
        }

        public override void Undo()
        {
            Collection.Remove(Note);
        }
    }

    public class RemoveBTOperation : NoteCollectionOperation<BTNote>
    {
        public override string Description { get { return "Remove BT Note"; } }

        public RemoveBTOperation(NoteView.NoteCollection collection, BTNote note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Remove(Note);
        }

        public override void Undo()
        {
            Collection.Add(Note);
        }
    }
    public class InsertFXOperation : NoteCollectionOperation<FXNote>
    {
        public override string Description { get { return "Insert FX Note"; } }

        public InsertFXOperation(NoteView.NoteCollection collection, FXNote note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Add(Note);
        }

        public override void Undo()
        {
            Collection.Remove(Note);
        }
    }

    public class RemoveFXOperation : NoteCollectionOperation<FXNote>
    {
        public override string Description { get { return "Remove FX Note"; } }

        public RemoveFXOperation(NoteView.NoteCollection collection, FXNote note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Remove(Note);
        }

        public override void Undo()
        {
            Collection.Add(Note);
        }
    }
    public class InsertLaserOperation : NoteCollectionOperation<LaserNote>
    {
        public override string Description { get { return "Insert Laser Note"; } }

        public InsertLaserOperation(NoteView.NoteCollection collection, LaserNote note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Add(Note);
        }

        public override void Undo()
        {
            Collection.Remove(Note);
        }
    }

    public class RemoveLaserOperation : NoteCollectionOperation<LaserNote>
    {
        public override string Description { get { return "Insert Laser Note"; } }

        public RemoveLaserOperation(NoteView.NoteCollection collection, LaserNote note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Remove(Note);
        }

        public override void Undo()
        {
            Collection.Add(Note);
        }
    }

}
