using System;

namespace BusinessLogic.DTOs
{
    public class ImageUploadTask
    {
        public int PostId { get; set; }
        public byte[] FileData { get; set; }
        public string FileName { get; set; }
        public string Folder { get; set; }
    }
}
