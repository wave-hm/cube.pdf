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
using Cube.FileSystem;
using Cube.Mixin.Collections;
using Cube.Mixin.String;
using Cube.Pdf.Itext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Cube.Pdf.Editor
{
    /* --------------------------------------------------------------------- */
    ///
    /// MainFacadeExtension
    ///
    /// <summary>
    /// Represents the extended methods to handle the MainFacade object.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    internal static class MainFacadeExtension
    {
        #region Methods

        #region Open

        /* ----------------------------------------------------------------- */
        ///
        /// Setup
        ///
        /// <summary>
        /// Invokes some actions through the specified arguments.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="args">User arguments.</param>
        ///
        /* ----------------------------------------------------------------- */
        public static void Setup(this MainFacade src, IEnumerable<string> args)
        {
            foreach (var ps in src.Settings.GetSplashProcesses()) ps.Kill();
            var path = src.GetFirst(args);
            if (path.HasValue()) src.Open(path);
            src.Backup.Cleanup();
        }

        /* ----------------------------------------------------------------- */
        ///
        /// IsOpen
        ///
        /// <summary>
        /// Gets the value indicating whether a PDF document is open.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        ///
        /// <returns>true for open.</returns>
        ///
        /* ----------------------------------------------------------------- */
        public static bool IsOpen(this MainFacade src) => src.Value.Source != null;

        /* ----------------------------------------------------------------- */
        ///
        /// Open
        ///
        /// <summary>
        /// Sets properties of the specified IDocumentReader.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="doc">Document information.</param>
        ///
        /// <remarks>
        /// PDFium は Metadata や Encryption の情報取得が不完全なため、
        /// これらの情報は、必要になったタイミングで iTextSharp を用いて
        /// 取得します。
        /// </remarks>
        ///
        /* ----------------------------------------------------------------- */
        public static void Open(this MainFacade src, IDocumentReader doc)
        {
            src.Value.Source = doc.File;
            if (!doc.Encryption.Enabled) src.Value.Encryption = doc.Encryption;
            src.Value.Images.Add(doc.Pages);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Open
        ///
        /// <summary>
        /// Opens the first item of the specified collection.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="files">File collection.</param>
        ///
        /* ----------------------------------------------------------------- */
        public static void Open(this MainFacade src, IEnumerable<string> files)
        {
            var path = src.GetFirst(files);
            if (path.HasValue()) src.Open(path);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// OpenLink
        ///
        /// <summary>
        /// Opens a PDF document with the specified link.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="link">Information for the link.</param>
        ///
        /* ----------------------------------------------------------------- */
        public static void OpenLink(this MainFacade src, Entity link)
        {
            try { src.Open(Shortcut.Resolve(link?.FullName)?.Target); }
            catch (Exception err)
            {
                var cancel = err is OperationCanceledException ||
                             err is TwiceException;
                if (!cancel) src.Value.IO.TryDelete(link?.FullName);
                throw;
            }
        }

        /* ----------------------------------------------------------------- */
        ///
        /// StartProcess
        ///
        /// <summary>
        /// Starts a new process with the specified arguments.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="args">User arguments.</param>
        ///
        /* ----------------------------------------------------------------- */
        public static void StartProcess(this MainFacade src, string args) =>
            Process.Start(new ProcessStartInfo
            {
                FileName  = Assembly.GetExecutingAssembly().Location,
                Arguments = args
            });

        /* ----------------------------------------------------------------- */
        ///
        /// GetFirst
        ///
        /// <summary>
        /// Gets the first item of the specified collection
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="files">File collection.</param>
        ///
        /* ----------------------------------------------------------------- */
        public static string GetFirst(this MainFacade src, IEnumerable<string> files) =>
            files.FirstOrDefault(e => e.IsPdf());

        #endregion

        #region Close

        /* ----------------------------------------------------------------- */
        ///
        /// Close
        ///
        /// <summary>
        /// Clears properties of the current PDF document.
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public static void Close(this MainFacade src)
        {
            src.Value.Source     = null;
            src.Value.Metadata   = null;
            src.Value.Encryption = null;

            src.Value.History.Clear();
            src.Value.Images.Clear();
        }

        #endregion

        #region Save

        /* ----------------------------------------------------------------- */
        ///
        /// Overwrite
        ///
        /// <summary>
        /// Overwrites the PDF document.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        ///
        /* ----------------------------------------------------------------- */
        public static void Overwrite(this MainFacade src)
        {
            if (src.Value.History.Undoable) src.Save(src.Value.Source.FullName);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Save
        ///
        /// <summary>
        /// Saves the PDF document to the specified file path.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="dest">File path.</param>
        ///
        /* ----------------------------------------------------------------- */
        public static void Save(this MainFacade src, string dest) => src.Save(dest, true);

        /* ----------------------------------------------------------------- */
        ///
        /// Save
        ///
        /// <summary>
        /// Save the PDF document
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="dest">Saving file information.</param>
        /// <param name="close">Close action.</param>
        ///
        /* ----------------------------------------------------------------- */
        public static void Save(this MainFacade src, Entity dest, Action close)
        {
            var data = src.Value;
            var tmp  = data.IO.Combine(dest.DirectoryName, Guid.NewGuid().ToString("D"));

            try
            {
                var reader = data.Source.GetItexReader(data.Query, data.IO);
                data.Set(reader.Metadata, reader.Encryption);

                using (var writer = new DocumentWriter())
                {
                    writer.Add(reader.Attachments);
                    writer.Add(data.Images.Select(e => e.RawObject), reader);
                    writer.Set(data.Metadata);
                    writer.Set(data.Encryption);
                    writer.Save(tmp);
                }

                close();
                src.Backup.Invoke(dest);
                data.IO.Copy(tmp, dest.FullName, true);
            }
            finally { data.IO.TryDelete(tmp); }
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Restruct
        ///
        /// <summary>
        /// Restructs some properties with the specified new PDF document.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="doc">New PDF document.</param>
        ///
        /* ----------------------------------------------------------------- */
        public static void Restruct(this MainFacade src, IDocumentReader doc)
        {
            var items = doc.Pages.Select((v, i) => new { Value = v, Index = i });
            foreach (var e in items) src.Value.Images[e.Index].RawObject = e.Value;
            src.Value.Source = doc.File;
            src.Value.History.Clear();
        }

        #endregion

        #region InsertOrMove

        /* ----------------------------------------------------------------- */
        ///
        /// IsInsertable
        ///
        /// <summary>
        /// Gets the value indicating whether the specified file is
        /// insertable.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="path">File path.</param>
        ///
        /// <remarks>
        /// TODO: 現在は拡張子で判断しているが、ファイル内容の Signature を
        /// 用いて判断するように修正する。
        /// </remarks>
        ///
        /* ----------------------------------------------------------------- */
        public static bool IsInsertable(this MainFacade src, string path)
        {
            var ext = src.Value.IO.Get(path).Extension.ToLowerInvariant();
            var cmp = new List<string> { ".pdf", ".png", ".jpg", ".jpeg", ".bmp" };
            return cmp.Contains(ext);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Insert
        ///
        /// <summary>
        /// Inserts the specified file behind the selected index.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="files">Collection of inserting files.</param>
        ///
        /* ----------------------------------------------------------------- */
        public static void Insert(this MainFacade src, IEnumerable<string> files) =>
            src.Insert(src.Value.Images.Selection.Last + 1, files);

        /* ----------------------------------------------------------------- */
        ///
        /// InsertOrMove
        ///
        /// <summary>
        /// Inserts or moves the specified pages according to the specified
        /// condition.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="obj">Drag&amp;Drop result.</param>
        ///
        /* ----------------------------------------------------------------- */
        public static void InsertOrMove(this MainFacade src, DragDropObject obj)
        {
            if (!obj.IsCurrentProcess)
            {
                var index = Math.Min(obj.DropIndex + 1, src.Value.Count);
                src.Insert(index, obj.Pages);
            }
            else if (obj.DragIndex < obj.DropIndex) src.MoveNext(obj);
            else src.MovePrevious(obj);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// MovePrevious
        ///
        /// <summary>
        /// Moves selected items according to the specified condition.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="obj">Drag&amp;Drop result.</param>
        ///
        /* ----------------------------------------------------------------- */
        private static void MovePrevious(this MainFacade src, DragDropObject obj)
        {
            var delta = obj.DropIndex - obj.DragIndex;
            var n     = src.Value.Images.Selection.Indices
                           .Where(i => i < obj.DragIndex && i >= obj.DropIndex).Count();
            src.Move(delta + n);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// MoveNext
        ///
        /// <summary>
        /// Moves selected items according to the specified condition.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="obj">Drag&amp;Drop result.</param>
        ///
        /* ----------------------------------------------------------------- */
        private static void MoveNext(this MainFacade src, DragDropObject obj)
        {
            var delta = obj.DropIndex - obj.DragIndex;
            var n     = src.Value.Images.Selection.Indices
                           .Where(i => i > obj.DragIndex && i <= obj.DropIndex).Count();
            src.Move(delta - n);
        }

        #endregion

        #region Metadata

        /* ----------------------------------------------------------------- */
        ///
        /// SetMetadata
        ///
        /// <summary>
        /// Sets the Metadata object.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="value">Metadata object.</param>
        ///
        /// <returns>
        /// History item to execute undo and redo actions.
        /// </returns>
        ///
        /* ----------------------------------------------------------------- */
        public static HistoryItem SetMetadata(this MainFacade src, Metadata value)
        {
            var prev = src.Value.Metadata;
            return HistoryItem.CreateInvoke(
                () => src.Value.Metadata = value,
                () => src.Value.Metadata = prev
            );
        }

        /* ----------------------------------------------------------------- */
        ///
        /// SetEncryption
        ///
        /// <summary>
        /// Sets the Encryption object.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        /// <param name="value">Encryption object.</param>
        ///
        /// <returns>
        /// History item to execute undo and redo actions.
        /// </returns>
        ///
        /* ----------------------------------------------------------------- */
        public static HistoryItem SetEncryption(this MainFacade src, Encryption value)
        {
            var prev = src.Value.Encryption;
            return HistoryItem.CreateInvoke(
                () => src.Value.Encryption = value,
                () => src.Value.Encryption = prev
            );
        }

        #endregion

        /* ----------------------------------------------------------------- */
        ///
        /// Select
        ///
        /// <summary>
        /// Sets or resets the IsSelected property of all items according
        /// to the current condition.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        ///
        /* ----------------------------------------------------------------- */
        public static void Select(this MainFacade src) =>
            src.Select(src.Value.Images.Selection.Count < src.Value.Images.Count);

        /* ----------------------------------------------------------------- */
        ///
        /// Zoom
        ///
        /// <summary>
        /// Executes the Zoom command by using the current settings.
        /// </summary>
        ///
        /// <param name="src">Facade object.</param>
        ///
        /* ----------------------------------------------------------------- */
        public static void Zoom(this MainFacade src)
        {
            var items = src.Value.Images.Preferences.ItemSizeOptions;
            var prev  = src.Value.Images.Preferences.ItemSizeIndex;
            var next  = items.LastIndex(x => x <= src.Settings.Value.ItemSize);
            src.Zoom(next - prev);
        }

        #endregion
    }
}
