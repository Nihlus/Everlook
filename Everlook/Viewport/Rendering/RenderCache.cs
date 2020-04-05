﻿//
//  RenderCache.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Everlook.Utility;
using Everlook.Viewport.Rendering.Core;
using Everlook.Viewport.Rendering.Shaders;
using JetBrains.Annotations;
using log4net;
using Silk.NET.OpenGL;
using Warcraft.BLP;
using Warcraft.Core;
using Warcraft.MDX.Visual;
using Warcraft.MPQ;
using SysPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Everlook.Viewport.Rendering
{
    /// <summary>
    /// OpenGL caching handler for objects that can be used more than once during a run of the program and
    /// may take some time to generate.
    ///
    /// Currently, these are textures and shader programs.
    /// </summary>
    public sealed class RenderCache : GraphicsObject, IDisposable
    {
        /// <summary>
        /// Logger instance for this class.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(RenderCache));

        /// <summary>
        /// The cache dictionary that maps active OpenGL textures on the GPU.
        /// </summary>
        private readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();

        /// <summary>
        /// The cache dictionary that maps active OpenGL shaders on the GPU.
        /// </summary>
        private readonly Dictionary<EverlookShader, ShaderProgram> _shaderCache =
            new Dictionary<EverlookShader, ShaderProgram>();

        /// <summary>
        /// Gets or sets a value indicating whether this object has been disposed.
        /// </summary>
        private bool IsDisposed { get; set; }

        /// <summary>
        /// Gets the the fallback texture.
        /// </summary>
        public Texture2D FallbackTexture
        {
            get
            {
                ThrowIfDisposed();

                if (!(_fallbackTextureInternal is null))
                {
                    return _fallbackTextureInternal;
                }

                var fallbackImage = ResourceManager.GetFallbackImage();
                if (fallbackImage is null)
                {
                    throw new InvalidOperationException();
                }

                _fallbackTextureInternal = new Texture2D(this.GL, fallbackImage);

                return _fallbackTextureInternal;
            }
        }

        private Texture2D? _fallbackTextureInternal;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderCache"/> class.
        /// </summary>
        /// <param name="gl">The OpenGL API.</param>
        public RenderCache([NotNull] GL gl)
            : base(gl)
        {
        }

        /// <summary>
        /// Gets a <see cref="ShaderProgram"/> for the specified shader type. If one is not already in the cache, it
        /// will be created.
        /// </summary>
        /// <param name="shader">The type of shader to retrieve.</param>
        /// <returns>A shader program object.</returns>
        public ShaderProgram GetShader(EverlookShader shader)
        {
            ThrowIfDisposed();

            if (HasCachedShader(shader))
            {
                return GetCachedShader(shader);
            }

            return CreateCachedShader(shader);
        }

        /// <summary>
        /// Determines whether or not the rendering cache has a cached texture id
        /// for the specified texture file path.
        /// </summary>
        /// <param name="texturePath">The path of the texture in its package group. Used as a lookup key.</param>
        /// <returns>true if a cached textures exists with the given path as a lookup key; false otherwise.</returns>
        public bool HasCachedTextureForPath(string texturePath)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(texturePath))
            {
                throw new ArgumentNullException(nameof(texturePath));
            }

            return _textureCache.ContainsKey
            (
                texturePath.ConvertPathSeparatorsToCurrentNativeSeparator().ToUpperInvariant()
            );
        }

        /// <summary>
        /// Determines whether or not the rendering cache has a cached shader
        /// for the specified shader type.
        /// </summary>
        private bool HasCachedShader(EverlookShader shader)
        {
            if (!Enum.IsDefined(typeof(EverlookShader), shader))
            {
                throw new ArgumentException("An unknown shader was passed to the rendering cache.", nameof(shader));
            }

            return _shaderCache.ContainsKey(shader);
        }

        /// <summary>
        /// Gets a cached texture ID from the rendering cache.
        /// </summary>
        /// <param name="texturePath">The path of the texture in its package group. Used as a lookup key.</param>
        /// <returns>A texture object.</returns>
        public Texture2D GetCachedTexture(string texturePath)
        {
            ThrowIfDisposed();

            return _textureCache[texturePath.ConvertPathSeparatorsToCurrentNativeSeparator().ToUpperInvariant()];
        }

        /// <summary>
        /// Gets a cached shader ID from the rendering cache.
        /// </summary>
        private ShaderProgram GetCachedShader(EverlookShader shader)
        {
            ThrowIfDisposed();

            return _shaderCache[shader];
        }

        /// <summary>
        /// Gets a <see cref="Texture2D"/> instance from the cache. If the texture is not already cached, it is
        /// extracted from the given <see cref="IGameContext"/>. If it is cached, the cached version is returned. If no
        /// texture can be extracted, a fallback texture is returned.
        /// </summary>
        /// <param name="texture">The texture definition.</param>
        /// <param name="gameContext">The context of the texture definition.</param>
        /// <param name="texturePathOverride">Optional. Overrides the filename in the texture definition.</param>
        /// <returns>A <see cref="Texture2D"/> object.</returns>
        public Texture2D GetTexture(MDXTexture texture, IGameContext gameContext, string? texturePathOverride = null)
        {
            ThrowIfDisposed();

            var filename = texture.Filename;
            if (string.IsNullOrEmpty(texture.Filename))
            {
                if (string.IsNullOrEmpty(texturePathOverride))
                {
                    Log.Warn("Texture with empty filename requested.");
                    return this.FallbackTexture;
                }

                filename = texturePathOverride!;
            }

            var wrapS = texture.Flags.HasFlag(MDXTextureFlags.TextureWrapX)
                ? TextureWrapMode.Repeat
                : TextureWrapMode.ClampToBorder;

            var wrapT = texture.Flags.HasFlag(MDXTextureFlags.TextureWrapY)
                ? TextureWrapMode.Repeat
                : TextureWrapMode.ClampToBorder;

            return GetTexture(filename!, gameContext.Assets, wrapS, wrapT);
        }

        /// <summary>
        /// Gets a <see cref="Texture2D"/> instance from the cache. If the texture is not already cached, it is
        /// extracted from the given <see cref="IPackage"/>. If it is cached, the cached version is returned. If no
        /// texture can be extracted, a fallback texture is returned.
        /// </summary>
        /// <param name="texturePath">The path to the texture in the package.</param>
        /// <param name="package">The package where the texture is stored.</param>
        /// <param name="wrappingModeS">The wrapping mode to use for the texture on the S axis.</param>
        /// <param name="wrappingModeT">The wrapping mode to use for the texture on the T axis.</param>
        /// <returns>A <see cref="Texture2D"/> object.</returns>
        public Texture2D GetTexture
        (
            string texturePath,
            IPackage package,
            TextureWrapMode wrappingModeS = TextureWrapMode.Repeat,
            TextureWrapMode wrappingModeT = TextureWrapMode.Repeat
        )
        {
            ThrowIfDisposed();

            if (HasCachedTextureForPath(texturePath))
            {
                return GetCachedTexture(texturePath);
            }

            var textureType = FileInfoUtilities.GetFileType(texturePath);
            switch (textureType)
            {
                case WarcraftFileType.BinaryImage:
                {
                    if (!package.TryExtractFile(texturePath, out var textureData))
                    {
                        return this.FallbackTexture;
                    }

                    var texture = new BLP(textureData);
                    return CreateCachedTexture(texture, texturePath, wrappingModeS, wrappingModeT);
                }
                case WarcraftFileType.BitmapImage:
                case WarcraftFileType.GIFImage:
                case WarcraftFileType.IconImage:
                case WarcraftFileType.PNGImage:
                case WarcraftFileType.JPGImage:
                case WarcraftFileType.TargaImage:
                {
                    if (!package.TryExtractFile(texturePath, out var data))
                    {
                        return this.FallbackTexture;
                    }

                    using var ms = new MemoryStream(data);
                    var texture = new Bitmap(ms);
                    return CreateCachedTexture(texture, texturePath);
                }
            }

            return this.FallbackTexture;
        }

        /// <summary>
        /// Creates a cached texture for the specified texture, using the specified path
        /// as a lookup key. This method will create a new texture, and cache it.
        /// </summary>
        /// /// <param name="imageData">A bitmap containing the image data.</param>
        /// <param name="texturePath">
        /// The path to the texture in its corresponding package group. This is used as a lookup key.
        /// </param>
        /// <param name="wrappingModeS">How the texture should wrap on the S axis.</param>
        /// <param name="wrappingModeT">How the texture should wrap on the T axis.</param>
        /// <returns>A new cached texture created from the data.</returns>
        public Texture2D CreateCachedTexture
        (
            BLP imageData,
            string texturePath,
            TextureWrapMode wrappingModeS = TextureWrapMode.Repeat,
            TextureWrapMode wrappingModeT = TextureWrapMode.Repeat
        )
        {
            ThrowIfDisposed();

            var texture = new Texture2D(this.GL, imageData, wrappingModeS, wrappingModeT);

            _textureCache.Add(texturePath.ConvertPathSeparatorsToCurrentNativeSeparator().ToUpperInvariant(), texture);
            return texture;
        }

        /// <summary>
        /// Creates a cached texture for the specified texture, using the specified path
        /// as a lookup key. This method will create a new texture, and cache it.
        /// </summary>
        /// <param name="imageData">A bitmap containing the image data.</param>
        /// <param name="texturePath">
        /// The path to the texture in its corresponding package group. This is used as a lookup key.
        /// </param>
        /// <returns>A new cached texture created from the data.</returns>
        public Texture2D CreateCachedTexture(Bitmap imageData, string texturePath)
        {
            ThrowIfDisposed();

            var texture = new Texture2D(this.GL, imageData);

            _textureCache.Add(texturePath.ConvertPathSeparatorsToCurrentNativeSeparator().ToUpperInvariant(), texture);
            return texture;
        }

        /// <summary>
        /// Creates a cached shader for the specifed shader, using the specified shader enumeration
        /// as a lookup key.
        /// </summary>
        private ShaderProgram CreateCachedShader(EverlookShader shader)
        {
            if (!Enum.IsDefined(typeof(EverlookShader), shader))
            {
                throw new ArgumentException("An unknown shader was passed to the rendering cache.", nameof(shader));
            }

            Log.Info($"Creating cached shader for \"{shader}\"");

            ShaderProgram shaderProgram;
            switch (shader)
            {
                case EverlookShader.Plain2D:
                {
                    shaderProgram = new Plain2DShader(this.GL);
                    break;
                }
                case EverlookShader.WorldModel:
                {
                    shaderProgram = new WorldModelShader(this.GL);
                    break;
                }
                case EverlookShader.BoundingBox:
                {
                    shaderProgram = new BoundingBoxShader(this.GL);
                    break;
                }
                case EverlookShader.GameModel:
                {
                    shaderProgram = new GameModelShader(this.GL);
                    break;
                }
                case EverlookShader.BaseGrid:
                {
                    shaderProgram = new BaseGridShader(this.GL);
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException
                    (
                        nameof(shader),
                        "No implemented shader class for this shader."
                    );
                }
            }

            _shaderCache.Add(shader, shaderProgram);
            return shaderProgram;
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the object has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the object is disposed.</exception>
        private void ThrowIfDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(ToString() ?? nameof(RenderCache));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.IsDisposed = true;

            foreach (var cachedTexture in _textureCache)
            {
                cachedTexture.Value?.Dispose();
            }
            _textureCache.Clear();

            foreach (var cachedShader in _shaderCache)
            {
                cachedShader.Value?.Dispose();
            }
            _shaderCache.Clear();
        }
    }
}
