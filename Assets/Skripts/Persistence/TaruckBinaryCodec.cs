using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public enum TaruckFileType : byte
{
    LocalData = 1,
    FullSaveExport = 2,
    CharacterExport = 3,
    ItemExport = 4
}

public sealed class UnsupportedTaruckVersionException : IOException
{
    public UnsupportedTaruckVersionException(string message) : base(message) { }
}

public static class TaruckBinaryCodec
{
    private const int HeaderSize = 29;
    private const int MaxFile = 128 * 1024 * 1024;
    private const int MaxPayload = 112 * 1024 * 1024;
    private const int MaxString = 4 * 1024 * 1024;
    private const int MaxCollection = 100000;
    private const int MaxMedia = 16 * 1024 * 1024;
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("TARUCKPK");
    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

    private enum Kind : byte { Text = 1, Int = 2, UInt = 3, Float = 4, Bool = 5, Object = 6, List = 7 }

    private sealed class Field
    {
        public uint Id;
        public Kind Type;
        public byte[] Data;
    }

    private sealed class ObjectReader
    {
        private readonly Dictionary<uint, Field> fields = new Dictionary<uint, Field>();
        public ObjectReader(byte[] source, int depth)
        {
            if (depth > 8) throw new InvalidDataException("Object nesting limit exceeded");
            using (var stream = new MemoryStream(source, false))
            using (var reader = new BinaryReader(stream))
                while (stream.Position < stream.Length)
                {
                    if (stream.Length - stream.Position < 9) throw new InvalidDataException("Truncated field");
                    uint id = reader.ReadUInt32();
                    Kind type = (Kind)reader.ReadByte();
                    uint length = reader.ReadUInt32();
                    if (length > MaxPayload || length > stream.Length - stream.Position)
                        throw new InvalidDataException("Invalid field length");
                    if (fields.ContainsKey(id)) throw new InvalidDataException("Duplicate field " + id);
                    if (fields.Count >= 1000000) throw new InvalidDataException("Field count limit exceeded");
                    fields.Add(id, new Field { Id = id, Type = type, Data = reader.ReadBytes((int)length) });
                }
        }
        public byte[] Get(uint id, Kind type, bool optional = false)
        {
            if (!fields.TryGetValue(id, out Field field))
            {
                if (optional) return null;
                throw new InvalidDataException("Missing field " + id);
            }
            if (field.Type != type) throw new InvalidDataException("Wrong type for field " + id);
            return field.Data;
        }
        public string Text(uint id, bool optional = false)
        {
            byte[] data = Get(id, Kind.Text, optional);
            if (data == null) return "";
            if (data.Length > MaxString) throw new InvalidDataException("String limit exceeded");
            return Utf8.GetString(data);
        }
        public int Int(uint id) => ReadFixed(Get(id, Kind.Int), 4, r => r.ReadInt32());
        public uint UInt(uint id, bool optional = false)
        {
            byte[] bytes = Get(id, Kind.UInt, optional);
            return bytes == null ? 0 : ReadFixed(bytes, 4, r => r.ReadUInt32());
        }
        public float Float(uint id) => ReadFinite(Get(id, Kind.Float));
        public byte[] Object(uint id) => Get(id, Kind.Object);
        public List<T> List<T>(uint id, Func<byte[], T> decode, int limit = MaxCollection)
        {
            byte[] data = Get(id, Kind.List);
            using (var stream = new MemoryStream(data, false))
            using (var reader = new BinaryReader(stream))
            {
                if (stream.Length < 4) throw new InvalidDataException("Truncated collection");
                uint count = reader.ReadUInt32();
                if (count > limit || count > (stream.Length - 4) / 4)
                    throw new InvalidDataException("Collection limit exceeded");
                var result = new List<T>((int)count);
                for (uint i = 0; i < count; i++)
                {
                    if (stream.Length - stream.Position < 4) throw new InvalidDataException("Truncated element");
                    uint length = reader.ReadUInt32();
                    if (length > MaxPayload || length > stream.Length - stream.Position)
                        throw new InvalidDataException("Invalid element length");
                    result.Add(decode(reader.ReadBytes((int)length)));
                }
                if (stream.Position != stream.Length) throw new InvalidDataException("Trailing collection bytes");
                return result;
            }
        }
    }

    private static T ReadFixed<T>(byte[] bytes, int size, Func<BinaryReader, T> read)
    {
        if (bytes.Length != size) throw new InvalidDataException("Invalid scalar length");
        using (var stream = new MemoryStream(bytes, false))
        using (var reader = new BinaryReader(stream)) return read(reader);
    }
    private static float ReadFinite(byte[] bytes)
    {
        float value = ReadFixed(bytes, 4, r => r.ReadSingle());
        if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidDataException("Non-finite float");
        return value;
    }
    private static bool ReadBool(byte[] bytes)
    {
        if (bytes.Length != 1 || bytes[0] > 1) throw new InvalidDataException("Invalid boolean");
        return bytes[0] == 1;
    }
    private static string ReadText(byte[] bytes)
    {
        if (bytes.Length > MaxString) throw new InvalidDataException("String limit exceeded");
        return Utf8.GetString(bytes);
    }
    private static byte[] Scalar(Action<BinaryWriter> write)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream)) { write(writer); return stream.ToArray(); }
    }
    private static byte[] TextBytes(string value)
    {
        byte[] data = Utf8.GetBytes(value ?? "");
        if (data.Length > MaxString) throw new InvalidDataException("String limit exceeded");
        return data;
    }
    private static byte[] FloatBytes(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidDataException("Non-finite float");
        return Scalar(w => w.Write(value));
    }
    private static void Put(BinaryWriter writer, uint id, Kind kind, byte[] bytes)
    {
        if (bytes.Length > MaxPayload) throw new InvalidDataException("Field limit exceeded");
        writer.Write(id); writer.Write((byte)kind); writer.Write((uint)bytes.Length); writer.Write(bytes);
    }
    private static void Text(BinaryWriter w, uint id, string value) => Put(w, id, Kind.Text, TextBytes(value));
    private static void Int(BinaryWriter w, uint id, int value) => Put(w, id, Kind.Int, Scalar(x => x.Write(value)));
    private static void UInt(BinaryWriter w, uint id, uint value) => Put(w, id, Kind.UInt, Scalar(x => x.Write(value)));
    private static void Float(BinaryWriter w, uint id, float value) => Put(w, id, Kind.Float, FloatBytes(value));
    private static void Obj(BinaryWriter w, uint id, byte[] value) => Put(w, id, Kind.Object, value);
    private static byte[] Build(Action<BinaryWriter> write) => Scalar(write);
    private static void List<T>(BinaryWriter w, uint id, IList<T> values, Func<T, byte[]> encode, int limit = MaxCollection)
    {
        if (values == null) values = new List<T>();
        if (values.Count > limit) throw new InvalidDataException("Collection limit exceeded");
        Put(w, id, Kind.List, Build(x =>
        {
            x.Write((uint)values.Count);
            foreach (T value in values)
            {
                if (value == null) throw new InvalidDataException("Null collection element");
                byte[] bytes = encode(value);
                x.Write((uint)bytes.Length); x.Write(bytes);
            }
        }));
    }

    private static byte[] EncodeStringEntry(StringSaveEntry e) => Build(w => { Text(w, 1, e.key); Text(w, 2, e.value); });
    private static StringSaveEntry DecodeStringEntry(byte[] b, int depth)
    {
        var r = new ObjectReader(b, depth);
        return new StringSaveEntry { key = r.Text(1), value = r.Text(2) };
    }
    private static byte[] EncodeIntEntry(IntSaveEntry e) => Build(w => { Text(w, 1, e.key); Int(w, 2, e.value); });
    private static IntSaveEntry DecodeIntEntry(byte[] b, int depth)
    {
        var r = new ObjectReader(b, depth);
        return new IntSaveEntry { key = r.Text(1), value = r.Int(2) };
    }
    private static byte[] EncodeFloatEntry(FloatSaveEntry e) => Build(w => { Text(w, 1, e.key); Float(w, 2, e.value); });
    private static FloatSaveEntry DecodeFloatEntry(byte[] b, int depth)
    {
        var r = new ObjectReader(b, depth);
        return new FloatSaveEntry { key = r.Text(1), value = r.Float(2) };
    }
    private static byte[] EncodeScene(CharacterSceneData s) => Build(w =>
    {
        Text(w, 1, s.sceneName);
        List(w, 2, s.inputData, TextBytes);
        List(w, 3, s.toggleData, b => new[] { (byte)(b ? 1 : 0) });
        List(w, 4, s.sliderData, FloatBytes);
        List(w, 5, s.dropdownData, i => Scalar(x => x.Write(i)));
        List(w, 6, s.stringData, EncodeStringEntry);
        List(w, 7, s.intData, EncodeIntEntry);
        List(w, 8, s.floatData, EncodeFloatEntry);
        Int(w, 9, s.stableFieldVersion);
    });
    private static CharacterSceneData DecodeScene(byte[] b, int depth)
    {
        var r = new ObjectReader(b, depth);
        var s = new CharacterSceneData(r.Text(1));
        s.inputData = r.List(2, ReadText);
        s.toggleData = r.List(3, ReadBool);
        s.sliderData = r.List(4, ReadFinite);
        s.dropdownData = r.List(5, x => ReadFixed(x, 4, y => y.ReadInt32()));
        s.stringData = r.List(6, x => DecodeStringEntry(x, depth + 1));
        s.intData = r.List(7, x => DecodeIntEntry(x, depth + 1));
        s.floatData = r.List(8, x => DecodeFloatEntry(x, depth + 1));
        s.stableFieldVersion = r.Int(9);
        return s;
    }
    private static byte[] EncodeCharacter(CharacterData c) => Build(w =>
    {
        Text(w, 1, c.id); Text(w, 2, c.characterName);
        Int(w, 3, c.maxHealth); Int(w, 4, c.currentHealth);
        List(w, 5, c.inputData, TextBytes);
        List(w, 6, c.toggleData, b => new[] { (byte)(b ? 1 : 0) });
        List(w, 7, c.sliderData, FloatBytes);
        List(w, 8, c.dropdownData, i => Scalar(x => x.Write(i)));
        List(w, 9, c.sceneStates, EncodeScene, 128);
        List(w, 10, c.sharedStringData, EncodeStringEntry);
    });
    private static CharacterData DecodeCharacter(byte[] b, int depth)
    {
        var r = new ObjectReader(b, depth);
        var c = new CharacterData(r.Text(1)) { characterName = r.Text(2), maxHealth = r.Int(3), currentHealth = r.Int(4) };
        c.inputData = r.List(5, ReadText);
        c.toggleData = r.List(6, ReadBool);
        c.sliderData = r.List(7, ReadFinite);
        c.dropdownData = r.List(8, x => ReadFixed(x, 4, y => y.ReadInt32()));
        c.sceneStates = r.List(9, x => DecodeScene(x, depth + 1), 128);
        c.sharedStringData = r.List(10, x => DecodeStringEntry(x, depth + 1));
        return c;
    }
    private static byte[] EncodeApp(AppSaveData a) => Build(w =>
    {
        Text(w, 1, a.lastActiveCharacterId);
        List(w, 2, a.characters, EncodeCharacter, 1000);
    });
    private static AppSaveData DecodeApp(byte[] b)
    {
        var r = new ObjectReader(b, 1);
        return new AppSaveData { lastActiveCharacterId = r.Text(1), characters = r.List(2, x => DecodeCharacter(x, 2), 1000) };
    }

    private static byte[] EncodeItemBody(InventoryItemExportData i, bool image) => Build(w =>
    {
        Text(w, 1, i.itemName); Text(w, 2, i.itemDescription);
        Int(w, 3, i.category); Int(w, 4, i.weaponIndex); Int(w, 5, i.armorIndex);
        Int(w, 6, i.bagsIndex); Int(w, 7, i.magicIndex); Int(w, 8, i.otherIndex); Int(w, 9, i.chegerIndex);
        if (image) UInt(w, 10, 1);
    });
    private static InventoryItemExportData DecodeItemBody(byte[] b, out uint imageId)
    {
        var r = new ObjectReader(b, 1);
        imageId = r.UInt(10, true);
        return new InventoryItemExportData
        {
            itemName = r.Text(1), itemDescription = r.Text(2), category = r.Int(3),
            weaponIndex = r.Int(4), armorIndex = r.Int(5), bagsIndex = r.Int(6),
            magicIndex = r.Int(7), otherIndex = r.Int(8), chegerIndex = r.Int(9)
        };
    }

    private static byte[] Envelope(TaruckFileType type, byte[] body, string appVersion, byte[] media = null)
    {
        if (string.IsNullOrEmpty(appVersion)) throw new InvalidDataException("App version required");
        byte[] payload = Build(w =>
        {
            Int(w, 1, 1); Text(w, 2, appVersion); Text(w, 3, "DND5E"); Obj(w, 10, body);
        });
        if (payload.Length > MaxPayload) throw new InvalidDataException("Payload limit exceeded");
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(Magic); writer.Write((ushort)1); writer.Write((byte)type); writer.Write((ushort)0);
            writer.Write((ulong)payload.Length); writer.Write(Crc32(payload)); writer.Write((uint)(media == null ? 0 : 1));
            writer.Write(payload);
            if (media != null)
            {
                writer.Write((uint)1);
                string mime = IsPng(media) ? "image/png" : "image/jpeg";
                string name = IsPng(media) ? "item-image.png" : "item-image.jpg";
                byte[] n = TextBytes(name), m = TextBytes(mime);
                writer.Write((ushort)n.Length); writer.Write(n); writer.Write((ushort)m.Length); writer.Write(m);
                writer.Write((uint)media.Length); writer.Write(Crc32(media)); writer.Write(media);
            }
            if (stream.Length > MaxFile) throw new InvalidDataException("File limit exceeded");
            return stream.ToArray();
        }
    }
    public static byte[] EncodeLocal(AppSaveData data, string appVersion)
    {
        TaruckDataValidator.Validate(data);
        return Envelope(TaruckFileType.LocalData, EncodeApp(data), appVersion);
    }
    public static byte[] EncodeFullSave(AppSaveData data, string appVersion)
    {
        TaruckDataValidator.Validate(data);
        return Envelope(TaruckFileType.FullSaveExport, EncodeApp(data), appVersion);
    }
    public static byte[] EncodeCharacterExport(CharacterData data, string appVersion)
    {
        TaruckDataValidator.Validate(data);
        return Envelope(TaruckFileType.CharacterExport, EncodeCharacter(data), appVersion);
    }
    public static byte[] EncodeItemExport(InventoryItemExportData data, string appVersion)
    {
        byte[] media = string.IsNullOrEmpty(data.customImageBase64) ? null : Convert.FromBase64String(data.customImageBase64);
        if (media != null && (media.Length > MaxMedia || (!IsPng(media) && !IsJpeg(media))))
            throw new InvalidDataException("Invalid item image");
        return Envelope(TaruckFileType.ItemExport, EncodeItemBody(data, media != null), appVersion, media);
    }
    private static bool IsPng(byte[] b) => b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 && b[4] == 13 && b[5] == 10 && b[6] == 26 && b[7] == 10;
    private static bool IsJpeg(byte[] b) => b.Length >= 4 && b[0] == 0xFF && b[1] == 0xD8 && b[b.Length - 2] == 0xFF && b[b.Length - 1] == 0xD9;

    private static byte[] ReadEnvelope(byte[] file, TaruckFileType expected, out byte[] media)
    {
        if (file == null || file.Length < HeaderSize || file.Length > MaxFile) throw new InvalidDataException("Invalid file size");
        using (var stream = new MemoryStream(file, false))
        using (var reader = new BinaryReader(stream))
        {
            if (!reader.ReadBytes(8).SequenceEqual(Magic)) throw new InvalidDataException("Wrong file signature");
            if (reader.ReadUInt16() != 1) throw new UnsupportedTaruckVersionException("Unsupported container version");
            if (reader.ReadByte() != (byte)expected) throw new InvalidDataException("Wrong file type");
            if (reader.ReadUInt16() != 0) throw new InvalidDataException("Unsupported flags");
            ulong length = reader.ReadUInt64();
            uint crc = reader.ReadUInt32();
            uint mediaCount = reader.ReadUInt32();
            if (length > MaxPayload || length > (ulong)(stream.Length - stream.Position) || mediaCount > 1)
                throw new InvalidDataException("Invalid payload length or media count");
            byte[] payload = reader.ReadBytes((int)length);
            if (Crc32(payload) != crc) throw new InvalidDataException("Payload checksum failed");
            media = null;
            if (mediaCount == 1)
            {
                if (stream.Length - stream.Position < 16 || reader.ReadUInt32() != 1) throw new InvalidDataException("Invalid media ID");
                ushort nameLength = reader.ReadUInt16();
                if (nameLength > 64 || nameLength > stream.Length - stream.Position) throw new InvalidDataException("Invalid media name");
                string name = Utf8.GetString(reader.ReadBytes(nameLength));
                if (name.Contains("/") || name.Contains("\\") || name.Contains("..") || name.Contains(":")) throw new InvalidDataException("Unsafe media name");
                ushort mimeLength = reader.ReadUInt16();
                if (mimeLength > 64 || mimeLength > stream.Length - stream.Position) throw new InvalidDataException("Invalid MIME");
                string mime = Utf8.GetString(reader.ReadBytes(mimeLength));
                uint mediaLength = reader.ReadUInt32();
                uint mediaCrc = reader.ReadUInt32();
                if (mediaLength > MaxMedia || mediaLength > stream.Length - stream.Position) throw new InvalidDataException("Invalid media length");
                media = reader.ReadBytes((int)mediaLength);
                if (Crc32(media) != mediaCrc || !(mime == "image/png" && name == "item-image.png" && IsPng(media) ||
                    mime == "image/jpeg" && name == "item-image.jpg" && IsJpeg(media)))
                    throw new InvalidDataException("Invalid media");
            }
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing file data");
            var root = new ObjectReader(payload, 0);
            if (root.Int(1) != 1) throw new UnsupportedTaruckVersionException("Unsupported schema version");
            if (string.IsNullOrEmpty(root.Text(2)) || root.Text(3) != "DND5E") throw new InvalidDataException("Unsupported application/rules version");
            return root.Object(10);
        }
    }
    public static AppSaveData DecodeLocal(byte[] file)
    {
        byte[] media; byte[] body = ReadEnvelope(file, TaruckFileType.LocalData, out media);
        if (media != null) throw new InvalidDataException("Unexpected media");
        AppSaveData result = DecodeApp(body);
        TaruckDataValidator.Validate(result);
        return result;
    }
    public static AppSaveData DecodeFullSave(byte[] file)
    {
        byte[] media; byte[] body = ReadEnvelope(file, TaruckFileType.FullSaveExport, out media);
        if (media != null) throw new InvalidDataException("Unexpected media");
        AppSaveData result = DecodeApp(body);
        TaruckDataValidator.Validate(result);
        return result;
    }
    public static CharacterData DecodeCharacterExport(byte[] file)
    {
        byte[] media; byte[] body = ReadEnvelope(file, TaruckFileType.CharacterExport, out media);
        if (media != null) throw new InvalidDataException("Unexpected media");
        CharacterData result = DecodeCharacter(body, 1);
        TaruckDataValidator.Validate(result);
        return result;
    }
    public static InventoryItemExportData DecodeItemExport(byte[] file)
    {
        byte[] media; byte[] body = ReadEnvelope(file, TaruckFileType.ItemExport, out media);
        uint id; var item = DecodeItemBody(body, out id);
        if ((id == 0) != (media == null) || id > 1) throw new InvalidDataException("Invalid image reference");
        if (media != null) item.customImageBase64 = Convert.ToBase64String(media);
        return item;
    }
    public static uint Crc32(byte[] bytes)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte value in bytes)
        {
            crc ^= value;
            for (int i = 0; i < 8; i++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xEDB88320u : 0u);
        }
        return crc ^ 0xFFFFFFFF;
    }
}
