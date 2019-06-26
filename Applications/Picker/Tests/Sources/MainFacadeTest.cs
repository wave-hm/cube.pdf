﻿/* ------------------------------------------------------------------------- */
//
// Copyright (c) 2010 CubeSoft, Inc.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
/* ------------------------------------------------------------------------- */
using Cube.Tests;
using NUnit.Framework;

namespace Cube.Pdf.Picker.Tests
{
    /* --------------------------------------------------------------------- */
    ///
    /// MainFacadeTest
    ///
    /// <summary>
    /// Tests the MainFacade class.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    [TestFixture]
    public class MainFacadeTest : FileFixture
    {
        #region Tests

        /* ----------------------------------------------------------------- */
        ///
        /// Create
        ///
        /// <summary>
        /// Tests the constructor.
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [TestCase("Sample.pdf", ExpectedResult = 0)]
        public int Create(string filename)
        {
            var src = new ImageCollection(GetSource(filename));
            return src.Count;
        }

        #endregion
    }
}
