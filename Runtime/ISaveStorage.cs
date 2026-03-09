using System.Threading.Tasks;

namespace Jovian.SaveSystem {
    /// <summary>
    /// Reads and writes raw byte arrays to a persistent location.
    /// Has no knowledge of save data types or serialization formats.
    /// </summary>
    public interface ISaveStorage {
        void Write(string path, byte[] data);
        byte[] Read(string path);
        bool Exists(string path);
        void Delete(string path);
        string[] List(string directoryPath);
        void CreateDirectory(string path);

        Task WriteAsync(string path, byte[] data);
        Task<byte[]> ReadAsync(string path);
        Task<bool> ExistsAsync(string path);
        Task DeleteAsync(string path);
        Task<string[]> ListAsync(string directoryPath);
    }
}
