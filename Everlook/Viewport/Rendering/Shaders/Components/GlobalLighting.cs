//
//  GlobalLighting.cs
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

using System.Numerics;
using Everlook.Viewport.Rendering.Core;
using Silk.NET.OpenGL;

namespace Everlook.Viewport.Rendering.Shaders.Components
{
    /// <summary>
    /// A global light shader component.
    /// </summary>
    public class GlobalLighting : GraphicsObject
    {
        private const string LightVectorIdentifier = "LightVector";
        private const string LightColourIdentifier = "LightColour";
        private const string LightIntensityIdentifier = "LightIntensity";

        private readonly uint _parentShaderNativeID;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalLighting"/> class, and attaches it to the given parent
        /// shader.
        /// </summary>
        /// <param name="gl">The OpenGL API.</param>
        /// <param name="parentShaderID">The native ID of the parent shader.</param>
        public GlobalLighting(GL gl, uint parentShaderID)
            : base(gl)
        {
            _parentShaderNativeID = parentShaderID;
        }

        private void EnableParent()
        {
            this.GL.UseProgram(_parentShaderNativeID);
        }

        /// <summary>
        /// Sets the colour of the global lighting shader component.
        /// </summary>
        /// <param name="lightColour">The colour of the light.</param>
        public void SetLightColour(Vector4 lightColour)
        {
            EnableParent();

            var colourLoc = this.GL.GetUniformLocation(_parentShaderNativeID, LightColourIdentifier);
            this.GL.Uniform4(colourLoc, lightColour);
        }

        /// <summary>
        /// Sets the light direction of the global lighting shader component.
        /// </summary>
        /// <param name="lightVector">The vector along which light shines.</param>
        public void SetLightDirection(Vector3 lightVector)
        {
            EnableParent();

            var vectorLoc = this.GL.GetUniformLocation(_parentShaderNativeID, LightVectorIdentifier);
            this.GL.Uniform3(vectorLoc, lightVector);
        }

        /// <summary>
        /// Sets the light intensity, in lux, of the global lighting shader component.
        /// </summary>
        /// <param name="lightIntensity">The intensity in lux.</param>
        public void SetLightIntensity(float lightIntensity)
        {
            EnableParent();

            var intensityLoc = this.GL.GetUniformLocation(_parentShaderNativeID, LightIntensityIdentifier);
            this.GL.Uniform1(intensityLoc, lightIntensity);
        }
    }
}
