using System.Collections.Generic;

namespace Engine
{
    public class Scene : BaseAsset
    {
        public List<GameObject> objects { get; } = new List<GameObject>();

        private bool disposed = false;


        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing) { }
            disposed = true;

            base.Dispose(disposing);
        }
    }
}