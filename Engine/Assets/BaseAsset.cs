using System;

namespace Engine
{
    public class BaseAsset : IDisposable
    {
        public string Guid { get; private set; }

        private bool disposed = false;

        internal BaseAsset WithGuid(string guid)
        {
            Guid = guid;
            return this;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                //NOTE: dispose managed state (managed objects)
            }
            
            //NOTE: free unmanaged resources (unmanaged objects) and override finalizer
            //NOTE: set large fields to null
            
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //NOTE:only use finalizers if object has unmanaged resources!!!
        // ~BaseAsset()
        // {
        //     Dispose(disposing: false);
        // }
        
        //NOTE: Protected implementation of Dispose pattern.
        // protected override void Dispose(bool disposing)
        // {
        //     if (disposed)
        //         return;
        //
        //     if (disposing)
        //     {
        //         // Free managed
        //     }
        //     //Free unmanaged
        //     disposed = true;
        //     
        //     base.Dispose(disposing);
        // }
    }
}