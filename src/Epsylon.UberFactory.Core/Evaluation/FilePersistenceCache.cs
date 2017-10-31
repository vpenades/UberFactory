using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberFactory.Evaluation
{
    class _PersistenceCache
    {
    }

    class _TaskPersistence
    {
        public Guid TaskId { get; private set; }
        public DateTime TimeStamp { get; private set; } // hash

        private readonly List<_FilePersistence> _InputFiles = new List<_FilePersistence>();
        private readonly List<_FilePersistence> _OutputFiles = new List<_FilePersistence>();
    }

    class _FilePersistence
    {
        public PathString FileName { get; private set; }
        public long FileLength { get; private set; }
        public DateTime TimeStamp { get; private set; }
    }
}
