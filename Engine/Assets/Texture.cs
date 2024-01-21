using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using Rectangle = System.Drawing.Rectangle;
using ImagingPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Engine
{
    public class Texture : BaseAsset
    {
        public Texture2D texture { get; private set; }
        public ImageSource Source { get; private set; }

        private List<IResourceViewCollection> viewsCollectons = new List<IResourceViewCollection>();

        private bool disposed = false;

        #region Legacy

        public static Bitmap DecodeTexture(byte[] bytes)
        {
            Stream stream = new MemoryStream(bytes);
            BitmapDecoder decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.Default);
            return GetBitmap(decoder.Frames[0]);
        }

        public static Bitmap GetBitmap(BitmapSource source)
        {
            Bitmap bmp = new Bitmap(
                source.PixelWidth,
                source.PixelHeight,
                ImagingPixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(
                new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size),
                ImageLockMode.WriteOnly,
                ImagingPixelFormat.Format32bppPArgb);
            source.CopyPixels(
                Int32Rect.Empty,
                data.Scan0,
                data.Height * data.Stride,
                data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        public Texture(Bitmap image, bool applyGammaCorrection = true)
        {
            if (image.PixelFormat != ImagingPixelFormat.Format32bppArgb)
                image = image.Clone(new Rectangle(0, 0, image.Width, image.Height), ImagingPixelFormat.Format32bppArgb);
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, ImagingPixelFormat.Format32bppArgb);

            texture = new Texture2D(GraphicsCore.CurrentDevice, new Texture2DDescription()
            {
                Width = image.Width,
                Height = image.Height,
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                Usage = ResourceUsage.Immutable,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = applyGammaCorrection ? Format.B8G8R8A8_UNorm_SRgb : Format.B8G8R8A8_UNorm,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.Shared,
                SampleDescription = new SampleDescription(1, 0)
            }, new DataRectangle(data.Scan0, data.Stride));

            image.UnlockBits(data);

            GenerateViews();
        }

        #endregion

        public Texture() { }

        public Texture(int width, int height, IEnumerable<byte>? defaultDataPerPixel, Format textureFormat, BindFlags usage, int arraySize = 1, int mipLevels = 1)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width", "Texture width must be positive.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException("height", "Texture height must be positive.");

            Format[] supportedFormats =
            {
                Format.B8G8R8A8_UNorm,
                Format.B8G8R8A8_UNorm_SRgb,
                Format.R32G32B32A32_Float,
                Format.R32_Typeless
            };
            if (!supportedFormats.Contains(textureFormat))
                throw new NotSupportedException("Texture format is not supported: %s" + textureFormat.ToString());

            nint dataPtr = 0;
            DataRectangle[]? rectangles = null;
            if (defaultDataPerPixel != null)
            {
                int bytesPerPixel = textureFormat.SizeOfInBytes();

                byte[] data = new byte[width * height * bytesPerPixel];

                IEnumerator<byte> enumerator = defaultDataPerPixel.GetEnumerator();
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        int pos = (i * width + j) * bytesPerPixel;
                        for (int k = 0; k < bytesPerPixel && enumerator.MoveNext(); k++)
                            data[pos + k] = enumerator.Current;
                        enumerator.Reset();
                    }
                }

                dataPtr = Marshal.AllocHGlobal(width * height * bytesPerPixel);
                Marshal.Copy(data, 0, dataPtr, width * height * bytesPerPixel);

                rectangles = new DataRectangle[arraySize];
                for (int i = 0; i < arraySize; i++)
                    rectangles[i] = new DataRectangle(dataPtr, width * bytesPerPixel);
            }
            texture = new Texture2D(GraphicsCore.CurrentDevice, new Texture2DDescription()
            {
                Width = width,
                Height = height,
                ArraySize = arraySize,
                BindFlags = usage,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = textureFormat,
                MipLevels = mipLevels,
                OptionFlags = ResourceOptionFlags.Shared,
                SampleDescription = new SampleDescription(1, 0)
            }, rectangles);

            if (defaultDataPerPixel != null)
                Marshal.FreeHGlobal(dataPtr);

            GenerateViews();
        }

        public Texture WithBitmapSource(Texture2DDescription description, BitmapSource bitmapSource)
        {
            int strideLength = description.Width * description.Format.SizeOfInBytes();
            int bytesCount = description.Height * strideLength;
            nint dataPtr = Marshal.AllocHGlobal(bytesCount);

            bitmapSource.CopyPixels(Int32Rect.Empty, dataPtr, bytesCount, strideLength);
            DataRectangle dataRectangle = new DataRectangle(dataPtr, strideLength);

            Source = bitmapSource;
            return WithRawData(description, dataRectangle);
        }

        public Texture WithRawData(Texture2DDescription description, DataRectangle dataRectangle)
        {
            texture?.Dispose();
            viewsCollectons.Clear();

            texture = new Texture2D(GraphicsCore.CurrentDevice, description, dataRectangle);
            GenerateViews();
            return this;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                texture?.Dispose();
                Source = null;
            }
            disposed = true;

            base.Dispose(disposing);
        }

        #region ResourceViews

        private void GenerateViews()
        {
            Format format = texture.Description.Format;
            if (format == Format.R32_Typeless)
                format = Format.R32_Float;
            BindFlags usage = texture.Description.BindFlags;

            int arraySize = texture.Description.ArraySize;
            int mipLevels = texture.Description.MipLevels;

            bool hasRenderTargets = usage.HasFlag(BindFlags.RenderTarget);
            bool hasDepthStencils = usage.HasFlag(BindFlags.DepthStencil);
            bool hasShaderResources = usage.HasFlag(BindFlags.ShaderResource);

            if (hasRenderTargets)
                viewsCollectons.Add(CreateResourceViewCollection<RenderTargetView>(format, arraySize, mipLevels));
            if (hasDepthStencils)
                viewsCollectons.Add(CreateResourceViewCollection<DepthStencilView>(format, arraySize, mipLevels));
            if (hasShaderResources)
                viewsCollectons.Add(CreateResourceViewCollection<ShaderResourceView>(format, arraySize, mipLevels));
        }

        private ResourceViewCollection<T> CreateResourceViewCollection<T>(Format format, int arraySize, int mipLevels)
            where T : ResourceView
        {
            if (arraySize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(arraySize));
            }

            if (mipLevels < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(mipLevels));
            }

            ResourceViewCollection<T> collection = new ResourceViewCollection<T>
            {
                ArrayViews = new List<T>(arraySize),
                MipsViews = new List<List<T>>(arraySize)
            };

            collection.GeneralView = collection switch
            {
                ResourceViewCollection<RenderTargetView> => CreateRenderTargetView(texture, format, arraySize, 0, 0) as T,
                ResourceViewCollection<DepthStencilView> => CreateDepthStencilView(texture, arraySize, 0, 0) as T,
                ResourceViewCollection<ShaderResourceView> => CreateShaderResourceView(texture, format, arraySize, 0, mipLevels, 0) as T,
                _ => throw new NotImplementedException(typeof(T).Name)
            };

            if (arraySize > 1 || mipLevels > 1)
            {
                for (int i = 0; i < arraySize; i++)
                {
                    T arrayItemView = collection switch
                    {
                        ResourceViewCollection<RenderTargetView> => CreateRenderTargetView(texture, format, 1, i, 0) as T,
                        ResourceViewCollection<DepthStencilView> => CreateDepthStencilView(texture, 1, i, 0) as T,
                        ResourceViewCollection<ShaderResourceView> => CreateShaderResourceView(texture, format, 1, i, mipLevels, 0) as T,
                        _ => throw new NotImplementedException(typeof(T).Name)
                    };

                    collection.ArrayViews.Add(arrayItemView);

                    collection.MipsViews.Add(new List<T>(mipLevels));

                    int mipStartInd = 1;

                    if (arrayItemView is not ShaderResourceView)
                    {
                        collection.MipsViews[0].Add(arrayItemView);
                    }
                    else
                    {
                        mipStartInd = 0;
                    }

                    for (int j = mipStartInd; j < mipLevels; j++)
                    {
                        T mipItemView = collection switch
                        {
                            ResourceViewCollection<RenderTargetView> => CreateRenderTargetView(texture, format, 1, i, j) as T,
                            ResourceViewCollection<DepthStencilView> => CreateDepthStencilView(texture, 1, i, j) as T,
                            ResourceViewCollection<ShaderResourceView> => CreateShaderResourceView(texture, format, 1, i, 1, j) as T,
                            _ => throw new NotImplementedException(typeof(T).Name)
                        };

                        collection.MipsViews[i].Add(mipItemView);
                    }
                }
            }
            else
            {
                collection.ArrayViews.Add(collection.GeneralView);
                collection.MipsViews.Add(new List<T> { collection.GeneralView });
            }

            return collection;
        }

        private RenderTargetView CreateRenderTargetView(Texture2D rawTexture, Format format, int arraySize, int arraySlice, int mipSlice)
        {
            return new RenderTargetView(GraphicsCore.CurrentDevice, rawTexture,
                                        new RenderTargetViewDescription()
                                        {
                                            Format = format,
                                            Dimension = RenderTargetViewDimension.Texture2DArray,
                                            Texture2DArray = new RenderTargetViewDescription.Texture2DArrayResource()
                                            {
                                                MipSlice = mipSlice,
                                                ArraySize = arraySize,
                                                FirstArraySlice = arraySlice
                                            }
                                        }
            );
        }

        private DepthStencilView CreateDepthStencilView(Texture2D rawTexture, int arraySize, int arraySlice, int mipSlice)
        {
            return new DepthStencilView(GraphicsCore.CurrentDevice, rawTexture,
                                        new DepthStencilViewDescription()
                                        {
                                            Format = Format.D32_Float,
                                            Dimension = DepthStencilViewDimension.Texture2DArray,
                                            Texture2DArray = new DepthStencilViewDescription.Texture2DArrayResource()
                                            {
                                                MipSlice = mipSlice,
                                                ArraySize = arraySize,
                                                FirstArraySlice = arraySlice
                                            }
                                        }
            );
        }

        private ShaderResourceView CreateShaderResourceView(Texture2D rawTexture, Format format, int arraySize, int arraySlice, int mipLevels, int mipSlice)
        {
            return new ShaderResourceView(GraphicsCore.CurrentDevice, rawTexture,
                                          new ShaderResourceViewDescription()
                                          {
                                              Format = format,
                                              Dimension = ShaderResourceViewDimension.Texture2DArray,
                                              Texture2DArray = new ShaderResourceViewDescription.Texture2DArrayResource()
                                              {
                                                  MipLevels = mipLevels,
                                                  MostDetailedMip = mipSlice,
                                                  ArraySize = arraySize,
                                                  FirstArraySlice = arraySlice
                                              }
                                          }
            );
        }

        private ResourceViewCollection<T> GetResourceViewCollection<T>() where T : ResourceView
        {
            return (ResourceViewCollection<T>)viewsCollectons.First(view => view is ResourceViewCollection<T>);
        }

        /// <summary>
        /// Check if texture contains ResourceView of specified type
        /// </summary>
        /// <typeparam name="T">Type of ResourceView</typeparam>
        /// <returns>True if contains otherwize false</returns>
        public bool HasViews<T>() where T : ResourceView
        {
            return viewsCollectons.Any(v => v is ResourceViewCollection<T>);
        }

        /// <summary>
        /// Get specified type of texture's ResourceView
        /// </summary>
        /// <typeparam name="T">Type of ResourceView</typeparam>
        /// <returns>ResourceView of specified type</returns>
        public T GetView<T>() where T : ResourceView
        {
            return GetResourceViewCollection<T>().GeneralView;
        }

        /// <summary>
        /// Get specified type of array slice texture's ResourceView
        /// </summary>
        /// <typeparam name="T">Type of ResourceView</typeparam>
        /// <param name="arraySlice">Array slice index</param>
        /// <returns>ResourceView of specified type</returns>
        public T GetView<T>(int arraySlice) where T : ResourceView
        {
            return GetResourceViewCollection<T>().ArrayViews[arraySlice];
        }

        /// <summary>
        /// Get specified type of mip slice texture's ResourceView
        /// </summary>
        /// <typeparam name="T">Type of ResourceView</typeparam>
        /// <param name="arraySlice">Array slice index</param>
        /// <param name="mipSlice">Mip slice index</param>
        /// <returns>ResourceView of specified type</returns>
        public T GetView<T>(int arraySlice, int mipSlice) where T : ResourceView
        {
            return GetResourceViewCollection<T>().MipsViews[arraySlice][mipSlice];
        }

        /// <summary>
        /// Bind texture to current ShaderPipeline as ShaderResourceView
        /// </summary>
        /// <param name="variable">Name of texture variable in shaders</param>
        public void Use(string variable)
        {
            UseInternal(variable, GetView<ShaderResourceView>());
        }

        /// <summary>
        /// Bind array slice of texture to current ShaderPipeline as ShaderResourceView
        /// </summary>
        /// <param name="variable">Name of texture variable in shaders</param>
        /// <param name="arraySlice">Array slice index</param>
        public void Use(string variable, int arraySlice)
        {
            UseInternal(variable, GetView<ShaderResourceView>(arraySlice));
        }

        /// <summary>
        /// Bind mip slice of texture to current ShaderPipeline as ShaderResourceView
        /// </summary>
        /// <param name="variable">Name of texture variable in shaders</param>
        /// <param name="arraySlice">Array slice index</param>
        /// <param name="mipSlice">Mip slice index</param>
        public void Use(string variable, int arraySlice, int mipSlice)
        {
            UseInternal(variable, GetView<ShaderResourceView>(arraySlice, mipSlice));
        }

        private void UseInternal(string variable, ShaderResourceView view)
        {
            if ((texture.Description.BindFlags & BindFlags.ShaderResource) == BindFlags.None)
                throw new Exception("This texture is not a shader resource");
            bool correctLocation = false;
            int location;
            foreach (Shader shader in ShaderPipeline.Current.Shaders)
            {
                if (shader.Locations.TryGetValue(variable, out location))
                {
                    correctLocation = true;
                    GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetShaderResource(location, view);
                }
            }
            if (!correctLocation)
                throw new ArgumentException("Variable " + variable + " not found in current pipeline.");
        }

        private interface IResourceViewCollection { }
        private struct ResourceViewCollection<T> : IResourceViewCollection where T : ResourceView
        {
            public T GeneralView;
            public List<T> ArrayViews;
            public List<List<T>> MipsViews;
        }

        #endregion ResourceViews

    }
}