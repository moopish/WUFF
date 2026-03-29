using WUFF.Err;
using WUFF.Bytes;
using System.Drawing;
using WUFF.Image.Bitmap;

namespace WUFF.Image.Icon
{
    /// <summary>
    /// Represents an Icon or Cursor.
    /// </summary>
    public class Icon
    {
        /// <summary>
        /// The type. Is it an Icon or a Cursor.
        /// </summary>
        private enum Type
        {
            Icon = 1,
            Cursor = 2
        }

        /// <summary>
        /// The file header.
        /// </summary>
        private Header _header;

        /// <summary>
        /// The images stored in the file.
        /// </summary>
        private Bitmap.Bitmap[] _images;

        /// <summary>
        /// The number of images in icon/cursor.
        /// </summary>
        public int Count => _images.Length;

        /// <summary>
        /// Get the image at the given index.
        /// </summary>
        /// <param name="index">The index of the image to retrieve.</param>
        /// <returns>The image at the index given.</returns>
        public Bitmap.Bitmap this[int index]
        {
            get => _images[index];
        }

        /// <summary>
        /// Creates an Icon/Cursor given the file header and the images parsed from the file.
        /// </summary>
        /// <param name="header">The file header.</param>
        /// <param name="images">The images from the file in an array.</param>
        private Icon(Header header, Bitmap.Bitmap[] images)
        {
            _header = header;
            _images = images;
        }

        /// <summary>
        /// Loads the given file as an Icon/Cursor file.
        /// </summary>
        /// <param name="filename">The file name.</param>
        /// <returns>The <see cref="Icon"/> that represents the given file.</returns>
        /// <exception cref="FileParseException"></exception>
        public static Icon Load(string filename)
        {
            byte[] bytes = File.ReadAllBytes(filename);
            LittleEndianReader reader = new(bytes);

            Header header = new()
            {
                Reserved = reader.UShort(),
                Type = (Type)reader.UShort(),
                Count = reader.UShort()
            };

            if (header.Reserved != 0) throw new FileParseException("First byte is not zero. It cannot be a Icon or Cursor file.");
            if (header.Type != Type.Cursor && header.Type != Type.Icon) throw new FileParseException("File is not marked as a Cursor or Icon type.");
            if (header.Count == 0) throw new FileParseException("File contains no images.");

            DirectoryEntry[] entries = new DirectoryEntry[header.Count];

            for (int i = 0; i < entries.Length; ++i)
            {
                if (reader.Remaining < 16) throw new FileParseException("File does not contain the stated number of entries.");

                entries[i] = new()
                {
                    Width = reader.Byte(),
                    Height = reader.Byte(),
                    ColourCount = reader.Byte(),
                    Reserved = reader.Byte(),
                    Field1 = reader.UShort(),
                    Field2 = reader.UShort(),
                    ByteCount = reader.UInt(),
                    Offset = reader.UInt()
                };

                //TODO validate entry
                if (entries[i].Offset < reader.Position) throw new FileParseException("Offset of entry points to before the end of the entry when it should be after.");
                if (entries[i].Offset >= bytes.Length) throw new FileParseException("Offset points to a location beyond the end of the file.");
                if (entries[i].Reserved != 0) throw new FileParseException("Reserved in directory entry was found not to be zero.");
            }

            Bitmap.Bitmap[] images = new Bitmap.Bitmap[header.Count];

            for (int i = 0; i < entries.Length; ++i)
            {
                // TODO handle PNG
                uint offset = entries[i].Offset;
                InfoHeader info = InfoHeader.Parse(bytes, offset);
                offset += (uint)info.Type;

                Palette palette = Palette.Parse(bytes, info, offset);
                offset += (uint)(palette.ColourCount * (Palette.HasAlpha(info.Type) ? 4 : 3));

                if (entries[i].Height * 2 == info.Height) info = info.IconHalfHeightVariant();
                InfoHeader maskInfo = info.IconMaskVariant();

                reader = new LittleEndianReader(bytes, offset);
                Color[] pixels = Bitmap.Bitmap.ParseColourData(reader, info, palette);
                Color[] mask = Bitmap.Bitmap.ParseColourData(reader, maskInfo, Palette.BlackAndWhitePalette);

                for (int j = 0; j < pixels.Length; ++j)
                {
                    if (mask[j] == Palette.BlackAndWhitePalette[1]) pixels[j] = Color.Transparent;
                }

                images[i] = new Bitmap.Bitmap(info.Width, info.Height, pixels);
            }

            return new Icon(header, images);
        }

        /// <summary>
        /// A structure representing the header of a Icon/Cursor file.
        /// </summary>
        private struct Header
        {
            public ushort Reserved;
            public Type Type;
            public ushort Count;
        }

        /// <summary>
        /// A structure representing a file directory entry.
        /// </summary>
        private struct DirectoryEntry
        {
            public byte Width;
            public byte Height;
            public byte ColourCount; // 0 if >=8bpp
            public byte Reserved; //should be 0
            public ushort Field1; // supposedly planes (icon), x hotspot (cursor)
            public ushort Field2; // supposedly bit count (icon), y hotspot (cursor)
            public uint ByteCount;
            public uint Offset;
        }
    }
}
