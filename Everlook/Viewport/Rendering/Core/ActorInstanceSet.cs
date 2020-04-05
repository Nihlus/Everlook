﻿//
//  ActorInstanceSet.cs
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
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Everlook.Viewport.Camera;
using Everlook.Viewport.Rendering.Interfaces;
using Silk.NET.OpenGL;

namespace Everlook.Viewport.Rendering.Core
{
    /// <summary>
    /// Represents a set of actor instances, differing by their transforms.
    /// </summary>
    /// <typeparam name="T">A renderable supporting instanced rendering.</typeparam>
    public class ActorInstanceSet<T> : GraphicsObject, IRenderable, IBoundedModel
        where T : class, IInstancedRenderable, IActor, IBoundedModel
    {
        /// <inheritdoc />
        public bool ShouldRenderBounds
        {
            get => this.Target.ShouldRenderBounds;
            set => this.Target.ShouldRenderBounds = value;
        }

        /// <inheritdoc />
        public bool IsStatic => this.Target.IsStatic;

        /// <inheritdoc />
        public bool IsInitialized { get; set; }

        /// <inheritdoc />
        public ProjectionType Projection => this.Target.Projection;

        /// <summary>
        /// Gets the target renderable of the instance set.
        /// </summary>
        public T Target { get; }

        /// <summary>
        /// Gets the number of instances in the set.
        /// </summary>
        public int Count => _instanceTransforms.Count;

        private Buffer<Matrix4x4>? _instanceModelMatrices;

        private List<Transform> _instanceTransforms;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorInstanceSet{T}"/> class.
        /// </summary>
        /// <param name="gl">The OpenGL API.</param>
        /// <param name="target">The target instanced renderable.</param>
        public ActorInstanceSet(GL gl, T target)
            : base(gl)
        {
            this.Target = target;
            _instanceTransforms = new List<Transform>();

            this.IsInitialized = false;
        }

        /// <summary>
        /// Sets the transforms of the instances in the set.
        /// </summary>
        /// <param name="instanceTransforms">The transforms of the instances.</param>
        public void SetInstances(IEnumerable<Transform> instanceTransforms)
        {
            _instanceTransforms = instanceTransforms.ToList();
        }

        /// <inheritdoc />
        public void Initialize()
        {
            _instanceModelMatrices = new Buffer<Matrix4x4>
            (
                this.GL,
                BufferTargetARB.ArrayBuffer,
                BufferUsageARB.StaticDraw);

            _instanceModelMatrices.AttachAttributePointer
            (
                new VertexAttributePointer
                (
                    this.GL,
                    6,
                    4,
                    VertexAttribPointerType.Float,
                    (uint)Marshal.SizeOf<Matrix4x4>(),
                    0
                )
            );

            _instanceModelMatrices.AttachAttributePointer
            (
                new VertexAttributePointer
                (
                    this.GL,
                    7,
                    4,
                    VertexAttribPointerType.Float,
                    (uint)Marshal.SizeOf<Matrix4x4>(),
                    16
                )
            );

            _instanceModelMatrices.AttachAttributePointer
            (
                new VertexAttributePointer
                (
                    this.GL,
                    8,
                    4,
                    VertexAttribPointerType.Float,
                    (uint)Marshal.SizeOf<Matrix4x4>(),
                    32
                )
            );

            _instanceModelMatrices.AttachAttributePointer
            (
                new VertexAttributePointer
                (
                    this.GL,
                    9,
                    4,
                    VertexAttribPointerType.Float,
                    (uint)Marshal.SizeOf<Matrix4x4>(),
                    48
                )
            );

            _instanceModelMatrices.Bind();
            _instanceModelMatrices.EnableAttributes();

            this.GL.VertexAttribDivisor(6, 1);
            this.GL.VertexAttribDivisor(7, 1);
            this.GL.VertexAttribDivisor(8, 1);
            this.GL.VertexAttribDivisor(9, 1);

            _instanceModelMatrices.Data = _instanceTransforms.Select(t => t.GetModelMatrix()).ToArray();

            this.IsInitialized = true;
        }

        /// <inheritdoc />
        public void Render(Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, ViewportCamera camera)
        {
            if (!this.IsInitialized || _instanceModelMatrices is null)
            {
                return;
            }

            _instanceModelMatrices.Bind();
            _instanceModelMatrices.EnableAttributes();

            this.Target.RenderInstances(viewMatrix, projectionMatrix, camera, this.Count);

            _instanceModelMatrices.DisableAttributes();
        }

        /// <summary>
        /// This method does nothing, and should not be called. The source actor should be disposed instead.
        /// </summary>
        public void Dispose()
        {
            throw new NotSupportedException
            (
                "A renderable instance set should not be disposed. Dispose the source actor instead."
            );
        }
    }
}
