namespace Jovian.SaveSystem {
    /// <summary>
    /// Converts typed data to and from byte arrays.
    /// Implementations define the format (JSON, binary, etc.).
    /// </summary>
    public interface ISaveSerializer {
        byte[] Serialize<TData>(TData data);
        TData Deserialize<TData>(byte[] payload);
    }
}
