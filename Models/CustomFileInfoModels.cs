using System;
using System.Collections.Generic;
using System.Text;

namespace GenerateCertificate.Models
{
    public class CustomFileInfoModels
    {
        public string PSPath { get; set; }
        public string PSParentPath { get; set; }
        public string PSChildName { get; set; }
        public bool PSIsContainer { get; set; }
        public string Mode { get; set; }
        public string VersionInfo { get; set; }
        public string BaseName { get; set; }
        public object LinkType { get; set; }
        public string PSComputerName { get; set; }
        public string RunspaceId { get; set; }
        public bool PSShowComputerName { get; set; }
        public string Name { get; set; }
        public int Length { get; set; }
        public string DirectoryName { get; set; }
        public string Directory { get; set; }
        public bool IsReadOnly { get; set; }
        public bool Exists { get; set; }
        public string FullName { get; set; }
        public string Extension { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime CreationTimeUtc { get; set; }
        public DateTime LastAccessTime { get; set; }
        public DateTime LastAccessTimeUtc { get; set; }
        public DateTime LastWriteTime { get; set; }
        public DateTime LastWriteTimeUtc { get; set; }
        public string Attributes { get; set; }
    }

}
