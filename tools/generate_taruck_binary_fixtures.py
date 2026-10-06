from pathlib import Path
import struct
import zlib

out = Path('Assets/Tests/Fixtures/Binary')
out.mkdir(parents=True, exist_ok=True)

def tlv(field, kind, value):
    return struct.pack('<IBI', field, kind, len(value)) + value

def txt(field, value):
    return tlv(field, 1, value.encode('utf-8'))

def num(field, value):
    return tlv(field, 2, struct.pack('<i', value))

def coll(field, items=()):
    value = struct.pack('<I', len(items))
    for item in items:
        value += struct.pack('<I', len(item)) + item
    return tlv(field, 7, value)

def envelope(kind, root, media=b''):
    payload = num(1, 1) + txt(2, 'fixture-1') + txt(3, 'DND5E') + tlv(10, 6, root)
    header = b'TARUCKPK' + struct.pack('<HBHQII', 1, kind, 0, len(payload), zlib.crc32(payload), bool(media))
    return header + payload + media

empty = txt(1, '') + coll(2)
(out / 'v1_empty_full.taruck-save').write_bytes(envelope(2, empty))

scene = txt(1, 'cartaPersonaj') + coll(2, [b'14', 'Клерик'.encode('utf-8')])
scene += coll(3, [b'\x01']) + coll(4, [struct.pack('<f', 0.375)]) + coll(5, [struct.pack('<i', 2)])
scene += coll(6) + coll(7) + coll(8) + num(9, 1)
character = txt(1, 'fixture-character') + txt(2, 'Тестовий герой') + num(3, 25) + num(4, 17)
character += coll(5) + coll(6) + coll(7) + coll(8) + coll(9, [scene]) + coll(10)
(out / 'v1_character.taruck-character').write_bytes(envelope(3, character))

def png_chunk(kind, data):
    return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data))

png = b'\x89PNG\r\n\x1a\n' + png_chunk(b'IHDR', struct.pack('>IIBBBBB', 1, 1, 8, 2, 0, 0, 0))
png += png_chunk(b'IDAT', zlib.compress(b'\x00\xff\x00\x00')) + png_chunk(b'IEND', b'')
item = txt(1, 'Зілля') + txt(2, 'Тест') + b''.join(num(i, 0) for i in range(3, 10))
item += tlv(10, 3, struct.pack('<I', 1))
name, mime = b'item-image.png', b'image/png'
media = struct.pack('<IH', 1, len(name)) + name + struct.pack('<H', len(mime)) + mime
media += struct.pack('<II', len(png), zlib.crc32(png)) + png
(out / 'v1_item_png.taruck-item').write_bytes(envelope(4, item, media))
