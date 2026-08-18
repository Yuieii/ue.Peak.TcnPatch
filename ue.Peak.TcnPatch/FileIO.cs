// Copyright (c) 2026 Yuieii.

using System.IO;

namespace ue.Peak.TcnPatch
{
    public class FileIO
    {
        private readonly string _path;
        
        public FileIO(string path)
        {
            _path = path;
        }
        
        public FileStream Open(FileMode mode, FileAccess access) 
            => new(_path, mode, access);

        public FileStream Open(FileMode mode, FileAccess access, FileShare share) 
            => new(_path, mode, access, share);
        
        public bool Exists => File.Exists(_path);
    }
}