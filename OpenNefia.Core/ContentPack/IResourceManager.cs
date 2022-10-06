﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using OpenNefia.Core.Utility;
using YamlDotNet.RepresentationModel;

namespace OpenNefia.Core.ContentPack
{
    /// <summary>
    ///     Virtual file system for all disk resources.
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        ///     Provides access to the writable user data folder.
        /// </summary>
        IWritableDirProvider UserData { get; }

        /// <summary>
        ///     Read a file from the mounted content roots.
        /// </summary>
        /// <param name="path">The path to the file in the VFS. Must be rooted.</param>
        /// <returns>The memory stream of the file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="path"/> does not exist in the VFS.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        /// <seealso cref="ResourceManagerExt.ContentFileReadOrNull"/>
        Stream ContentFileRead(ResourcePath path, ContentRootID? rootID = null);

        /// <summary>
        ///     Read a file from the mounted content roots.
        /// </summary>
        /// <param name="path">The path to the file in the VFS. Must be rooted.</param>
        /// <returns>The memory stream of the file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="path"/> does not exist in the VFS.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        Stream ContentFileRead(string path, ContentRootID? rootID = null);

        /// <summary>
        ///     Check if a file exists in any of the mounted content roots.
        /// </summary>
        /// <param name="path">The path of the file to check.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        bool ContentFileExists(ResourcePath path, ContentRootID? rootID = null);

        /// <summary>
        ///     Check if a file exists in any of the mounted content roots.
        /// </summary>
        /// <param name="path">The path of the file to check.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        bool ContentFileExists(string path, ContentRootID? rootID = null);

        /// <summary>
        ///     Try to read a file from the mounted content roots.
        /// </summary>
        /// <param name="path">The path of the file to try to read.</param>
        /// <param name="fileStream">The memory stream of the file's contents. Null if the file could not be loaded.</param>
        /// <returns>True if the file could be loaded, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        /// <seealso cref="ResourceManagerExt.ContentFileReadOrNull"/>
        bool TryContentFileRead(ResourcePath path, [NotNullWhen(true)] out Stream? fileStream, ContentRootID? rootID = null);

        /// <summary>
        ///     Try to read a file from the mounted content roots.
        /// </summary>
        /// <param name="path">The path of the file to try to read.</param>
        /// <param name="fileStream">The memory stream of the file's contents. Null if the file could not be loaded.</param>
        /// <returns>True if the file could be loaded, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        bool TryContentFileRead(string path, [NotNullWhen(true)] out Stream? fileStream, ContentRootID? rootID = null);

        /// <summary>
        ///     Recursively finds all files in a directory and all sub directories.
        /// </summary>
        /// <remarks>
        ///     If the directory does not exist, an empty enumerable is returned.
        /// </remarks>
        /// <param name="path">Directory to search inside of.</param>
        /// <returns>Enumeration of all absolute file paths of the files found.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        IEnumerable<ResourcePath> ContentFindFiles(ResourcePath path, ContentRootID? rootID = null);

        IEnumerable<ResourcePath> ContentFindRelativeFiles(ResourcePath path, ContentRootID? rootID = null)
        {
            foreach (var absPath in ContentFindFiles(path, rootID))
            {
                if (!absPath.TryRelativeTo(path, out var rel))
                {
                    DebugTools.Assert("Past must be relative to be returned, unreachable.");
                    throw new InvalidOperationException("This is unreachable");
                }

                yield return rel;
            }
        }

        /// <summary>
        ///     Recursively finds all files in a directory and all sub directories.
        /// </summary>
        /// <remarks>
        ///     If the directory does not exist, an empty enumerable is returned.
        /// </remarks>
        /// <param name="path">Directory to search inside of.</param>
        /// <returns>Enumeration of all absolute file paths of the files found.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        IEnumerable<ResourcePath> ContentFindFiles(string path, ContentRootID? rootID = null);

        /// <summary>
        ///     Returns a list of paths to all top-level content directories
        /// </summary>
        /// <returns></returns>
        IEnumerable<ResourcePath> GetContentRoots();

        /// <summary>
        ///     Returns a list of paths to all top-level content directories
        /// </summary>
        /// <returns></returns>
        IEnumerable<(ContentRootID, ResourcePath)> GetContentRootsAndIDs();

        /// <summary>
        ///     Read a file from the mounted content paths to a string.
        /// </summary>
        /// <param name="path">Path of the file to read.</param>
        string ContentFileReadAllText(string path, ContentRootID? rootID = null)
        {
            return ContentFileReadAllText(new ResourcePath(path), rootID);
        }

        /// <summary>
        ///     Read a file from the mounted content paths to a string.
        /// </summary>
        /// <param name="path">Path of the file to read.</param>
        string ContentFileReadAllText(ResourcePath path, ContentRootID? rootID = null)
        {
            return ContentFileReadAllText(path, EncodingHelpers.UTF8, rootID);
        }

        /// <summary>
        ///     Read a file from the mounted content paths to a string.
        /// </summary>
        /// <param name="path">Path of the file to read.</param>
        /// <param name="encoding">Text encoding to use when reading.</param>
        string ContentFileReadAllText(ResourcePath path, Encoding encoding, ContentRootID? rootID = null)
        {
            using var stream = ContentFileRead(path, rootID);
            using var reader = new StreamReader(stream, encoding);

            return reader.ReadToEnd();
        }

        public YamlStream ContentFileReadYaml(ResourcePath path, ContentRootID? rootID = null)
        {
            using var reader = ContentFileReadText(path, rootID);

            var yamlStream = new YamlStream();
            yamlStream.Load(reader);

            return yamlStream;
        }

        public StreamReader ContentFileReadText(ResourcePath path, ContentRootID? rootID = null)
        {
            return ContentFileReadText(path, EncodingHelpers.UTF8, rootID);
        }

        public StreamReader ContentFileReadText(ResourcePath path, Encoding encoding, ContentRootID? rootID = null)
        {
            var stream = ContentFileRead(path, rootID);
            return new StreamReader(stream, encoding);
        }
    }

    public struct ContentRootID
    {
        internal ContentRootID(int id)
        {
            ID = id;
        }

        private int ID { get; }
    }
}
