using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.UI.Operations
{
    /// <summary>
    /// This is a class that manages operations.
    /// </summary>
    public class OperationManager
    {
        public event EventHandler OperationHistoryChanged;
        public event EventHandler ChangesCommitted;

        protected Stack<IOperation> UndoStack { get; } = new Stack<IOperation>();
        protected Stack<IOperation> RedoStack { get; } = new Stack<IOperation>();

        private IOperation LastCommittedOperation { get; set; } = null;

        /// <summary>
        /// Gets a collection of undo operations summaries.
        /// </summary>
        public IEnumerable<string> UndoOperationsDescription
        {
            get { return UndoStack.Select(p => p.Description); }
        }

        /// <summary>
        /// Gets a collection of summaries of operations to redo.
        /// </summary>
        public IEnumerable<string> RedoOperationsDescription
        {
            get { return RedoStack.Select(p => p.Description); }
        }

        /// <summary>
        /// Get whether the operation can be undone.
        /// </summary>
        public bool CanUndo { get { return UndoStack.Count > 0; } }

        /// <summary>
        /// Get whether the operation can be redone.
        /// </summary>
        public bool CanRedo { get { return RedoStack.Count > 0; } }

        /// <summary>
        /// Get whether changes have been made since the last call to <see cref="CommitChanges"/>.
        /// </summary>
        public bool IsChanged { get { return LastCommittedOperation != (UndoStack.Count > 0 ? UndoStack.Peek() : null); } }

        /// <summary>
        /// Record a new operation.
        /// </summary>
        /// <param name="op">Operation to record</param>
        public void Push(IOperation op)
        {
            UndoStack.Push(op);
            RedoStack.Clear();
            OperationHistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Perform the operation and record it.
        /// </summary>
        /// <param name="op">Operation to perform/record</param>
        public void InvokeAndPush(IOperation op)
        {
            op.Redo();
            Push(op);
        }

        /// <summary>
        /// Undoes the previous action.
        /// </summary>
        public void Undo()
        {
            IOperation op = UndoStack.Pop();
            op.Undo();
            RedoStack.Push(op);
            OperationHistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Redo the immediately following operation.
        /// </summary>
        public void Redo()
        {
            IOperation op = RedoStack.Pop();
            op.Redo();
            UndoStack.Push(op);
            OperationHistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Clear recorded operations.
        /// </summary>
        public void Clear()
        {
            UndoStack.Clear();
            RedoStack.Clear();
            LastCommittedOperation = null;
            OperationHistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Notifies that the current state of <see cref="OperationManager"/> has been saved.
        /// </summary>
        public void CommitChanges()
        {
            LastCommittedOperation = UndoStack.Count > 0 ? UndoStack.Peek() : null;
            ChangesCommitted?.Invoke(this, EventArgs.Empty);
        }
    }
}
