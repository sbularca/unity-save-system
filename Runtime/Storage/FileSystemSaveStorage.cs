using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Jovian.SaveSystem {
    /// <summary>
    /// File-system backed storage. Base path is typically Application.persistentDataPath.
    /// All paths passed to methods are relative to the constructed base path.
    /// </summary>
    public sealed class FileSystemSaveStorage : ISaveStorage {
        private readonly string basePath;

        public FileSystemSaveStorage(string rootPath, string saveDirectoryName) {
            basePath = Path.Combine(rootPath, saveDirectoryName);
        }

        private string ResolvePath(string relativePath) {
            return Path.Combine(basePath, relativePath);
        }

        public void CreateDirectory(string path) {
            string fullPath = ResolvePath(path);
            if(!Directory.Exists(fullPath)) {
                Directory.CreateDirectory(fullPath);
            }
        }

        public void Write(string path, byte[] data) {
            string fullPath = ResolvePath(path);
            EnsureDirectory(fullPath);
            File.WriteAllBytes(fullPath, data);
        }

        public byte[] Read(string path) {
            string fullPath = ResolvePath(path);
            if(!File.Exists(fullPath)) {
                throw new FileNotFoundException($"Save file not found: {fullPath}");
            }
            return File.ReadAllBytes(fullPath);
        }

        public bool Exists(string path) {
            return File.Exists(ResolvePath(path));
        }

        public void Delete(string path) {
            string fullPath = ResolvePath(path);
            if(File.Exists(fullPath)) {
                File.Delete(fullPath);
            }
        }

        public string[] List(string directoryPath) {
            string fullPath = ResolvePath(directoryPath);
            if(!Directory.Exists(fullPath)) {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(fullPath)
                .Select(f => Path.GetFileName(f))
                .ToArray();
        }

        public async Task WriteAsync(string path, byte[] data) {
            string fullPath = ResolvePath(path);
            EnsureDirectory(fullPath);
            using(FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true)) {
                await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            }
        }

        public async Task<byte[]> ReadAsync(string path) {
            string fullPath = ResolvePath(path);
            if(!File.Exists(fullPath)) {
                throw new FileNotFoundException($"Save file not found: {fullPath}");
            }

            using(FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true)) {
                byte[] buffer = new byte[stream.Length];
                await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                return buffer;
            }
        }

        public Task<bool> ExistsAsync(string path) {
            return Task.FromResult(Exists(path));
        }

        public Task DeleteAsync(string path) {
            Delete(path);
            return Task.CompletedTask;
        }

        public Task<string[]> ListAsync(string directoryPath) {
            return Task.FromResult(List(directoryPath));
        }

        private static void EnsureDirectory(string filePath) {
            string directory = Path.GetDirectoryName(filePath);
            if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
