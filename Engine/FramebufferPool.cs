using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class FrameBufferPool : IDisposable
    {
        private List<FrameBuffer> frameBuffers = new List<FrameBuffer>();
        private Stack<FrameBuffer> freeFrameBuffers = new Stack<FrameBuffer>();
        private bool disposed;

        public int PoolCapacity => frameBuffers.Count();

        public int Width { get; private set; }
        public int Height { get; private set; }

        public FrameBufferPool(int capacity, int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;

            for (int i = 0; i < capacity; i++)
            {
                frameBuffers.Add(new FrameBuffer(width, height));
                freeFrameBuffers.Push(frameBuffers[i]);
            }
        }

        public FrameBuffer Get()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(FrameBufferPool));

            if (freeFrameBuffers.Count == 0)
            {
                int capacity = PoolCapacity;
                for (int i = 0; i < capacity; i++)
                {
                    FrameBuffer framebuffer = new FrameBuffer(Width, Height);
                    frameBuffers.Add(framebuffer);
                    freeFrameBuffers.Push(framebuffer);
                }
            }

            return freeFrameBuffers.Pop();
        }

        public void Release(FrameBuffer curFrameBuffer)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(FrameBufferPool));

            foreach (FrameBuffer fb in frameBuffers)
                if (fb == curFrameBuffer)
                {
                    freeFrameBuffers.Push(curFrameBuffer);
                    return;
                }
            throw new ArgumentException("This FrameBuffer does not exist in this FrameBufferPool");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                foreach (FrameBuffer fb in frameBuffers)
                    fb.Dispose();

                disposed = true;
            }
        }

        ~FrameBufferPool()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}